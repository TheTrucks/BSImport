using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BSImport.Restrictor;
using FHR = EntityNH.Entity;
using DFO = BSImport.DFOEntity.Entity;

namespace BSImport
{
    public class Importer<T>
    {
        private Dictionary<int, int> _VarStationType;
        private CacheManager DateCache;
        private IRestrictor<T> StationRestrictor;
        private int HoursBack;
        public Importer(string ParamsFilename, IRestrictor<T> InputStationRestrictor, string CacheFilename, int HoursBack)
        {
            _VarStationType = LoadVarStationTypes(ParamsFilename);
            StationRestrictor = InputStationRestrictor;
            this.HoursBack = HoursBack;
            DateCache = new CacheManager(CacheFilename);
        }
        public void StartImport(string DateSince = null)
        {
            DateTime Since;
            if (String.IsNullOrEmpty(DateSince)
                || !DateTime.TryParseExact(DateSince.Trim(), "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out Since))
            { Since = DateCache.LastDateTime.AddHours(-HoursBack); }

            var Launched = DateTime.UtcNow;

            for (DateTime TimePiece = Since; Launched - TimePiece > TimeSpan.FromMinutes(1); TimePiece = TimePiece.AddHours(3)) // breaking big timespan into smaller pieces
            {
                Import(TimePiece, TimePiece.AddHours(3));
            }

            DateCache.LastDateTime = Launched;
        }
        private Dictionary<int, int> LoadVarStationTypes(string PFile)
        {
            Dictionary<int, int> Result = new Dictionary<int, int>();
            using (var FS = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PFile), FileMode.Open))
            {
                using (var SR = new StreamReader(FS, Encoding.UTF8))
                {
                    int CurrentVar = -1;
                    while (!SR.EndOfStream)
                    {
                        string TheLine = SR.ReadLine().Trim();
                        if (TheLine == String.Empty)
                            continue;
                        else
                            TheLine = new string(TheLine.TakeWhile(x => x != '#').ToArray()).Trim();

                        if (TheLine.StartsWith("["))
                        {
                            CurrentVar = Int32.Parse(TheLine.Trim(new char[] { '[', ']' }));
                        }
                        else
                        {
                            if (CurrentVar > 0)
                            {
                                int TheKey = Int32.Parse(TheLine);
                                if (!Result.ContainsKey(TheKey))
                                    Result.Add(TheKey, CurrentVar);
                            }
                            else
                                continue;
                        }

                    }
                }
            }

            return Result;
        }

        private void Import(DateTime Since, DateTime To)
        {
            LogManager.Log.Info("#########");
            LogManager.Log.Info($"Starting import from {Since.ToString("yyyy-MM-dd HH:mm")} to {To.ToString("yyyy-MM-dd HH:mm")}");
            List<FHR.Data.DataValue> InitialDVList;
            ILookup<int, FHR.Meta.SiteAttrValue> FHRAttrValues;
            using (var MainSession = ConnectionManager.AmurFerhri.OpenSession())
            {
                InitialDVList = AmurMainWorker.LoadMeteoData(MainSession, Since, To, _VarStationType.Keys.ToArray(), StationRestrictor.RestrictedTypes(), StationRestrictor.StationsList());
                FHRAttrValues = AmurMainWorker.LoadSiteAttr(MainSession, StationRestrictor.StationsList());
                LogManager.Log.Info($"Loaded {InitialDVList.Count} DV from AmurMain");
            }
            if (InitialDVList.Count == 0)
            {
                LogManager.Log.Info("No new data were found.");
                return;
            }

            using (var AltSession = ConnectionManager.AmurDFO.OpenSession())
            {
                var StationList = AmurDFOWorker.StationList(AltSession);
                LogManager.Log.Info($"Loaded {StationList.Count} stations from AmurDFO");

                AltSession.Clear();
                using (var trans = AltSession.BeginTransaction())
                {
                    List<DFO.MeteoData> CurrentDataList = AmurDFOWorker.MeteoDataList(AltSession, Since, To);

                    TransmuteData Transmuted = Transmute(InitialDVList, StationList, FHRAttrValues);
                    List<DFO.MeteoData> FHRtoDFO = Transmuted.TransmutedData;

                    int NewStationsDataCounter = 0;
                    if (Transmuted.NewStations != null && Transmuted.NewStations.Count > 0)
                    {
                        LogManager.Log.Info($"New stations to add: {Transmuted.NewStations.Count}");

                        foreach (var NewStationData in Transmuted.NewStations)
                        {
                            DFO.Station TempNewStation = NewStationData.Key;
                            if (TempNewStation == null)
                                continue;
                            AltSession.Save(TempNewStation);
                            TempNewStation.MeteoData = new List<DFO.MeteoData>();
                            foreach (var MData in NewStationData)
                            {
                                TempNewStation.MeteoData.Add(TransmuteOne(MData, TempNewStation));
                                NewStationsDataCounter++;
                            }
                            AddOptAttrs(TempNewStation, FHRAttrValues[NewStationData.First().Catalog.Site.Id.Value].OrderByDescending(x => x.DateS));
                            AltSession.Update(TempNewStation);
                        }
                    }

                    List<DFO.MeteoData> UpdateMeteoData;
                    var NewMeteoData = NewValues(FHRtoDFO, CurrentDataList, out UpdateMeteoData);
                    if (UpdateMeteoData.Count > 0) LogManager.Log.Info($"{UpdateMeteoData.Count} updates");
                    if (NewMeteoData.Count > 0) LogManager.Log.Info($"{NewMeteoData.Count + NewStationsDataCounter} new values");

                    foreach (var MeteoItem in NewMeteoData)
                    {
                        AltSession.Save(MeteoItem);
                        System.Console.WriteLine("Информация по станции id=" + MeteoItem.Station.Id + " за срок " + MeteoItem.DateUtc + " успешно добавлена.");
                    }
                    foreach (var MeteoItem in UpdateMeteoData)
                    {
                        AltSession.Update(MeteoItem);
                    }

                    try
                    {
                        trans.Commit();
                    }
                    catch (Exception Exc)
                    {
                        LogManager.Log.Error($"[{Since.ToString("yyyy-MM-dd HH:mm")}][{To.ToString("yyyy-MM-dd HH:mm")}]An error occured trying to commit transaction:");
                        LogManager.Log.Error(Exc.InnerException != null ? Exc.InnerException.ToString() : Exc.ToString());
                    }
                }
            }
        }
        /// <summary>
        /// Filters out already existing data, splits data left on new one and the one that needs to be updated (existing but with different value)
        /// </summary>
        /// <param name="InputList">Converted DFO DB data list</param>
        /// <param name="FilterList">Data loaded from DFO DB</param>
        /// <param name="Updates">Out param for the 'update data'</param>
        /// <returns></returns>
        private List<DFO.MeteoData> NewValues(List<DFO.MeteoData> InputList, List<DFO.MeteoData> FilterList, out List<DFO.MeteoData> Updates)
        {
            var Comparer = new MeteoDataComparer();
            List<DFO.MeteoData> NewValues = new List<DFO.MeteoData>();

            HashSet<DFO.MeteoData> Set = new HashSet<DFO.MeteoData>(Comparer);
            Updates = new List<DFO.MeteoData>();

            foreach (var Element in FilterList)
                Set.Add(Element);

            foreach (var Element in InputList)
            {
                if (!Set.Contains(Element))
                    NewValues.Add(Element);
                else
                {
                    var TmpMeteo = FilterList.First(x => Comparer.Equals(x, Element));
                    if (TmpMeteo.Value.Value != Element.Value.Value)
                    {
                        TmpMeteo.Value = Element.Value;
                        Updates.Add(TmpMeteo);
                    }
                }
            }                

            return NewValues;
        }
        /// <summary>
        /// Convert FHR DB data to DFO DB data
        /// </summary>
        /// <param name="InputList">FHR DB data list</param>
        /// <param name="StationFilter">DFO DB stations list</param>
        /// <returns>Converted data list and key-value list with key = station to add in DFO DB and value is its data list</returns>
        private TransmuteData Transmute (List<FHR.Data.DataValue> InputList, List<DFO.Station> StationFilter, ILookup<int, FHR.Meta.SiteAttrValue> AttrList)
        {
            HashSet<int> NewStationsHash = new HashSet<int>();
            List<FHR.Data.DataValue> NewStationsData = new List<FHR.Data.DataValue>();
            ILookup<DFO.Station, FHR.Data.DataValue> NewStations = null;
            var StationComparer = new DFOStationComparer();
            var CatalogComparer = new FHRCatalogComparer();

            HashSet<ValueTuple<string, int>> StationsHash = new HashSet<ValueTuple<string, int>>();
            foreach (var Station in StationFilter)
                StationsHash.Add(new ValueTuple<string, int>(Station.Code, Station.StationType.Id));

            List<DFO.MeteoData> Result = new List<DFO.MeteoData>();
            foreach (var InputGroup in InputList.GroupBy(x => x.Catalog, CatalogComparer))
            {
                var Input = InputGroup.Key;
                var InputStationType = GetStationType(Input, StationRestrictor.IsStrong(Input.Site.Type.Id.Value, Input.Site.Station.Code));
                if (!(StationRestrictor.Approved(Input.Site.Station.Code, Input.Site.Type.Id.Value) && 
                    OrigStationParamExists(Input)))
                    continue;

                var FilteredData = FilterOnItself(InputGroup.ToList());
                if (!StationsHash.Contains(new ValueTuple<string, int>(Input.Site.Station.Code, InputStationType)))
                    NewStationsData.AddRange(FilteredData);
                else
                {
                    var TmpDFOStation = StationFilter.First(x => x.Code == Input.Site.Station.Code && x.StationType.Id == InputStationType);
                    foreach (var DataItem in FilteredData)
                        Result.Add(TransmuteOne(DataItem, TmpDFOStation));
                }
            }

            if (NewStationsData.Count > 0)
            {
                NewStations = NewStationsData.ToLookup(key => CreateDFOStation(
                    key, 
                    AttrList,
                    StationRestrictor.IsStrong(key.Catalog.Site.Type.Id.Value, key.Catalog.Site.Station.Code)), 
                    StationComparer);
            }

            return new TransmuteData(Result, NewStations);
        }
        /// <summary>
        /// Filter values to deny multiple data values per hour
        /// </summary>
        /// <param name="InputList"></param>
        /// <returns>Filtered FHR DB data list</returns>
        private List<FHR.Data.DataValue> FilterOnItself(List<FHR.Data.DataValue> InputList)
        {
            List<FHR.Data.DataValue> Result = new List<FHR.Data.DataValue>();
            HashSet<ValueTuple<DateTime, int, double>> UniqueDV = new HashSet<ValueTuple<DateTime, int, double>>();
            foreach (var Item in InputList.OrderByDescending(x => x.Id.Value))
            {
                if (UniqueDV.Add(new ValueTuple<DateTime, int, double>(Item.DateUTC, Item.Catalog.OffsetType.Id.Value, Item.Catalog.OffsetValue.Value)))
                    Result.Add(Item);
            }
            return Result;
        }
        private DFO.Station CreateDFOStation(FHR.Data.DataValue Input, ILookup<int, FHR.Meta.SiteAttrValue> AttrList, bool IsStrong)
        {
            try
            {
                if (!Input.Catalog.Site.Station.AddrRegion.HasValue)
                {
                    LogManager.Log.Error($"No AddrRegion for station [{Input.Catalog.Site.Station.Id.Value}] {Input.Catalog.Site.Station.Name}");
                    throw new ArgumentNullException("No AddrRegion");
                }
                var SiteAttr = AttrList[Input.Catalog.Site.Id.Value].OrderByDescending(x => x.DateS);
                var NewStation = new DFO.Station
                {
                    Altitude = GetAttrValue(SiteAttr, 1006),
                    Latitude = GetAttrValue(SiteAttr, 1000),
                    Longtitude = GetAttrValue(SiteAttr, 1001),
                    Code = Input.Catalog.Site.Station.Code,
                    Name = Input.Catalog.Site.Station.Name,
                    StationType = new DFO.StationType
                    {
                        Id = GetStationType(Input.Catalog, IsStrong)
                    },
                    Region = new DFO.Addr
                    {
                        Id = Input.Catalog.Site.Station.AddrRegion.Value
                    }
                };
                
                return NewStation;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Attempt to insert optional attribute data
        /// </summary>
        /// <param name="Station">DFO DB meteo station the attributes are intended for</param>
        /// <param name="AttrList">FHR DB attributes from the corresponding station</param>
        private void AddOptAttrs(DFO.Station Station, IEnumerable<FHR.Meta.SiteAttrValue> AttrList)
        {
            if (AttrList.Count() > 0)
            {
                Station.AttrValues = new List<DFO.AttrValue>();
                foreach (var AttrType in new int[] { 1087, 1088, 1089 })
                {
                    var NewAttr = GetAttrString(AttrList, AttrType);
                    if (!String.IsNullOrEmpty(NewAttr))
                        Station.AttrValues.Add(
                            new DFO.AttrValue
                            {
                                Attribute = new DFO.Attr
                                {
                                    Id = AttrType
                                },
                                DateStart = DateTime.UtcNow,
                                Station = new DFO.Station
                                {
                                    Id = Station.Id
                                },
                                Value = NewAttr
                            }
                        );
                }
            }
        }
        private string GetAttrString(IEnumerable<FHR.Meta.SiteAttrValue> AttrList, int AttrType)
        {
            var LastOne = AttrList.FirstOrDefault(x => x.Type.Id == AttrType);
            if (LastOne != null)
                return LastOne.Value.Replace(",", ".");
            else return null;
        }
        private double? GetAttrValue(IEnumerable<FHR.Meta.SiteAttrValue> AttrList, int AttrType)
        {
            var LastOne = GetAttrString(AttrList, AttrType);
            if (!String.IsNullOrEmpty(LastOne))
                return Double.Parse(LastOne, System.Globalization.CultureInfo.InvariantCulture);
            else return null;
        }
        /// <summary>
        /// Transmutes meteo data from FHR DB to corresponding DFO DB meteo data
        /// </summary>
        /// <param name="Input">FHR DB data to transmute</param>
        /// <param name="Station">DFO DB meteo station for which the data is intended</param>
        /// <returns>Transmuted DFO DB meteo data</returns>
        private DFO.MeteoData TransmuteOne(FHR.Data.DataValue Input, DFO.Station Station)
        {
            return new DFO.MeteoData
            {
                Station = new DFO.Station
                {
                    Id = Station.Id
                },
                Variable = new DFO.Variable
                {
                    Id = Input.Catalog.Variable.Id.Value
                },
                DateLoc = Input.DateLocal,
                DateUtc = Input.DateUTC,
                OffsetType = new DFO.OffsetType
                {
                    Id = Input.Catalog.OffsetType.Id.Value
                },
                OffsetValue = Input.Catalog.OffsetValue.Value,
                Value = Input.Value
            };
        }
        private int GetStationType(FHR.Data.Catalog Input, bool IsStrong)
        {
            int Result;
            if (IsStrong)
                Result = Input.Site.Type.Id.Value;
            else if (!_VarStationType.TryGetValue(Input.Variable.Id.Value, out Result))
                Result = -1;
            return Result;
        }
        private bool OrigStationParamExists(FHR.Data.Catalog Input)
        {
            return (_VarStationType.ContainsKey(Input.Variable.Id.Value) && 
                _VarStationType[Input.Variable.Id.Value] == Input.Site.Type.Id.Value);
        }
        private class MeteoDataComparer : IEqualityComparer<DFO.MeteoData> // custom comparer which ignores value and id
        {
            public bool Equals(DFO.MeteoData First, DFO.MeteoData Second)
            {
                return
                    First.DateUtc == Second.DateUtc
                    && First.Station.Id == Second.Station.Id
                    && First.Variable.Id == Second.Variable.Id
                    && First.OffsetType.Id == Second.OffsetType.Id
                    && First.OffsetValue == Second.OffsetValue;
            }

            public int GetHashCode(DFO.MeteoData Value)
            {
                unchecked
                {
                    return Value.DateUtc.GetHashCode() * (Value.Station.Id >> Value.Variable.Id + (int)Value.OffsetValue << Value.OffsetType.Id.Value); 
                }
            }
        }

        private class DFOStationComparer : IEqualityComparer<DFO.Station> // custom comparer for creating ILookup
        {
            public bool Equals(DFO.Station First, DFO.Station Second)
            {
                if (First == null && Second == null)
                    return true;
                else if (First == null || Second == null)
                    return false;
                else
                    return First.Code == Second.Code && First.StationType.Id == Second.StationType.Id;   
            }

            public int GetHashCode(DFO.Station Value)
            {
                if (Value == null)
                    return 0;
                return Value.Code.GetHashCode() ^ (Value.StationType.Id * 255) >> 27;
            }
        }

        private class FHRCatalogComparer : IEqualityComparer<FHR.Data.Catalog> // custom comparer for grouping
        {
            public bool Equals(FHR.Data.Catalog First, FHR.Data.Catalog Second)
            {
                return First.Site.Id.Value == Second.Site.Id.Value && First.Variable.Id.Value == Second.Variable.Id.Value;
            }

            public int GetHashCode(FHR.Data.Catalog Value)
            {
                return Value.Site.Id.Value >> 27 + Value.Variable.Id.Value << 27;
            }
        }

        private struct TransmuteData
        {
            public List<DFO.MeteoData> TransmutedData;
            public ILookup<DFO.Station, FHR.Data.DataValue> NewStations;

            public TransmuteData(List<DFO.MeteoData> TransmutedData, ILookup<DFO.Station, FHR.Data.DataValue> NewStations)
            {
                this.TransmutedData = TransmutedData;
                this.NewStations = NewStations;
            }
        }
    }

    public class ImporterJob : Quartz.IJob
    {
        public async Task Execute(Quartz.IJobExecutionContext Details)
        {
            try
            {
                int[] StationTypes = Details.Trigger.JobDataMap.GetString("Types")
                    .Split(new char[] { ',' })
                    .Select(x => Int32.Parse(x))
                    .ToArray();

                var DbRestrReader = new DatabaseRestrictsUpdater(ConnectionManager.AmurDFO, StationTypes);
                var DbRestrictor = new DefaultRestrictor<NHibernate.ISessionFactory>(DbRestrReader);

                var ImportWorker = new Importer<NHibernate.ISessionFactory>(
                    Details.Trigger.JobDataMap.GetString("Params"),
                    DbRestrictor,
                    Details.Trigger.JobDataMap.GetString("Cache"),
                    Details.Trigger.JobDataMap.GetInt("Hours"));
                await Task.Run(() => ImportWorker.StartImport());
            }
            catch (Exception Exc)
            {
                LogManager.Log.Error("Job failed:");
                LogManager.Log.Error(Exc.ToString());
            }
        }
    }
}

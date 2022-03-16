using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FHR = EntityNH.Entity;
using DFO = BSImport.DFOEntity.Entity;
using NHibernate.Type;
using NHibernate.SqlCommand;

namespace BSImport
{
    public class Importer
    {
        private Dictionary<int, int> _VarStationType = new Dictionary<int, int>();
        public Importer(string ConfigPath)
        {
            LoadVarStationTypes(ConfigPath);

        }
        public void StartImport()
        {
            var Since = CacheManager.LastDateTime.AddHours(-3);
            var Launched = DateTime.UtcNow.AddMinutes(1);

            for (DateTime TimePiece = Since; TimePiece < Launched; TimePiece = TimePiece.AddHours(3)) // breaking big timespan into smaller pieces
            {
                Import(TimePiece, TimePiece.AddHours(3));
            }

            CacheManager.LastDateTime = Launched;
        }
        private void LoadVarStationTypes(string CPath)
        {
            using (var FS = File.Open(CPath, FileMode.Open))
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
                            TheLine = TheLine.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                        if (TheLine.StartsWith("["))
                        {
                            CurrentVar = Int32.Parse(TheLine.Trim(new char[] { '[', ']' }));
                        }
                        else
                        {
                            if (CurrentVar > 0)
                            {
                                int TheKey = Int32.Parse(TheLine);
                                if (!_VarStationType.ContainsKey(TheKey))
                                    _VarStationType.Add(TheKey, CurrentVar);
                            }
                            else
                                continue;
                        }

                    }
                }
            }
        }

        private void Import(DateTime Since, DateTime To)
        {
            LogManager.Log.Info("#########");
            LogManager.Log.Info($"Starting import from {Since.ToString("yyyy-MM-dd HH:mm")} to {To.ToString("yyyy-MM-dd HH:mm")}");
            List<FHR.Data.DataValue> InitialDVList;
            using (var MainSession = ConnectionManager.AmurFerhri.OpenSession())
            {
                InitialDVList = AmurMainWorker.LoadMeteoData(MainSession, Since, To, _VarStationType.Keys.ToArray());
                LogManager.Log.Info($"Loaded {InitialDVList.Count} DV from AmurMain");
            }

            using (var AltSession = ConnectionManager.AmurDFO.OpenSession())
            {
                var StationList = AmurDFOWorker.StationList(AltSession);
                LogManager.Log.Info($"Loaded {StationList.Count} stations from AmurDFO");

                AltSession.Clear();
                using (var trans = AltSession.BeginTransaction())
                {
                    var CurrentDataList = AmurDFOWorker.MeteoDataList(AltSession, Since, To);

                    var Transmuted = Transmute(InitialDVList, StationList);
                    var FHRtoDFO = Transmuted.TransmutedData;
                    LogManager.Log.Info($"Transmuted {FHRtoDFO.Count}/{InitialDVList.Count}");

                    int NewStationsDataCounter = 0;
                    if (Transmuted.NewStations != null && Transmuted.NewStations.Count > 0)
                    {
                        LogManager.Log.Info($"New stations to add: {Transmuted.NewStations.Count}");

                        foreach (var NewStationData in Transmuted.NewStations)
                        {
                            var TempNewStation = NewStationData.Key;
                            if (TempNewStation == null)
                                continue;
                            AltSession.Save(TempNewStation);
                            TempNewStation.MeteoData = new List<DFO.MeteoData>();
                            foreach (var MData in NewStationData)
                            {
                                TempNewStation.MeteoData.Add(TransmuteOne(MData, TempNewStation));
                                NewStationsDataCounter++;
                            }
                            AddOptAttrs(TempNewStation, NewStationData.First().Catalog.Site.AttrValues.OrderByDescending(x => x.DateS));
                            AltSession.Update(TempNewStation);
                        }
                    }

                    List<DFO.MeteoData> UpdateMeteoData;
                    var NewMeteoData = NewValues(FHRtoDFO, CurrentDataList, out UpdateMeteoData);
                    LogManager.Log.Info($"{UpdateMeteoData.Count}/{FHRtoDFO.Count} updates");
                    LogManager.Log.Info($"{NewMeteoData.Count + NewStationsDataCounter}/{FHRtoDFO.Count} new values");

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
                    if (TmpMeteo.Value != Element.Value)
                    {
                        TmpMeteo.Value = Element.Value;
                        Updates.Add(TmpMeteo);
                    }
                }
            }                

            return NewValues;
        }

        private TransmuteData Transmute (List<FHR.Data.DataValue> InputList, List<DFO.Station> StationFilter)
        {
            HashSet<int> NewStationsHash = new HashSet<int>();
            List<FHR.Data.DataValue> NewStationsData = new List<FHR.Data.DataValue>();
            ILookup<DFO.Station, FHR.Data.DataValue> NewStations = null;
            var StationComparer = new DFOStationComparer();
            var CatalogComparer = new FHRCatalogComparer();
            var StationsRestrict = new Restrictor("restricts.ff");

            HashSet<ValueTuple<string, int>> StationsHash = new HashSet<ValueTuple<string, int>>();
            foreach (var Station in StationFilter)
                StationsHash.Add(new ValueTuple<string, int>(Station.Code, Station.StationType.Id));

            List<DFO.MeteoData> Result = new List<DFO.MeteoData>();
            foreach (var InputGroup in InputList.GroupBy(x => x.Catalog, CatalogComparer))
            {
                var Input = InputGroup.Key;
                if (!StationsRestrict.Approved(Input.Site.Station.Code, GetStationType(Input.Variable.Id.Value)))
                    continue;
                if (!StationsHash.Contains(new ValueTuple<string, int>(Input.Site.Station.Code, GetStationType(Input.Variable.Id.Value))))
                    NewStationsData.AddRange(InputGroup.ToList());
                else
                {
                    var TmpDFOStation = StationFilter.First(x => x.Code == Input.Site.Station.Code && x.StationType.Id == GetStationType(Input.Variable.Id.Value));
                    foreach (var DataItem in InputGroup)
                        Result.Add(TransmuteOne(DataItem, TmpDFOStation));
                }
            }

            if (NewStationsData.Count > 0)
            {
                NewStations = NewStationsData.ToLookup(key => CreateDFOStation(key), StationComparer);
            }

            return new TransmuteData(Result, NewStations);
        }
        private DFO.Station CreateDFOStation(FHR.Data.DataValue Input)
        {
            try
            {
                if (!Input.Catalog.Site.Station.AddrRegion.HasValue)
                {
                    LogManager.Log.Error($"No AddrRegion for station [{Input.Catalog.Site.Station.Id.Value}] {Input.Catalog.Site.Station.Name}");
                    throw new ArgumentNullException("No AddrRegion");
                }
                var SiteAttr = Input.Catalog.Site.AttrValues.OrderByDescending(x => x.DateS);
                var NewStation = new DFO.Station
                {
                    Altitude = GetAttrValue(SiteAttr, 1006),
                    Latitude = GetAttrValue(SiteAttr, 1000),
                    Longtitude = GetAttrValue(SiteAttr, 1001),
                    Code = Input.Catalog.Site.Station.Code,
                    Name = Input.Catalog.Site.Station.Name,
                    StationType = new DFO.StationType
                    {
                        Id = GetStationType(Input.Catalog.Variable.Id.Value)
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
        private void AddOptAttrs(DFO.Station Station, IEnumerable<FHR.Meta.SiteAttrValue> AttrList)
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
        private int GetStationType(int VarID)
        {
            int Result;
            if (!_VarStationType.TryGetValue(VarID, out Result))
                Result = -1;
            return Result;
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
                return Value.DateUtc.GetHashCode() + (Value.Station.Id ^ Value.Variable.Id) + (int)(Value.OffsetValue * Value.OffsetType.Id.Value);
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
                return Value.Code.GetHashCode() ^ 3 * Value.StationType.Id;
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
                return Value.Site.Id.Value ^ 5 * Value.Variable.Id.Value;
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
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityNH.Hibernate;
using FHR = EntityNH.Entity;
using DFO = BSImport.DFOEntity.Entity;

namespace BSImport
{
    public class Importer
    {
        public void StartImport()
        {
            var Since = CacheManager.LastDateTime.AddHours(-1);
            var Launched = DateTime.UtcNow;

            for (DateTime TimePiece = Since; TimePiece < Launched; TimePiece = TimePiece.AddHours(3)) // breaking big timespan on smaller pieces
            {
                Import(TimePiece, TimePiece.AddHours(3));
            }

            CacheManager.LastDateTime = Launched;
        }

        private void Import(DateTime Since, DateTime To)
        {
            LogManager.Log.Info($"Starting import from {Since.ToString("yyyy-MM-dd HH:mm")} to {To.ToString("yyyy-MM-dd HH:mm")}");
            List<FHR.Data.DataValue> InitialDVList;
            using (var MainSession = ConnectionManager.AmurFerhri.OpenStatelessSession())
            {
                InitialDVList = AmurMainWorker.LoadMeteoData(MainSession, Since, To);
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

                    List<DFO.MeteoData> UpdateMeteoData;
                    var FHRtoDFO = Transmute(InitialDVList, StationList);
                    LogManager.Log.Info($"Transmuted {FHRtoDFO.Count}/{InitialDVList.Count}");
                    var NewMeteoData = NewValues(FHRtoDFO, CurrentDataList, out UpdateMeteoData);
                    LogManager.Log.Info($"{NewMeteoData.Count}/{FHRtoDFO.Count} new values");
                    LogManager.Log.Info($"{UpdateMeteoData.Count}/{FHRtoDFO.Count} updates");
                    var stations_id = new List<int>();
                    foreach (var MeteoItem in NewMeteoData)
                    {
                        stations_id.Add(MeteoItem.Station.Id);
                        AltSession.Save(MeteoItem);
                        System.Console.WriteLine("Информация по станции id="+MeteoItem.Station.Id +" за срок "+MeteoItem.DateUtc + " успешно добавлена.");
                    }                        
                    foreach (var MeteoItem in UpdateMeteoData)
                        AltSession.Update(MeteoItem);

                    try
                    {
                        trans.Commit();
                    }
                    catch (Exception Exc)
                    {
                        LogManager.Log.Error("An error occured trying to commit transaction:");
                        LogManager.Log.Error(Exc.ToString());
                    }
                }
            }
        }

        private List<DFO.MeteoData> NewValues(List<DFO.MeteoData> InputList, List<DFO.MeteoData> FilterList, out List<DFO.MeteoData> Updates)
        {
            var Comparer = new MeteoDataComparer();
            var NewValues = InputList.Except(FilterList, Comparer).ToList();
            Updates = InputList.Except(NewValues, Comparer).ToList();
            return NewValues;
        }

        private List<DFO.MeteoData> Transmute (List<FHR.Data.DataValue> InputList, List<DFO.Station> StationFilter)
        {
            HashSet<ValueTuple<string, int>> StationsHash = new HashSet<ValueTuple<string, int>>();
            foreach (var Station in StationFilter)
                StationsHash.Add(new ValueTuple<string, int>(Station.Code, Station.StationType.Id));

            List<DFO.MeteoData> Result = new List<DFO.MeteoData>();
            foreach (var Input in InputList)
            {
                if (StationsHash.Contains(new ValueTuple<string, int> (Input.Catalog.Site.Station.Code, Input.Catalog.Site.Type.Id.Value)))
                {
                    Result.Add(TransmuteOne(Input));
                }
            }
            return Result;
        }
        private DFO.MeteoData TransmuteOne(FHR.Data.DataValue Input)
        {
            return new DFO.MeteoData
            {
                Station = new DFO.Station
                {
                    Id = Input.Catalog.Site.Id.Value
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

        private class MeteoDataComparer : IEqualityComparer<DFO.MeteoData>
        {
            public bool Equals(DFO.MeteoData First, DFO.MeteoData Second)
            {
                int comparer = 5;
                if (First.DateUtc == Second.DateUtc)
                    comparer--;
                if (First.Station.Id == Second.Station.Id)
                    comparer--;
                if (First.Variable.Id == Second.Variable.Id)
                    comparer--;
                if (First.OffsetType.Id == Second.OffsetType.Id)
                    comparer--;
                if (First.OffsetValue == Second.OffsetValue)
                    comparer--;

                return comparer == 0;
            }

            public int GetHashCode(DFO.MeteoData Value)
            {
                return Value.DateUtc.GetHashCode() - (Value.Station.Id ^ Value.Variable.Id) + (int)(Value.OffsetValue * Value.OffsetType.Id.Value);
            }
        }
    }
}

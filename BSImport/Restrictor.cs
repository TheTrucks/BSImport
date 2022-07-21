using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Linq;
using System.IO;

namespace BSImport.Restrictor
{
    public abstract class BaseRestrictsUpdater<T>
    {
        public T InputRead;
        public BaseRestrictsUpdater(T Input)
        {
            InputRead = Input;
        }

        public abstract ValueTuple<Dictionary<int, RestrictionEntry>, HashSet<string>> GetData();
    }

    public sealed class RestrictionEntry
    {
        public HashSet<string> DefStations;
        public HashSet<string> StrStations;

        public RestrictionEntry()
        {
            DefStations = new HashSet<string>();
            StrStations = new HashSet<string>();
        }

        public bool AddStation(string StationCode, bool IsStrong)
        {
            bool Result;
            if (IsStrong)
                Result = StrStations.Add(StationCode);
            else
                Result = DefStations.Add(StationCode);
            return Result;
        }

        public bool Contains(string StationCode)
        {
            return StrStations.Contains(StationCode) || DefStations.Contains(StationCode);
        }

        public bool IsStrong(string StationCode)
        {
            return StrStations.Contains(StationCode);
        }
    }

    public class FileRestrictsUpdater : BaseRestrictsUpdater<string>
    {
        public FileRestrictsUpdater(string Input) : base(Input) { }
        public override ValueTuple<Dictionary<int, RestrictionEntry>, HashSet<string>> GetData()
        {
            var RestrictionList = new Dictionary<int, RestrictionEntry>();
            var AllStationsList = new HashSet<string>();
            using (var FS = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, InputRead), FileMode.Open))
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
                            CurrentVar = Int32.Parse(TheLine.Trim(new char[] { '[', ']', ' ' }));
                            if (!RestrictionList.ContainsKey(CurrentVar))
                                RestrictionList.Add(CurrentVar, new RestrictionEntry());
                        }
                        else
                        {
                            if (CurrentVar > 0)
                            {
                                RestrictionList[CurrentVar].AddStation(TheLine.Trim(new char[] { '!', ' ' }), TheLine.Contains("!"));
                                AllStationsList.Add(TheLine);
                            }
                            else
                                continue;
                        }

                    }
                }
            }
            return ValueTuple.Create(RestrictionList, AllStationsList);
        }
    }

    public class DatabaseRestrictsUpdater : BaseRestrictsUpdater<NHibernate.ISessionFactory>
    {
        private int[] StationTypes;
        public DatabaseRestrictsUpdater(NHibernate.ISessionFactory InputSessionFactory, int[] InputStationTypes) : base(InputSessionFactory) 
        {
            if (InputStationTypes.Length > 1 || InputStationTypes[0] > 0)
                StationTypes = InputStationTypes;
            else
                StationTypes = new int[0];
        }
        public override ValueTuple<Dictionary<int, RestrictionEntry>, HashSet<string>> GetData()
        {
            var RestrictionList = new Dictionary<int, RestrictionEntry>();
            var AllStationsList = new HashSet<string>();

            using (var session = InputRead.OpenSession())
            {
                var ImportQuery = session.Query<DFOEntity.Entity.ImportStation>();
                if (StationTypes.Length > 0)
                    ImportQuery = ImportQuery.Where(x => StationTypes.Contains(x.StationType));
                
                var ImportStats = ImportQuery.ToArray();

                foreach (var Stat in ImportStats)
                {
                    if (!RestrictionList.ContainsKey(Stat.StationType))
                        RestrictionList.Add(Stat.StationType, new RestrictionEntry());
                    RestrictionList[Stat.StationType].AddStation(Stat.Code, Stat.IsStrong);
                    AllStationsList.Add(Stat.Code);
                }
            }

            return ValueTuple.Create(RestrictionList, AllStationsList);
        }
    }

    public interface IRestrictor<T>
    {
        void Update();
        bool Approved(string StationCode, int StationType);
        bool IsStrong(int AmurSiteType, string Code);
        int[] RestrictedTypes();
        string[] StationsList();
    }

    public class DefaultRestrictor<T> : IRestrictor<T>
    {
        private Dictionary<int, RestrictionEntry> _Restrictions;
        private HashSet<string> _WholeList;
        private BaseRestrictsUpdater<T> RestrictsReader;
        public DefaultRestrictor(BaseRestrictsUpdater<T> InputRestrictsReader)
        {
            RestrictsReader = InputRestrictsReader;
            Update();
        }
        
        public void Update()
        {
            var RestrictionsData = RestrictsReader.GetData();
            _Restrictions = RestrictionsData.Item1;
            _WholeList = RestrictionsData.Item2;
        }

        public bool Approved(string Code, int StationType)
        {
            return 
                _Restrictions.ContainsKey(StationType) && 
                _Restrictions[StationType].Contains(Code);
        }
        public bool IsStrong(int AmurSiteType, string Code)
        {
            return
                _Restrictions.ContainsKey(AmurSiteType) &&
                _Restrictions[AmurSiteType].IsStrong(Code);
        }
        public int[] RestrictedTypes()
        {
            return _Restrictions.Keys.ToArray();
        }
        public string[] StationsList()
        {
            return _WholeList.ToArray();
        }
    }
}

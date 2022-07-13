using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BSImport
{
    public class Restrictor
    {
        private Dictionary<int, RestrictionEntry> _Restrictions;
        private HashSet<string> _WholeList;
        public Restrictor(string Filename)
        {
            Update(Filename);
        }
        public void Update(string Filename)
        {
            _Restrictions = new Dictionary<int, RestrictionEntry>();
            _WholeList = new HashSet<string>();

            using (var FS = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Filename), FileMode.Open))
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
                            CurrentVar = Int32.Parse(TheLine.Trim(new char[] { '[', ']', '!', ' ' }));
                            if (!_Restrictions.ContainsKey(CurrentVar))
                                _Restrictions.Add(CurrentVar, new RestrictionEntry(TheLine.Contains("!")));
                        }
                        else
                        {
                            if (CurrentVar > 0)
                            {
                                
                                _Restrictions[CurrentVar].Stations.Add(TheLine);
                                _WholeList.Add(TheLine);
                            }
                            else
                                continue;
                        }

                    }
                }
            }
        }
        public bool Approved(string Code, int StationType)
        {
            return 
                _Restrictions.ContainsKey(StationType) && 
                _Restrictions[StationType].Stations.Contains(Code);
        }
        public bool IsStrong(int AmurSiteType)
        {
            return
                _Restrictions.ContainsKey(AmurSiteType) &&
                _Restrictions[AmurSiteType].StrongStored;
        }
        public int[] RestrictedTypes()
        {
            return _Restrictions.Keys.ToArray();
        }
        public string[] StationsList()
        {
            return _WholeList.ToArray();
        }

        private struct RestrictionEntry
        {
            public bool StrongStored;
            public HashSet<string> Stations;

            public RestrictionEntry(bool IsStrong)
            {
                StrongStored = IsStrong;
                Stations = new HashSet<string>();
            }
        }
    }
}

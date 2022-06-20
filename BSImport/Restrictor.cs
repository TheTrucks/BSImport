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
        private Dictionary<int, HashSet<string>> _Restrictions;
        private HashSet<string> _WholeList;
        public Restrictor(string Filename)
        {
            Update(Filename);
        }
        public void Update(string Filename)
        {
            _Restrictions = new Dictionary<int, HashSet<string>>();
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
                            CurrentVar = Int32.Parse(TheLine.Trim(new char[] { '[', ']' }));
                        }
                        else
                        {
                            if (CurrentVar > 0)
                            {
                                if (!_Restrictions.ContainsKey(CurrentVar))
                                    _Restrictions.Add(CurrentVar, new HashSet<string>());
                                _Restrictions[CurrentVar].Add(TheLine);
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
            if (_Restrictions.ContainsKey(StationType))
                if (_Restrictions[StationType].Contains(Code))
                    return true;
            return false;
        }
        public string[] StationsList()
        {
            return _WholeList.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSImport.DFOEntity.Entity;
using NHibernate.Linq;
using NHibernate;

namespace BSImport
{
    public static class AmurDFOWorker
    {
        public static List<MeteoData> MeteoDataList(ISession session, DateTime Since, DateTime To)
        {
            return session.Query<MeteoData>()
                .Where(x => x.DateUtc >= Since && x.DateUtc < To)
                .ToList();
        }

        public static List<Station> StationList(ISession session)
        {
            return session.Query<Station>().ToList();
        }
        public static List<Variable> VariableList(ISession session)
        {
            return session.Query<Variable>().ToList();
        }
    }
}

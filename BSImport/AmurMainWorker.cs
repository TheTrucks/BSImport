using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using EntityNH.Entity.Data;
using EntityNH.Entity.Meta;
using EntityNH.Hibernate;

namespace BSImport
{
    public static class AmurMainWorker
    { 
        public static List<DataValue> LoadMeteoData(IStatelessSession session, DateTime Since, DateTime To)
        {
            List<DataValue> Result;
            int[] MeteoVars = System.Configuration.ConfigurationManager.AppSettings["MeteoVars"]
                .Split(new char[] { ',' })
                .Select(x => Int32.Parse
                (
                    x.Trim()
                ))
                .ToArray();
            Result = session.Query<DataValue>()
                    .Where(x => MeteoVars.Contains(x.Catalog.Variable.Id.Value) && x.DateUTC >= Since && x.DateUTC < To)
                    .Fetch(x => x.Catalog)
                    .ThenFetch(x => x.Site)
                    .ThenFetch(x => x.Station)
                    .ToList();
            return Result;
        }
        public static List<Station> LoadStations(IStatelessSession session)
        {
            return null;
        }
    }
}

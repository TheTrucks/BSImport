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
        public static List<DataValue> LoadMeteoData(ISession session, DateTime Since, DateTime To, int[] MeteoVars)
        {
            var DataQuery = session.Query<DataValue>()
                    .Where(x => MeteoVars.Contains(x.Catalog.Variable.Id.Value) && new int[] { 1, 2 }.Contains(x.Catalog.Site.Type.Id.Value) && x.DateUTC >= Since && x.DateUTC < To)
                    .Fetch(x => x.Catalog)
                    .ThenFetch(x => x.Site);
            DataQuery.ThenFetch(x => x.Station).ToFuture();
            DataQuery.ThenFetchMany(x => x.AttrValues).ToFuture(); // not optimal, still ok.

            return DataQuery.ToFuture().ToList();
        }
    }
}

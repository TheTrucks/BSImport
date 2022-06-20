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
        public static List<DataValue> LoadMeteoData(ISession session, DateTime Since, DateTime To, int[] MeteoVars, string[] Codes)
        {
            return session.Query<DataValue>()
                    .Where(x => MeteoVars.Contains(x.Catalog.Variable.Id.Value) 
                        && new int[] { 1, 2 }.Contains(x.Catalog.Site.Type.Id.Value) 
                        && Codes.Contains(x.Catalog.Site.Station.Code)
                        && x.DateUTC >= Since && x.DateUTC < To)
                    .Fetch(x => x.Catalog)
                    .ThenFetch(x => x.Site)
                    .ThenFetch(x => x.Station).ToList();
        }

        public static ILookup<int, SiteAttrValue> LoadSiteAttr(ISession session, string[] Codes)
        {
            return session.Query<SiteAttrValue>()
                .Where(x => Codes.Contains(x.Entity.Station.Code))
                .ToLookup(x => x.Entity.Id.Value);
        }
    }
}

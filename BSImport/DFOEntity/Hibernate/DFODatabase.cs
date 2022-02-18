using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;

namespace BSImport.DFOEntity.Hibernate
{
    public class DFODatabase
    {
        private ISessionFactory _sessionFactory;

        public ISessionFactory SessionFactory
        {
            get
            {
                return _sessionFactory;
            }
        }

        public void Initialize(string databaseConnectionString, int batch_size = 1)
        {
            _sessionFactory = Fluently.Configure()
              .ExposeConfiguration(c => c.SetProperty(NHibernate.Cfg.Environment.WrapResultSets, "true"))
              .Database(
                PostgreSQLConfiguration.Standard.ConnectionString(databaseConnectionString))
              //#if DEBUG
              //                .ExposeConfiguration(c => c.SetInterceptor(new SQLDebugOutput()))
              //#endif
              .Mappings(m => m.FluentMappings
                .AddFromAssemblyOf<Entity.Station>())
                .ExposeConfiguration(cfg => cfg.SetProperty("adonet.batch_size", batch_size.ToString()))
              //  .AddFromAssemblyOf<MaterialMap>()
              //  .AddFromAssemblyOf<MethodMap>()
              //  .AddFromAssemblyOf<PostMap>()
              //  .AddFromAssemblyOf<PoolMap>())
              .BuildSessionFactory();
        }

        public void Drop()
        {
            _sessionFactory.Dispose();
        }
    }
}

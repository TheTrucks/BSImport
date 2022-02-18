using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityNH.Hibernate;
using NHibernate;

namespace BSImport
{
    public static class ConnectionManager
    {
        private static Database _maindb;
        private static DFOEntity.Hibernate.DFODatabase _altdb;

        static ConnectionManager()
        {
            try
            {
                _maindb = new Database();
                    _maindb.Initialize(ConfigurationManager.AppSettings["AmurMain"], 25);
                _altdb = new DFOEntity.Hibernate.DFODatabase();
                    _altdb.Initialize(ConfigurationManager.AppSettings["AmurDFO"], 25);
                LogManager.Log.Info("Databases succesfully initialized");
            }
            catch (Exception Exc)
            {
                LogManager.Log.Error("Error initializing databases:");
                LogManager.Log.Error(Exc.ToString());
            }
        }

        public static ISessionFactory AmurFerhri { get { return _maindb.SessionFactory; } }
        public static ISessionFactory AmurDFO { get { return _altdb.SessionFactory; } }
    }
}

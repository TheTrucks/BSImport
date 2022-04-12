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
            InitMain();
            InitAlt();
        }

        private static void InitMain()
        {
            try
            {
                _maindb = new Database();
                _maindb.Initialize(ConfigurationManager.AppSettings["AmurMain"], 25);
                LogManager.Log.Info("Database main succesfully initialized");
            }
            catch (Exception Exc)
            {
                LogManager.Log.Error("Error initializing main database:");
                LogManager.Log.Error(Exc.ToString());
            }
        }
        private static void InitAlt()
        {
            try
            {
                _altdb = new DFOEntity.Hibernate.DFODatabase();
                _altdb.Initialize(ConfigurationManager.AppSettings["AmurDFO"], 25);
                LogManager.Log.Info("Database alt succesfully initialized");
            }
            catch (Exception Exc)
            {
                LogManager.Log.Error("Error initializing alt database:");
                LogManager.Log.Error(Exc.ToString());
            }
        }

        public static void CloseConnections()
        {
            if (_maindb != null)
                _maindb.Drop();
            if (_altdb != null)
                _altdb.Drop();
        }

        public static ISessionFactory AmurFerhri { 
            get 
            {
                if (_maindb == null || _maindb.SessionFactory.IsClosed)
                    InitMain();
                return _maindb.SessionFactory; 
            } 
        }
        public static ISessionFactory AmurDFO {
            get 
            {
                if (_altdb == null || _altdb.SessionFactory.IsClosed)
                    InitAlt();
                return _altdb.SessionFactory; 
            } 
        }
    }
}

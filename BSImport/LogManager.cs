using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace BSImport
{
    public static class LogManager
    {
        private static NLog.Logger _log;
        static LogManager()
        {
            var NlogConfig = new NLog.Config.LoggingConfiguration();
            var ErrorLog = new NLog.Targets.FileTarget("ErrorLog");
            ErrorLog.FileName = "Logs/${shortdate}/errs.txt";
            var InfoLog = new NLog.Targets.FileTarget("InfoLog");
            InfoLog.FileName = "Logs/${shortdate}/info.txt";
            NlogConfig.AddRuleForOneLevel(LogLevel.Error, ErrorLog);
            NlogConfig.AddRuleForOneLevel(LogLevel.Info, InfoLog);

            NLog.LogManager.Configuration = NlogConfig;
            _log = NLog.LogManager.GetLogger("BSImport");
        }

        public static Logger Log { get { return _log; } }
    }
}

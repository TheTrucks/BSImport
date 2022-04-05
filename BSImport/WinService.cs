using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace BSImport
{
    partial class WinService : ServiceBase
    {
        private IScheduler ImportSchedule;
        private CancellationTokenSource Stopper;
        public WinService()
        {
            this.ServiceName = "DFO Import";
            this.CanStop = true;
            this.CanPauseAndContinue = false;
        }

        private static void Main()
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            ServiceBase.Run((ServiceBase)new WinService());
            using (var Dummy = new Npgsql.NpgsqlConnection()) { } // to make vs copy npgsql.dll when bsimport is dependency
        }

        protected override async void OnStart(string[] args)
        {
            Stopper = new CancellationTokenSource();

            ImportSchedule = await Quartz.Impl.StdSchedulerFactory.GetDefaultScheduler(Stopper.Token);
            await ImportSchedule.Start(Stopper.Token);
            await RebuildJob(Stopper.Token);
        }

        private async Task RebuildJob(CancellationToken StopToken)
        {
            await ImportSchedule.Clear(StopToken);
            IJobDetail ImportJob = JobBuilder.Create<ImporterJob>().StoreDurably().Build();
            await ImportSchedule.AddJob(ImportJob, true, StopToken);

            foreach (var Param in ReadParameters())
            {
                try
                {
                    ITrigger ImportTrigger = TriggerBuilder.Create()
                        .WithIdentity(Param.Cache, "MainGroup")
                        .UsingJobData("Params", Param.Params)
                        .UsingJobData("Stations", Param.Stations)
                        .UsingJobData("Cache", Param.Cache)
                        .UsingJobData("Hours", Param.HoursBack)
                        .WithCronSchedule(Param.CronString)
                        .ForJob(ImportJob)
                        .Build();

                    await ImportSchedule.ScheduleJob(ImportTrigger, Stopper.Token);
                    LogManager.Log.Info($"Scheduled job with parameters: CS: {Param.CronString}, P: {Param.Params}, S: {Param.Stations}, C: {Param.Cache}");
                }
                catch (Exception Ex)
                {
                    LogManager.Log.Error($"Unable to add job with parameters: CS: {Param.CronString}, P: {Param.Params}, S: {Param.Stations}, C: {Param.Cache}");
                    LogManager.Log.Error(Ex.ToString());
                }
            }
        }
        private List<Jobparams> ReadParameters()
        {
            List<Jobparams> Result = new List<Jobparams>();

            using (var FS = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schedule.ff"), FileMode.Open))
            {
                using (var SR = new StreamReader(FS, Encoding.UTF8))
                {
                    while (!SR.EndOfStream)
                    {
                        string TheLine = SR.ReadLine().Trim();
                        if (TheLine == String.Empty || TheLine.StartsWith("#"))
                            continue;

                        string[] ParamsList = new string(TheLine
                            .TakeWhile(x => x != '#').ToArray())
                            .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim()).ToArray();
                        if (ParamsList.Length >= 4)
                        {
                            var Params = new Jobparams
                            {
                                CronString = ParamsList[0],
                                Params = ParamsList[1],
                                Stations = ParamsList[2],
                                Cache = ParamsList[3]
                            };
                            int Hours;
                            if (ParamsList.Length == 5 && Int32.TryParse(ParamsList[4], out Hours))
                                Params.HoursBack = Hours;
                            else
                                Params.HoursBack = 1;

                            Result.Add(Params);
                        }
                    }
                }
            }

            return Result;
        }

        struct Jobparams
        {
            public string CronString;
            public string Params;
            public string Stations;
            public string Cache;
            public int HoursBack;
        }

        protected override void OnStop()
        {
            Stopper.Cancel();
            ConnectionManager.CloseConnections();
            LogManager.Log.Info("Shutting down");
        }
    }
}

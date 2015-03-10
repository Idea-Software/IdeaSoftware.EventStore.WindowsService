using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using IdeaSoftware.EventStore.WindowsService.BackUp.Core;
using NLog;

namespace IdeaSoftware.EventStore.WindowsService
{
    public class ServiceWrapper
    {
        private readonly EsBackupProcessConfig _backupConfig;
        private readonly bool _backupScheduleEnabled;
        private readonly int _hour;
        private readonly int _minute;
        private readonly string _eventStoreExeLocation;
        private Process _esProcess;
        private readonly  Logger _logger;

        private Scheduler _scheduler;

        private readonly DirectoryInfo _baseBackUpDir;

        public ServiceWrapper(EsBackupProcessConfig backupConfig, bool backupScheduleEnabled, int hour, int minute, string eventStoreExeLocation)
        {
            _backupConfig = backupConfig;
            _backupScheduleEnabled = backupScheduleEnabled;
            _hour = hour;
            _minute = minute;
            _eventStoreExeLocation = eventStoreExeLocation;
            _logger = LogManager.GetLogger("ServiceWrapper");
            _baseBackUpDir = new DirectoryInfo(backupConfig.BackupPath.FullName);
        }

        public void Start()
        {
            _logger.Info("Started Service. BackUp Enabled: {0}", _backupScheduleEnabled);

            if (_backupScheduleEnabled)
            {
                _scheduler = new Scheduler();
                _scheduler.DailyAt(_hour, _minute, () =>
                {
                    _backupConfig.BackupPath = _baseBackUpDir.CreateSubdirectory(DateTime.Today.ToString("yyyy-MM-dd"));
                    _logger.Info("Running BackUp. From: {0}. To: {1}", _backupConfig.DataPath.FullName, _backupConfig.BackupPath.FullName);
                    var backupProc = new EsBackupProcess(_backupConfig);
                    backupProc.Backup().Wait();
                    _logger.Info("BackUp Complete");
                });
 
            }
    



            var exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var basePath = new FileInfo(exe).DirectoryName;
            var updater = new SettingsUpdater();

            string workingDir = Path.GetFullPath(Path.Combine(basePath, _eventStoreExeLocation));

            string esExecutable = Path.GetFullPath(Path.Combine(workingDir, "EventStore.ClusterNode.exe"));
            var info = new ProcessStartInfo
            {
                FileName = esExecutable,
                Arguments = updater.GetEsArgs(),
                UseShellExecute = false,
                WorkingDirectory = workingDir
            };

            _esProcess = Process.Start(info);
            _esProcess.EnableRaisingEvents = true;
            _esProcess.Exited += (sender, args) => { if (!_topShelfStopping) throw new ApplicationException("es errored"); };



            if (!WaitForEs(TimeSpan.FromSeconds(30)))
            {
                throw new CouldNotConnectToEventStoreException();
            }
            _logger.Info("Started EventStore process.");

           

        }

        private bool WaitForEs(TimeSpan timeout)
        {
            var sw = new Stopwatch();
            sw.Start();


            var client = new HttpClient();


            while (true)
            {
                try
                {
                    var result =
                        client.SendAsync(new HttpRequestMessage(HttpMethod.Options, new SettingsUpdater().GetHttpUri()))
                            .Result;
                    if (result.StatusCode == HttpStatusCode.OK)
                        return true;
                }
                catch
                {
                    //i dont care
                }
                if (sw.Elapsed > timeout)
                    return false;


            }
        }

        private bool _topShelfStopping;

        public void Stop()
        {

            _logger.Info("Stopping Service.");

            if (_backupScheduleEnabled)
            {
                if (_scheduler != null)
                    _scheduler.End();
            }

            _topShelfStopping = true;
            _esProcess.Refresh();

            if (_esProcess.HasExited) return;

            _esProcess.Kill();
            _esProcess.WaitForExit(TimeSpan.FromSeconds(30).Milliseconds);
            _logger.Info("Stopped EventStore process.");
            _esProcess.Dispose();



        }
    }
}
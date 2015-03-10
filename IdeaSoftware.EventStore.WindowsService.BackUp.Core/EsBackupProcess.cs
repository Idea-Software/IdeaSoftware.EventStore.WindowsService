using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core
{
    public class EsBackupProcess
    {
        private readonly EsBackupProcessConfig _esBackupProcessConfig;
        private Logger _logger;

        public EsBackupProcess(EsBackupProcessConfig esBackupProcessConfig)
        {
            _esBackupProcessConfig = esBackupProcessConfig;
            _logger = LogManager.GetLogger("EsBackupProcess");
        }

        public async Task  Backup()
        {
            try
            {
                _logger.Info("BackUp process started.");
                _esBackupProcessConfig.ValidateDependencies();
                _logger.Info("Copying check files from {0}, to {1}.", _esBackupProcessConfig.DataPath.FullName, _esBackupProcessConfig.BackupPath.FullName);
                OnStageComplete(await new CheckFileCopier().ExecuteAsync(_esBackupProcessConfig.DataPath, _esBackupProcessConfig.BackupPath));
                _logger.Info("Copying data files from {0}, to {1}.", _esBackupProcessConfig.DataPath.FullName, _esBackupProcessConfig.BackupPath.FullName);
                OnStageComplete( await new DataFileCopier().ExecuteAsync(_esBackupProcessConfig.DataPath, _esBackupProcessConfig.BackupPath));
                _logger.Info("BackUp process complete.");
            }
            catch (Exception ex)
            {
                _logger.Fatal(String.Format("Failed to restore {0} service.", _esBackupProcessConfig.EsServiceName), ex);
                throw;
            }
        }


        public async Task Restore(bool restartEventstore = true)
        {
            _logger.Info("Restore process started.");

            using (var service = new ServiceController(_esBackupProcessConfig.EsServiceName))
            {

                try
                {

                    StopService(service, _esBackupProcessConfig.Timeout);

                    _logger.Info("Cleaning destination directory {0}", _esBackupProcessConfig.DataPath.FullName);
                    OnStageComplete(await new DestinationCleaner().ExecuteAsync(_esBackupProcessConfig.BackupPath, _esBackupProcessConfig.DataPath));
                    _logger.Info("Copying truncate files from {0}, to {1}.", _esBackupProcessConfig.BackupPath.FullName, _esBackupProcessConfig.DataPath.FullName);
                    OnStageComplete(await new TruncateFileRestorer().ExecuteAsync(_esBackupProcessConfig.BackupPath, _esBackupProcessConfig.DataPath));
                    _logger.Info("Copying data files from {0}, to {1}.", _esBackupProcessConfig.BackupPath.FullName, _esBackupProcessConfig.DataPath.FullName);
                    OnStageComplete(await new DataFileCopier().ExecuteAsync(_esBackupProcessConfig.BackupPath, _esBackupProcessConfig.DataPath));
                    _logger.Info("Copying other files from {0}, to {1}.", _esBackupProcessConfig.BackupPath.FullName, _esBackupProcessConfig.DataPath.FullName);
                    OnStageComplete(await new OtherFileCopier().ExecuteAsync(_esBackupProcessConfig.BackupPath, _esBackupProcessConfig.DataPath));



                    _logger.Info("Restarting service {0}.", _esBackupProcessConfig.EsServiceName);
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, _esBackupProcessConfig.Timeout);
                    _logger.Info("Service {0} started.", _esBackupProcessConfig.EsServiceName);
                    _logger.Info("Restore process complete.");
                }
                catch (Exception ex)
                {
                    _logger.Fatal(String.Format("Failed to restore {0} service.", _esBackupProcessConfig.EsServiceName), ex);
                    throw;
                }


            }
          

        }

        public static void StopService(ServiceController service, TimeSpan timeout)
        {
            
            var logger = LogManager.GetLogger("EsBackupProcess");

            logger.Info("Service status is {0}", service.Status);

            if (service.Status != ServiceControllerStatus.Stopped)
            {
                logger.Info("Can stop: {0}", service.CanStop);
                if (service.CanStop)
                {
                    logger.Info("Trying to stop: {0}", service.CanStop);
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    logger.Info("Service status is {0}", service.Status);

                    //allow time for any files to be released?
                    Thread.Sleep(1000);
                }
                else
                {
                    throw new ApplicationException("Cannot stop service.");
                }
            }

        }

        public event StageCompletedHandler StageCompleted;

        private void OnStageComplete(EventArgs args)
        {

            _logger.Info("Stage Complete: {0}", args.GetType().Name);
            if (StageCompleted != null)
            {
                StageCompleted.Invoke(this, args);
            }
        }

    }
}

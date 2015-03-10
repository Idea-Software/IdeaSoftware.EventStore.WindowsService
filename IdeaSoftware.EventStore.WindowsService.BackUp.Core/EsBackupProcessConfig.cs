using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core
{
    public class EsBackupProcessConfig
    {
        private readonly DirectoryInfo _dataPath;
        private readonly string _esServiceName;
        private readonly TimeSpan _timeout;

        public EsBackupProcessConfig(string backupPath, string dataPath, string esServiceName, TimeSpan timeout = default(TimeSpan))
        {
            _timeout = timeout == default(TimeSpan) ? TimeSpan.FromSeconds(30) : timeout;
            
            BackupPath = new DirectoryInfo(backupPath);           
            _dataPath = new DirectoryInfo(dataPath);
            _esServiceName = esServiceName;
            ValidateDependencies();

        }

        public DirectoryInfo BackupPath { get; set; }

        public DirectoryInfo DataPath
        {
            get { return _dataPath; }
        }

        public string EsServiceName
        {
            get { return _esServiceName; }
        }

        public TimeSpan Timeout
        {
            get { return _timeout; }
        }


        public void ValidateDependencies()
        {

            if (!BackupPath.Exists)
                BackupPath.Create();

            if (!ServiceController.GetServices().Any(s => s.ServiceName == EsServiceName))
                throw new ArgumentException("Eventstore service name is invalid");
        }
    }
}
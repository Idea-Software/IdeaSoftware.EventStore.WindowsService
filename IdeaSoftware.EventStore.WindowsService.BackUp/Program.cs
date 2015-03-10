using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdeaSoftware.EventStore.WindowsService.BackUp.Core;

namespace IdeaSoftware.EventStore.WindowsService.BackUp
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var p = PowerArgs.Args.Parse<BackupArgs>(args);

            var config = new EsBackupProcessConfig(
                backupPath: p.Destination,
                dataPath:p.Source,
                esServiceName: p.ServiceName);

            var backupProcess = new EsBackupProcess(config);
            backupProcess.Backup().Wait();

        }
    }
}

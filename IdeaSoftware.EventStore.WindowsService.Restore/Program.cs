using IdeaSoftware.EventStore.WindowsService.BackUp.Core;

namespace IdeaSoftware.EventStore.WindowsService.Restore
{
    class Program
    {
        static void Main(string[] args)
        {

            var p = PowerArgs.Args.Parse<RestoreArg>(args);

            var config = new EsBackupProcessConfig(               
                backupPath: p.Source,
                dataPath: p.Destination,
                esServiceName: p.ServiceName);
                                
            var backupProcess = new EsBackupProcess(config);
            backupProcess.Restore().Wait();

        }
    }
}

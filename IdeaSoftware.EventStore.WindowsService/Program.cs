using System;
using System.IO;
using IdeaSoftware.EventStore.WindowsService.BackUp.Core;
using Topshelf;
using Topshelf.ServiceConfigurators;

namespace IdeaSoftware.EventStore.WindowsService
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var settingsUpdater = new SettingsUpdater();
            
            HostFactory.Run(x =>
            {

                x.AddCommandLineDefinition("ip", s =>
                {
                    settingsUpdater["ES:--ext-ip"] = s;
                    settingsUpdater["ES:--int-ip"] = s;
                });
                x.AddCommandLineDefinition("tcpport", s =>
                {
                    settingsUpdater["ES:--int-tcp-port"] = s;
                    settingsUpdater["ES:--ext-tcp-port"] = s;
                });
                x.AddCommandLineDefinition("httpport", s =>
                {
                    settingsUpdater["ES:--int-http-port"] = s;
                    settingsUpdater["ES:--ext-http-port"] = s;
                });
                x.AddCommandLineDefinition("data", s =>
                {
                    settingsUpdater["ES:--db"] = s;
                });
                x.AddCommandLineDefinition("log", s =>
                {
                    settingsUpdater["ES:--log"] = s;
                });
                x.AddCommandLineDefinition("projections", s =>
                {
                    settingsUpdater["ES:--run-projections"] = s;
                });
                x.AddCommandLineDefinition("backupenable", s =>
                {
                    settingsUpdater["BackUp:Enable"] = s;
                });
                x.AddCommandLineDefinition("backuppath", s =>
                {
                    settingsUpdater["BackUp:Path"] = s;
                });
                x.AddCommandLineDefinition("backuphour", s =>
                {
                    settingsUpdater["BackUp:RunsAt"] = s;
                });
                x.AddCommandLineDefinition("backuptimeout", s =>
                {
                    settingsUpdater["BackUp:Timeout"] = s;
                });
                x.AddCommandLineDefinition("esexepath", s =>
                {
                    settingsUpdater["Service:EsExeLocation"] = s;
                });




                x.ApplyCommandLine();




                x.Service((ServiceConfigurator<ServiceWrapper> s) =>
                {


                    s.ConstructUsing(settings =>
                    {


                        var resolvedDataPath = Path.GetFullPath(Path.Combine(settingsUpdater["Service:EsExeLocation"],
                            settingsUpdater["ES:--db"]));

                        var resolvedBackUpPath = Path.GetFullPath(Path.Combine(settingsUpdater["Service:EsExeLocation"],
                            settingsUpdater["BackUp:Path"]));

                        var config = new EsBackupProcessConfig(                            
                            resolvedBackUpPath,
                            resolvedDataPath,
                            settings.ServiceName,
                            TimeSpan.FromSeconds(Int32.Parse(settingsUpdater["BackUp:Timeout"])
                            ));


                        var runsAt = settingsUpdater["BackUp:RunsAt"];
                        var hour = int.Parse(runsAt.Split(':')[0]);
                        var minute = int.Parse(runsAt.Split(':')[1]);
                            

                        return new ServiceWrapper(config, bool.Parse(settingsUpdater["BackUp:Enable"]), hour, minute , settingsUpdater["Service:EsExeLocation"]);
                    });



                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.EnableShutdown();
                x.EnableServiceRecovery(c => c.RestartService(1));

            });

        }

    }
}

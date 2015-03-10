using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using IdeaSoftware.EventStore.WindowsService.BackUp.Core;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core.Tests
{
    [TestFixture]
    [Category("Integration")]
    public class EsBackupProcessShould
    {
        const string WorkingDirectory = @"C:\TEMPTESTS";
        private const string ServiceName = "EventStore-IntegrationTest";
        private const string EsExePath = "../../../EventStore.Binaries";

        private static string ServiceExePath = "../../../IdeaSoftware.EventStore.WindowsService/bin/{0}/IdeaSoftware.EventStore.WindowsService.exe";



        [TestFixtureSetUp]
        public void TestSetup()
        {

            try
            {
                UpdateServicePath();

                var esExecutable = GetEsExecutablePath();
                InstallEventStore(esExecutable);
                StartEventStore();

                if (!Directory.Exists(WorkingDirectory))
                    Directory.CreateDirectory(WorkingDirectory);
            }
            catch (Exception ex)
            {
                TearDownRoutine();
                throw;
            }
           

        }



        [TestFixtureTearDown]
        public void TearDown()
        {
            TearDownRoutine();
        }

        private static void TearDownRoutine()
        {
            if (Directory.Exists(WorkingDirectory))
                Directory.Delete(WorkingDirectory, true);

            string esExecutable = GetEsExecutablePath();
            UninstallEventStore(esExecutable);
        }

        [Test]
        public async Task CopyDotChkFilesFirst_ThenDataFiles_WhenPerformingBackUp()
        {
            var esBackupProcessConfig = GetDefaultConfig();
            var backUpProcess = new EsBackupProcess(esBackupProcessConfig);


            var argsSequence = new List<EventArgs>();
            backUpProcess.StageCompleted += (sender, args) => argsSequence.Add(args);


            await backUpProcess.Backup();

            argsSequence.Count.Should().Be(2);
            argsSequence.First().Should().BeOfType<CheckPointBackupCompletedArgs>();
            argsSequence.Last().Should().BeOfType<DataFilesCopied>();


        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task CleanDestinationDir_ThenCopyDotChkFilesFirst_ThenDataFiles_ThenOtherFiles_WhenPerformingRestore(bool isEventStoreAlreadyStopped)
        {
            if (isEventStoreAlreadyStopped)
                StopEventStore();


            var esBackupProcessConfig = GetDefaultConfig();
            var backUpProcess = new EsBackupProcess(esBackupProcessConfig);

            var argsSequence = new List<EventArgs>();
            await backUpProcess.Backup();

            backUpProcess.StageCompleted += (sender, args) => argsSequence.Add(args);
            await backUpProcess.Restore();

            argsSequence.Count.Should().Be(4);
            argsSequence.First().Should().BeOfType<DestinationCleaningCompleted>();
            argsSequence.ElementAt(1).Should().BeOfType<ChaserRestoreCompleted>();
            argsSequence.ElementAt(2).Should().BeOfType<DataFilesCopied>();
            argsSequence.Last().Should().BeOfType<OtherFilesCopied>();


        }

        [Test]
        [Category("Integration")]
        public void  RestoreSuccsesfully_WhenRequested()
        {
            StartEventStore();
            

            var eventBeforeBackup = new SomeEvent { Id = 1 };
            var eventAfterBackup = new SomeEvent { Id = 2 };

            var stream = string.Format("somestream-{0}", Guid.NewGuid());
            var config = GetDefaultConfig();
            var backupProcess = new EsBackupProcess(config);


            var esCon1 = CreateEsConnection("con1");
            esCon1.ConnectAsync().Wait();

            var events = GetEventsFromTestStream(stream, esCon1).Result;
            events.Should().BeEmpty();



            WriteToTestStream(stream, eventBeforeBackup, esCon1).Wait();

            backupProcess.Backup().Wait();


            WriteToTestStream(stream, eventAfterBackup, esCon1).Wait();
            events = GetEventsFromTestStream(stream, esCon1).Result;

            events.Should().Contain(e => e.Id == eventBeforeBackup.Id);
            events.Should().Contain(e => e.Id == eventAfterBackup.Id);
            esCon1.Close();
           
            backupProcess.Restore().Wait();
            var con2 =  CreateEsConnection("testcn-2");
            con2.ConnectAsync().Wait();
            events = GetEventsFromTestStream(stream, con2).Result;
            events.Should().Contain(e => e.Id == eventBeforeBackup.Id);
            events.Should().NotContain(e => e.Id == eventAfterBackup.Id);
            con2.Close();
            con2.Dispose();



        }

        private static IEventStoreConnection CreateEsConnection(string name = "testcn")
        {
            var esIP = ConfigurationManager.AppSettings["ES:--ext-ip"];
            var esPort = int.Parse(ConfigurationManager.AppSettings["ES:--ext-tcp-port"]);
            var ip = new IPEndPoint(IPAddress.Parse(esIP), esPort);
            var settings = ConnectionSettings.Create()
                .FailOnNoServerResponse()
                .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
            
            var con = EventStoreConnection.Create(settings, ip, name);
            return con;
        }

      

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowAnExceptionWhenEsServiceDoesNotExist()
        {

            var backupdir = Guid.NewGuid().ToString();
            var fullBackupDir = Path.Combine(WorkingDirectory, backupdir);

            var config = new EsBackupProcessConfig( fullBackupDir, ConfigurationManager.AppSettings["ES:--db"], "THISSERVICEDOESNOTEXIST");

        }

        [Test]
        public void CreateBackupLocationIfItDoesntExit()
        {

            var backupdir = Guid.NewGuid().ToString();
            var fullBackupDir = Path.Combine(WorkingDirectory, backupdir);

            Directory.Exists(fullBackupDir).Should().BeFalse();

            var proc = new EsBackupProcess(new EsBackupProcessConfig(fullBackupDir, ConfigurationManager.AppSettings["ES:--db"], ServiceName));


            Directory.Exists(fullBackupDir).Should().BeTrue();

        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void ThrowNotSupportedExceptionIfBackupLocationIsAnInvalidPath()
        {

            var proc = new EsBackupProcess(new EsBackupProcessConfig( @"-THISISCRAP-$$$$:\", ConfigurationManager.AppSettings["ES:--db"], ServiceName));

        }

        [Test]
        public void TimeoutDefaultsTo30Seconds()
        {
            var backupdir = Guid.NewGuid().ToString();
            var fullBackupDir = Path.Combine(WorkingDirectory, backupdir);
            var config = new EsBackupProcessConfig( fullBackupDir, ConfigurationManager.AppSettings["ES:--db"], ServiceName);
            config.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        }

        [TestCase(60)]
        [TestCase(2)]
        public void TimeoutIsSetToProvidedValue(int timeoutInSeconds)
        {
            var backupdir = Guid.NewGuid().ToString();
            var fullBackupDir = Path.Combine(WorkingDirectory, backupdir);
            var config = new EsBackupProcessConfig( fullBackupDir, ConfigurationManager.AppSettings["ES:--db"], ServiceName, TimeSpan.FromSeconds(timeoutInSeconds));
            config.Timeout.Should().Be(TimeSpan.FromSeconds(timeoutInSeconds));
        }



        #region Helpers


        private static void UpdateServicePath()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var dir = new DirectoryInfo(new FileInfo(new Uri(assembly).LocalPath).DirectoryName);
            ServiceExePath = string.Format(ServiceExePath, dir.Name);
        }

        private static void StartEventStore()
        {

            using (var esService = new ServiceController(ServiceName))
            {
                if (esService.Status != ServiceControllerStatus.Running)
                    esService.Start();

                esService.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            }

        }

        private static void InstallEventStore(string esExecutable)
        {
            var installInfo = new ProcessStartInfo
            {
                FileName = esExecutable,
                Arguments =
                    String.Format("install -servicename:{0} -description:{0} -displayname:{0} -esexepath:{1} -tcpport:{2}", ServiceName,
                        EsExePath, int.Parse(ConfigurationManager.AppSettings["ES:--ext-tcp-port"])),
                Verb = "runas"
            };
            Process.Start(installInfo).WaitForExit();

        }

    


        private static string GetEsExecutablePath()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var basePath = new FileInfo(new Uri(assembly).LocalPath).DirectoryName;
            string esExecutable = Path.GetFullPath(Path.Combine(basePath, ServiceExePath));
            return esExecutable;
        }

        private static void UninstallEventStore(string esExecutable)
        {
            StopEventStore();

            var info = new ProcessStartInfo
            {
                FileName = esExecutable,
                Arguments = String.Format("uninstall -servicename:{0}", ServiceName),
                Verb = "runas",                
                
            };

            Process.Start(info).WaitForExit();
            
        }

        private static EsBackupProcessConfig GetDefaultConfig()
        {
            var dataPath = ConfigurationManager.AppSettings["ES:--db"];
            var backupLocation = ConfigurationManager.AppSettings["BackUp:Path"];

            var esBackupProcessConfig = new EsBackupProcessConfig(Path.GetFullPath(Path.Combine(WorkingDirectory, backupLocation)),
                Path.GetFullPath(Path.Combine(EsExePath, dataPath)), ServiceName, TimeSpan.FromSeconds(30));
            return esBackupProcessConfig;
        }
        private static async Task<List<SomeEvent>> GetEventsFromTestStream(string stream, IEventStoreConnection con)
        {

            var slice = await con.ReadStreamEventsForwardAsync(stream, 0, 10, false);
            return slice.Events.Select(e => JsonConvert.DeserializeObject<SomeEvent>(Encoding.UTF8.GetString(e.OriginalEvent.Data))).ToList();
        }

        private static async Task WriteToTestStream(string stream, SomeEvent someEvent, IEventStoreConnection con)
        {
            var json = JsonConvert.SerializeObject(someEvent);
            var bytes = Encoding.UTF8.GetBytes(json);

            await con.AppendToStreamAsync(stream, ExpectedVersion.Any, new List<EventData>
            {
                new EventData(Guid.NewGuid(), someEvent.GetType().Name, true, bytes, null)
            });
        }

        private static void StopEventStore()
        {
            using (var esService = new ServiceController(ServiceName))
            {
                EsBackupProcess.StopService(esService, TimeSpan.FromSeconds(30));
            }
            //allow time for any files to be released?
            Thread.Sleep(1000);
        }

        #endregion
    }
}

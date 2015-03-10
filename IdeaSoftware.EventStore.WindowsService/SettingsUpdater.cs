using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace IdeaSoftware.EventStore.WindowsService
{
    public class SettingsUpdater
    {


        public string this[string key]
        {
            get { return ConfigurationManager.AppSettings[key]; }
            set
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings[key].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
            }
        }

        public string Convert(Dictionary<string, string> args)
        {
            var sb = new StringBuilder();

            foreach (var key in args.Keys)
            {
                sb.AppendFormat("{0}={1} ", key, args[key]);
            }

            return sb.ToString();
        }
        public Dictionary<string, string> Convert(string args)
        {
            return args.Trim().Split(' ')
                    .ToList()
                    .Select(s => new { key = s.Split('=')[0], value = s.Split('=')[1] })
                    .ToDictionary(t => t.key, t => t.value);
        }


        public string GetEsArgs()
        {
            return Convert(ConfigurationManager.AppSettings.AllKeys.Where(s => s.StartsWith("ES:"))
                 .ToDictionary(k => k.Split(':')[1], k => ConfigurationManager.AppSettings[k]));

        }
        public Dictionary<string, string> GetBackupSettings()
        {
            return Convert(ConfigurationManager.AppSettings["ES:Args"]);

        }


        public void StoreEsArgs(Dictionary<string, string> args)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["ES:Args"].Value = Convert(args);
            config.Save(ConfigurationSaveMode.Modified);
        }


        public Uri GetHttpUri()
        {

            return new Uri(string.Format("http://{0}:{1}", ConfigurationManager.AppSettings["ES:--int-ip"], ConfigurationManager.AppSettings["ES:--int-http-port"]));
        }
    }
}
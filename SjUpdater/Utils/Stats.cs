using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Amib.Threading;
using RestSharp;

namespace SjUpdater.Utils
{
    public class Stats
    {
        public class SimpleResponse<T>
        {
            public T Value { get; set; }
        }

        public enum TrackActivity
        {
            AppStart,
            AppTerm,
            AppUpdate
        };

        public static string StatsUrl;
        public static bool AllowCustom;

        public static void TrackAction(TrackActivity action, string comment = null)
        {
            StaticInstance.ThreadPool.QueueWorkItem(delegate
            {
                try
                {
                    var client = new RestClient(StatsUrl);

                    var request = new RestRequest("trackAction", Method.GET);
                    request.AddHeader("Accept", "application/xml");

                    request.AddParameter("id", getUniqueID());
                    request.AddParameter("version", GetVersionString());
                    request.AddParameter("action", action.ToString());
                    if (!string.IsNullOrEmpty(comment))
                    {
                        request.AddParameter("comment", comment);
                    }

                    var response = client.Execute<SimpleResponse<string>>(request);

                    if (response.Data.Value == "ok")
                    {
                        //good
                    }
                }
                catch
                {
                    //sorry
                }
            }, true, WorkItemPriority.Lowest);
        }

        public static void TrackCustomVariable(string key, object value, string comment = null)
        {
            if (!AllowCustom) return;

            StaticInstance.ThreadPool.QueueWorkItem(delegate
            {
                try
                {
                    var client = new RestClient(StatsUrl);

                    var request = new RestRequest("trackCustomVariable", Method.GET);
                    request.AddHeader("Accept", "application/xml");

                    request.AddParameter("id", getUniqueID());
                    request.AddParameter("version", GetVersionString());
                    request.AddParameter("key", key);
                    request.AddParameter("value", SimpleJson.SerializeObject(value));
                    if (!string.IsNullOrEmpty(comment))
                    {
                        request.AddParameter("comment", comment);
                    }

                    var response = client.Execute<SimpleResponse<string>>(request);

                    if (response.Data.Value == "ok")
                    {
                        //good
                    }
                }
                catch
                {
                    //sorry
                }
            }, true, WorkItemPriority.Lowest);
        }

        public static string GetVersionString()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            var sli = new List<string>();
            sli.Add(version.Major.ToString());

            if (version.Minor != 0 || version.Revision != 0 || version.Build != 0)
            {
                sli.Add(version.Minor.ToString());
                if (version.Revision != 0 || version.Build != 0)
                {
                    sli.Add(version.Build.ToString());
                    if (version.Revision != 0)
                    {
                        sli.Add(version.Revision.ToString());
                    }
                }
            }

            return "v" + string.Join(".", sli);
        }

        private static string getUniqueID()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SjUpdater", "uid.uid");

            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (!File.Exists(path) || File.ReadAllText(path).Length != 16)
            {
                var random = new RNGCryptoServiceProvider();
                var data = new byte[8];
                random.GetBytes(data);
                var uid = BitConverter.ToString(data).ToLower().Replace("-", "");

                File.WriteAllText(path, uid);
            }

            return File.ReadAllText(path);
        }

        public static string GetInfoForUser()
        {
            var sample = new StringBuilder();
            sample.AppendLine("A sample of what we collect:\n");
            sample.AppendLine("id: " + getUniqueID() + "    (yes, that's your id)");
            sample.AppendLine("version: " + GetVersionString());
            sample.AppendLine("action: " + TrackActivity.AppStart);

            /*sample.AppendLine("\nAnd if you have that Checkbox enabled, we also collect:");
            var shows = Settings.Instance.TvShows.Select(s => s.Name);
            sample.Append("shows: ");

            foreach (var show in shows)
            {
                sample.Append(show + ", ");
            }
            sample.Remove(sample.Length - 1, 1);*/

            sample.Append("\n\nWe are collecting this data because it's interesting for us seeing how many people we reach and how many have updated to latest version.\n\n" +
                          /*"Even though we collect which shows you have added, we don't analyze that (as it is kind of uselss) and probably remove it next release.\n\n" +*/
                          "Using a ID which is completely randomly generated, it's impossible for us to identify you. We need it to count unique users.");

            return sample.ToString();
        }
    }
}

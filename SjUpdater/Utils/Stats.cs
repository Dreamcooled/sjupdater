using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using RestSharp;
using SjUpdater.Updater;
using DataFormat = RestSharp.DataFormat;

namespace SjUpdater.Utils
{
    public class Stats
    {

        public class SimpleResponse<T> {

            public T Value { get; set; }
        }

        public enum TrackActivity
        {
            AppStart,
            AppTerm,
            AppUpdate,
            ShowAdd,
            Download,
            Review,
            Browse,
            Filter
        };

        public static String StatsUrl;
        public static bool AllowCustom;

        public static void TrackAction(TrackActivity action, String comment=null)
        {
            try
            {
                var client = new RestClient(StatsUrl);

                var request = new RestRequest("trackAction", Method.GET);
                request.AddHeader("Accept", "application/xml");

                request.AddParameter("id", getUniqueID());
                request.AddParameter("version", GetVersionString());
                request.AddParameter("action", action.ToString());
                if (!String.IsNullOrEmpty(comment))
                {
                    request.AddParameter("comment", comment);
                }

                var response = client.Execute<SimpleResponse<String>>(request);

                if (response.Data.Value == "ok")
                {
                    //good
                }
            }
            catch
            {
                //sorry
            }

        }

        public static void TrackCustomVariable(String key, object value, String comment=null)
        {
            if (!AllowCustom) return;
            try
            {
                var client = new RestClient(StatsUrl);

                var request = new RestRequest("trackCustomVariable", Method.GET);
                request.AddHeader("Accept", "application/xml");

                request.AddParameter("id", getUniqueID());
                request.AddParameter("version", GetVersionString());
                request.AddParameter("key", key);
                request.AddParameter("value", SimpleJson.SerializeObject(value));
                if (!String.IsNullOrEmpty(comment))
                {
                    request.AddParameter("comment", comment);
                }

                var response = client.Execute<SimpleResponse<String>>(request);

                if (response.Data.Value == "ok")
                {
                    //good
                }
            }
            catch 
            {
                //sorry
            }
          
        }

        public static string GetVersionString()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            var sli  = new List<String>();
            sli.Add(v.Major.ToString());
            if (v.Minor != 0 || v.Revision != 0 || v.Build != 0)
            {
                sli.Add(v.Minor.ToString());
                if (v.Revision != 0 || v.Build != 0)
                {
                    sli.Add(v.Build.ToString());
                    if (v.Revision != 0)
                    {
                        sli.Add(v.Revision.ToString());
                    }
                }
            }
            return "v"+String.Join(".", sli);
        }

        private static string getUniqueID()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SjUpdater", "uid.uid");

            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (!File.Exists(path) || File.ReadAllText(path).Length != 16)
            {
                RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
                byte[] data = new byte[8];
                random.GetBytes(data);
                string uid = BitConverter.ToString(data).ToLower().Replace("-", "");

                File.WriteAllText(path, uid);
            }

            return File.ReadAllText(path);
        }
    }
}

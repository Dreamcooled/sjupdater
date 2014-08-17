using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Documents;

namespace SjUpdater.Utils
{
    public class Stats
    {

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

        public static void SendStats(bool sendPersonalData)
        {
            try
            {
                string uid = BitConverter.ToString(new Crc64Iso().ComputeHash(Encoding.ASCII.GetBytes(getUniqueID()))).ToLower().Replace("-", "");

                HttpWebRequest request = HttpWebRequest.CreateHttp("http://sjupdater.batrick.de/stats/");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.AllowAutoRedirect = true;
                request.KeepAlive = false;

                string data = string.Format("uid={0}&version={1}", uid,GetVersionString());
                if (sendPersonalData)
                {
                    data += "&shows=" + String.Join(",", Settings.Instance.TvShows.Select(f => f.Name));
                }


                byte[] postData = Encoding.UTF8.GetBytes(data);

                request.ContentLength = postData.Length;

                request.GetRequestStream().Write(postData, 0, postData.Length);
                request.GetResponse().Close();
            }
            catch
            {
                // :'(
            }

        }

        private static string getUniqueID()
        {
            if (Properties.Settings.Default.uid.Length != 16)
            {
                RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
                byte[] data = new byte[8];
                random.GetBytes(data);
                string uid = BitConverter.ToString(data).ToLower().Replace("-", "");
                Properties.Settings.Default.uid = uid;
                Properties.Settings.Default.Save();
            }

            return Properties.Settings.Default.uid;
        }
    }
}

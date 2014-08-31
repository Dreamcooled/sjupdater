using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using RestSharp;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater
{
    public static class SjInfo
    {
        private static readonly ObservableCollection<KeyValuePair<String, String>> ListShows = new ObservableCollection<KeyValuePair<string, string>>();
        public static ObservableCollection<KeyValuePair<String, String>> Shows
        {
            get { return ListShows; }
        }

        static string CleanText(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    if (Char.IsLetterOrDigit(c) || c == ' ')
                        stringBuilder.Append(c);
                    else
                        stringBuilder.Append(' ');
                }
            }

            return new Regex(@"[ ]{2,}", RegexOptions.None).Replace(stringBuilder.ToString().Normalize(NormalizationForm.FormC),@" ").Trim();

        }


        public static String SearchSjDe(String Title)
        {
            Title = CleanText(Title);
            var words = Title.Split(new[] {' '});
            var queries = new List<String>();
            String longest = "";
            foreach (string word in words)
            {
                longest += " "+word;
                queries.Add(longest.Trim());
            }

            queries.Reverse();

            foreach (string query in queries)
            {
                String url = "http://www.serienjunkies.de/i/autocomplete.php?q=" + WebUtility.UrlEncode(query);
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Proxy = null;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64)";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StreamReader responseReader = new StreamReader(response.GetResponseStream());
                string responseContent = responseReader.ReadToEnd();
                responseReader.Close();
                response.Close();
                try
                {
                    JsonObject d = SimpleJson.DeserializeObject(responseContent) as JsonObject;
                    if (d["items"] != null)
                    {
                        JsonArray a = d["items"] as JsonArray;
                        foreach (object o in a)
                        {
                            JsonObject show = o as JsonObject;
                            bool b_allfound = words.All(word => ((String) show["text"]).ToLower().Contains(word.ToLower()) || ((String) show["info"]).ToLower().Contains(word.ToLower()));
                            if (b_allfound)
                            {
                                return "http://www.serienjunkies.de" + (String) show["link"];
                            }
                        }
                    }
                    
                }
                catch (Exception)
                { 
                }
              
            }
            return "";
        }


        public static SjDeReview ParseSjDeSite(String SjDeUrl , int season, int episode)
        {
            if (String.IsNullOrWhiteSpace(SjDeUrl) || season==-1 || episode ==-1)
            {
                return null;
            }
            String url = SjDeUrl + "/reviews/";

            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Proxy = null;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64)";
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            StreamReader responseReader = new StreamReader(response.GetResponseStream());
            string responseContent = responseReader.ReadToEnd();
            responseReader.Close();
            response.Close();
       

            Regex r = new Regex( "<div[^>]+class\\s*=\\s*\"serienreviewliste\"[^>]*>\\s*<a[^>]+href\\s*=\\s*\"([^\"]+)\"[^>]*>\\s*<img[^>]+src\\s*=\\s*\"([^\"]+)\"[^>]*>\\s*</a>\\s*<a[^>]+>\\s*([^<]+?)\\s*</a>\\s*\\((\\d+)\\D(\\d+)\\)");
            MatchCollection mc = r.Matches(responseContent);
            foreach (Match match in mc)
            {
                int seasonNr= int.Parse(match.Groups[4].Value);
                int episodeNr = int.Parse(match.Groups[5].Value);
                if (seasonNr == season && episodeNr == episode)
                {
                    var ret = new SjDeReview();
                    ret.Name = match.Groups[3].Value;
                    ret.ReviewUrl = "http://www.serienjunkies.de" + match.Groups[1].Value;
                    ret.Thumbnail = match.Groups[2].Value;
                    return ret;
                }
            }
            return null;
        }




        public static List<KeyValuePair<String, String>> SearchSjOrg(string Title)
        {
            byte[] data = Encoding.ASCII.GetBytes(WebUtility.HtmlEncode("string=" + Title));

            HttpWebRequest request = WebRequest.CreateHttp("http://serienjunkies.org/media/ajax/search/search.php");
            request.Proxy = null;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data,0,data.Length);
            requestStream.Close();

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            StreamReader responseReader = new StreamReader(response.GetResponseStream());
            string responseContent = responseReader.ReadToEnd();
            responseReader.Close();
            response.Close();

            Regex regex = new Regex(@"\[(\d+),""(.*?)""\]");
            MatchCollection matches = regex.Matches(responseContent);

            List<KeyValuePair<string, string>> resultList = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < matches.Count; i++)
            {
                resultList.Add(new KeyValuePair<string, string>(WebUtility.HtmlDecode(matches[i].Groups[2].Value),
                    "http://serienjunkies.org/?cat=" + matches[i].Groups[1].Value));
            }

            return resultList;
        }

        public static List<DownloadData> ParseSjOrgSite(ShowData showData, out string firstcover, UploadCache uploadCache = null)
        {
            if(showData==null || String.IsNullOrWhiteSpace(showData.Url) ||  String.IsNullOrWhiteSpace(showData.Name)) 
                throw new ArgumentNullException();
            string nextpage = showData.Url;
            firstcover = "";
            var episodes = new List<DownloadData>();
            do
            {
                string cover;
                var a = ParseSite(showData, nextpage, out nextpage, out cover,uploadCache);
                if (firstcover == "")
                {
                    firstcover = cover;
                }
                episodes.AddRange(a);
            } while (nextpage != "");
            return episodes;
        }

        private static List<DownloadData> ParseSite(ShowData showData, string url, out string nextpageurl, out string firstcover, UploadCache uploadCache)
        {
            nextpageurl = "";
            firstcover = "";

            HttpWebRequest req = HttpWebRequest.CreateHttp(url);
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Proxy = null;
            var resp = req.GetResponse();
            BufferedStream buffStream = new BufferedStream(req.GetResponse().GetResponseStream());
            url = resp.ResponseUri.ToString();
            StreamReader reader = new StreamReader(buffStream);

            bool inContent = false;
            bool inPost = false;
            bool inPostContent = false;
            List<DownloadData> list = new List<DownloadData>();
            Match m;
            SeasonData seasonData=null;
            UploadData uploadData=null;
           // int season = -1;



            while (!reader.EndOfStream)
            {
                String line = reader.ReadLine();
                if (inContent)
                {
                    if (inPost)
                    {
                        if (inPostContent)
                        {
                            //Debug.Assert(seasonData!=null);
                            if (seasonData == null) 
                            {
                                Console.WriteLine("Warning: Invalid Html received while parsing "+showData.Name+". Trying to continue");
                                continue;
                            }
                            if ((m = new Regex("<p>\\s*<strong>\\s*(.+?)\\s*</strong>\\s*[^/]*\\s*<br\\s?/>").Match(line)).Success)
                            {
                                //Debug.Assert(uploadData != null);
                                if (uploadData == null) 
                                {
                                    Console.WriteLine("Warning: Invalid Html received while parsing " + showData.Name + ". Trying to continue");
                                    continue;
                                } 
                                string title = WebUtility.HtmlDecode(m.Groups[1].Value);
                                /*int episode = -1;
                                if (season != -1)
                                {
                                    Match m1 = new Regex("S0{0,4}" + season + "E(\\d+)", RegexOptions.IgnoreCase).Match(title);
                                    if (m1.Success)
                                    {
                                        int.TryParse(m1.Groups[1].Value, out episode);
                                    }
                                }*/

                                var downloads = new Dictionary<string, string>();
                                Regex r = new Regex("<a\\s+href\\s*=\"([^\"]+)\".+\\s+(.+?)\\s*<");
                                while(true)
                                {
                                    line = reader.ReadLine();
                                    Match m2 = r.Match(line);
                                    if (m2.Success)
                                    {
                                        String keyOrg = WebUtility.HtmlDecode(m2.Groups[2].Value);
                                        String key = keyOrg;
                                        while(downloads.ContainsKey(key))
                                        {
                                            Match mx = new Regex("\\((\\d+)\\)$").Match(key);
                                            int num = 1;
                                            if (mx.Success)
                                            {
                                                num = int.Parse(mx.Groups[1].Value);
                                            }

                                            key = keyOrg + "(" + ++num + ")";
                                        }
                                        String val = m2.Groups[1].Value;
                                        if (val != null && !String.IsNullOrWhiteSpace(val) && val.StartsWith("http://"))
                                        {
                                            downloads.Add(key, val);
                                        }
                                        else
                                        {
                                            //ignoring invalid download
                                        }
                                    }
                                    if (line.Contains("</p>"))
                                        break;
                                }

         
                            
                                if (title.Contains("720p") )
                                {
                                    uploadData.Format= "720p";
                                }
                                else if (title.Contains("1080p") )
                                {
                                    uploadData.Format = "1080p";
                                }
                                else if (title.Contains("720i"))
                                {
                                    uploadData.Format = "720i";
                                }
                                else if (title.Contains("1080i"))
                                {
                                    uploadData.Format = "1080i";
                                }


                                DownloadData dd = new DownloadData();
                                dd.Upload = uploadCache == null ? uploadData : uploadCache.GetUniqueUploadData(uploadData);


                                //ed.EpisodeN = episode;
                                dd.Title = title;

                                foreach (var download in downloads)
                                {
                                    dd.Links.Add(download.Key,download.Value);
                                }

                                list.Add(dd);


                            }
                            else if ((m = new Regex("(?:(?:<p(?:\\s+style\\s*\\=\\s*\\\"[^\\\"]+\\\"\\s*)?>)|(?:<div\\s+class\\s*=\\s*\"info\">))\\s*(.*?(?:Dauer|Größe|Sprache|Format|Uploader).*?)\\s*(?:(?:</p>)|(?:</div>))").Match(line)).Success || 
                                ((m = new Regex("<p\\s+style\\s*\\=\\s*\\\"[^\\\"]+\\\"\\s*>").Match(line)).Success && ((line=reader.ReadLine())!="") && (m = new Regex("\\s*(.*?(?:Dauer|Größe|Sprache|Format|Uploader).*?)\\s*</p>").Match(line)).Success))
                            {
                                /*
                                 * Nice case:
                                 <p><strong>Dauer:</strong> 20:00 | <strong>Größe:</strong> 175 MB | <strong>Sprache:</strong> Englisch &amp; deutsche Untertitel | <strong>Format:</strong> XviD | <strong>HQ-Cover:</strong> <a href="http://justpic.info/?s=cover&amp;id=&amp;name=&amp;keyword=VGhlIEJpZyBCYW5nIFRoZW9yeQ,,">Download</a> | <strong>Uploader:</strong> block06</p>
                                 
                                 * Bad case: (note newline!!)
                                  <p style="background-color: #f0f0f0;">
                                    <strong>Dauer:</strong> 20:00 | <strong>Größe:</strong> 175 MB | <strong>Sprache:</strong> Englisch | <strong>Format:</strong> XviD | <strong>HQ-Cover:</strong> <a href="http://justpic.info/?s=cover&#038;id=&#038;name=&#038;keyword=VGhlIEJpZyBCYW5nIFRoZW9yeQ,,">Download</a> | <strong>Uploader:</strong> block06</p>
                                 */

                                uploadData = new UploadData();
                                uploadData.Season = seasonData;

                                String c = WebUtility.HtmlDecode(m.Groups[1].Value);
                                MatchCollection mc = new Regex("<strong>\\s*(.+?)\\s*</strong>\\s*(.+?)\\s*(?:\\||$)").Matches(c);
                                foreach (Match match in mc)
                                {
                                    String key = match.Groups[1].Value.ToLower();
                                    String value = match.Groups[2].Value;
                                    if (key.Contains("dauer") || key.Contains("runtime") || key.Contains("duration"))
                                    {
                                        uploadData.Runtime = value;
                                    }
                                    else if (key.Contains("grösse") || key.Contains("größe") || key.Contains("size"))
                                    {
                                        uploadData.Size =value;
                                    }
                                    else if (key.Contains("uploader"))
                                    {
                                        uploadData.Uploader = value;
                                    }
                                    else if (key.Contains("format"))
                                    {
                                        uploadData.Format = value;
                                    }
                                    else if (key.Contains("sprache") || key.Contains("language"))
                                    {
                                        value = value.ToLower();
                                        if (value.Contains("deutsch") || value.Contains("german"))
                                        {
                                            uploadData.Language |= UploadLanguage.German;
                                        }
                                        if (value.Contains("englisch") || value.Contains("english"))
                                        {
                                            uploadData.Language |= UploadLanguage.English;
                                        }
                                    }
                                }
                            }
                            else if ((m = new Regex("<p>\\s*([^<]+)\\s*</p>").Match(line)).Success)
                            {
                                if (seasonData.Description == "")
                                {
                                    seasonData.Description = WebUtility.HtmlDecode(m.Groups[1].Value);
                                }
                            } 
                            else if ((m = new Regex("<p>\\s*<img\\s+src\\s*=\"([^\"]+)\".*?/>\\s*</p>").Match(line)).Success)
                            {
                                seasonData.CoverUrl = m.Groups[1].Value;
                                if (firstcover == "")
                                {
                                    firstcover = seasonData.CoverUrl;
                                }
                            } 
                            else if (new Regex("</div>").Match(line).Success)
                            {
                                inPostContent = false;
                                seasonData = null;
                                uploadData = null;
                            }
                       
       
                        }
                        else if ((m = new Regex("<h2>\\s*<a\\s+href\\s*=\"([^\"]+)\".*?>(.+?)</a>\\s*</h2>").Match(line)).Success)
                        {
                            seasonData = new SeasonData();
                            seasonData.Show = showData;

                            seasonData.Url= m.Groups[1].Value;
                            seasonData.Title = WebUtility.HtmlDecode(m.Groups[2].Value);
                            /*season = -1;
                            Match m2 = new Regex("(?:season|staffel)\\s*(\\d+)", RegexOptions.IgnoreCase).Match(seasonData.Title);
                            if (m2.Success)
                            {
                                int.TryParse(m2.Groups[1].Value,out season);
                            }*/

                        } 
                        else if (new Regex("<div\\s+class\\s*=\\s*\"post-content\"\\s*>").Match(line).Success)
                        {
                            inPostContent = true;
                        } 
                        else if (new Regex("</div>").Match(line).Success)
                        {
                            inPost = false;
                        }
                    }
                    else if (new Regex("<div\\s+class\\s*=\\s*\"post\"\\s*>").Match(line).Success)
                    {
                        inPost = true;
                    } 
                    else 
                    {
                        if ((m=new Regex("<span\\s+class\\s*=\\s*'page\\s+current'>\\s*(\\d+)\\s*</span>").Match(line)).Success)
                        {
                            int currentPage = int.Parse(m.Groups[1].Value);
                            int nextPage = currentPage + 1;
                            if (new Regex("title\\s*='"+ nextPage + "'").Match(line).Success)
                            {
                                if (new Regex("/page/" + currentPage + "/?$").Match(url).Success)
                                {
                                    nextpageurl = url.Replace("page/" + currentPage, "page/" + nextPage);
                                }
                                else
                                {
                                    nextpageurl = url;
                                    if (!nextpageurl.EndsWith("/"))
                                    {
                                        nextpageurl += "/";
                                    }
                                    nextpageurl += "page/" + nextPage + "/";
                                }
                            }



                           
                        }
                        if (new Regex("</div>").Match(line).Success)
                        {
                            inContent = false;
                        }
                    }
                }
                else if(new Regex("<div\\s+id\\s*=\\s*\"content\"\\s*>").Match(line).Success)
                {
                    inContent = true;
                }
            }
            reader.Close();
            return list;
        }




        /*public static ImageSource GetNewestCover(String urlOrName)
        {
            String url="";
            if (new Regex("serienjunkies\\.org/serie/.+").Match(urlOrName).Success)
            {
                url = urlOrName;
            }
            else
            {
                foreach (KeyValuePair<string, string> pair in ListShows)
                {
                    if (pair.Key == urlOrName)
                    {
                        url = pair.Value;
                        break;
                    }
                }
            }
            if (String.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Invalid Url or Tvshow name");
            }


            HttpWebRequest req = HttpWebRequest.CreateHttp(url);
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Proxy = null;
            BufferedStream buffStream = new BufferedStream(req.GetResponse().GetResponseStream());
            StreamReader reader = new StreamReader(buffStream);
            Regex r = new Regex("<li\\s+class\\s*=\\s*\"cat-item.*?href=\"([^\"]+)\".*?>(.*?)</a>");
            return null;
        }*/

    }
}

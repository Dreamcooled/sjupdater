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
using System.Windows.Controls;
using HtmlAgilityPack;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater
{
    public static class SjInfo
    {

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
       

        public static List<KeyValuePair<String, String>> SearchSjOrg(string Title)
        {
            try
            {


                byte[] data = Encoding.ASCII.GetBytes(WebUtility.HtmlEncode("string=" + Title));

                HttpWebRequest request = WebRequest.CreateHttp("http://serienjunkies.org/media/ajax/search/search.php");
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
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
            catch (Exception ex)
            {
                Console.WriteLine("Show Search Request failed. No internet connection?\n" + ex.ToString());
                return new List<KeyValuePair<string, string>>();
            }
        }

        public static List<DownloadData> ParseSjOrgSite(ShowData showData, out string firstcover, UploadCache uploadCache = null)
        {
            if(String.IsNullOrWhiteSpace(showData?.Url) || String.IsNullOrWhiteSpace(showData.Name)) 
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

            WebResponse resp = null;
        
            for (int i = 0; i <= 7; i++)
            {
                try
                {
                    HttpWebRequest req = WebRequest.CreateHttp(url);
                    req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    req.KeepAlive = false;
                    resp = req.GetResponse();
                    break;
                }
                catch (WebException ex) 
                {
                    if (ex.Status != WebExceptionStatus.Timeout)
                        throw;
                } 
            }
            if (resp == null)
            {
                throw new TimeoutException();
            }


            HtmlDocument doc = new HtmlDocument();
            doc.OptionDefaultStreamEncoding=Encoding.UTF8;
            doc.Load(resp.GetResponseStream());
            resp.Dispose();
            List<DownloadData> list = new List<DownloadData>();

            HtmlNode content = doc.GetElementbyId("content");

            HtmlNode nextLink = content.SelectSingleNode("//div[@class='navigation']//a[@href][@class='next']");
            if (nextLink != null)
            {
                nextpageurl = nextLink.GetAttributeValue("href", null);
            }

            var posts = content.SelectNodes("div[@class='post']");
            if (posts == null) return list;
            foreach (var post in posts)
            {
                //--------------Season Title-----------------------------------
                var title = post.SelectSingleNode("h2/a[@href]");
                if (title == null)
                {
                    Console.WriteLine("SjInfo Parser: No Title");
                    continue;
                }
                var seasonData = new SeasonData();
                seasonData.Show = showData;
                seasonData.Url = title.GetAttributeValue("href", null);
                seasonData.Title = WebUtility.HtmlDecode(title.InnerText);


                var postContent = post.SelectSingleNode("div[@class='post-content']");
                if(postContent == null)
                {
                    Console.WriteLine("SjInfo Parser: No Post content");
                    continue;
                }

                //----------------Season Cover------------------------------------------------
                var cover = postContent.SelectSingleNode(".//p/img[@src]");
                if (cover != null)
                {
                    seasonData.CoverUrl = cover.GetAttributeValue("src", null);
                    if (String.IsNullOrEmpty(firstcover))
                    {
                        firstcover = seasonData.CoverUrl;
                    }
                }

                //----------------Season description-----------------------------------------
                var desc = postContent.SelectSingleNode(".//p[count(node())=1][not(@class='post-info-co')]/text()");
                if (desc != null)
                {
                    seasonData.Description = WebUtility.HtmlDecode(desc.InnerText);
                }

                UploadData uploadData = null;
           
                var ps = postContent.SelectNodes(".//node()[self::p|self::div][count(strong)>=2]");
                if (ps == null)
                {
                    Console.WriteLine("SjInfo Parser: no uploads/headers");
                    continue;
                }

                foreach (var p in ps)
                {
                    //--------------- Upload Header ------------------------------
                    if (p.SelectSingleNode("self::node()[not(./a[@target])]") != null)
                    {
                        uploadData = new UploadData();
                        uploadData.Season = seasonData;

                        String c = WebUtility.HtmlDecode(p.InnerHtml);
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
                                uploadData.Size = value;
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
                                if (value.Contains("subbed"))
                                {
                                    uploadData.Subbed = true;
                                }
                            }
                        }
                    } else if (uploadData != null) {
                        // ------------------ Links -------------------------
                        var ulTitle = p.SelectSingleNode("strong[position()=1][count(node())=1]/text()");
                        if (ulTitle == null)
                        {
                            Console.WriteLine("SjInfo Parser: No title for link? " + p.InnerHtml);
                            continue;
                        }
                        string titleStr = WebUtility.HtmlDecode(ulTitle.InnerText).Trim();

                        var links = p.SelectNodes("a[@href][following-sibling::text()]");
                        if (links == null) continue;
                        var downloads = new Dictionary<string, string>();
                        foreach (var link in links)
                        {
                            string ur = link.GetAttributeValue("href", null);
                            string keyOrg = WebUtility.HtmlDecode(link.NextSibling.InnerText.Trim());
                            if(keyOrg.StartsWith("|")) keyOrg = keyOrg.Substring(1).Trim();

                            String key = keyOrg;
                            int i = 1;
                            while (downloads.ContainsKey(key))
                            {
                                key = keyOrg + "(" + i++ + ")";
                            }
                            downloads.Add(key, ur);
                        }

                        if (titleStr.Contains("720p"))
                        {
                            uploadData.Format = "720p";
                        }
                        else if (titleStr.Contains("1080p"))
                        {
                            uploadData.Format = "1080p";
                        }
                        else if (titleStr.Contains("720i"))
                        {
                            uploadData.Format = "720i";
                        }
                        else if (titleStr.Contains("1080i"))
                        {
                            uploadData.Format = "1080i";
                        }

                        DownloadData dd = new DownloadData();
                        dd.Upload = uploadCache == null ? uploadData : uploadCache.GetUniqueUploadData(uploadData);
                        dd.Title = titleStr;

                        if (titleStr.ToLower().Contains("subbed"))
                        {
                            dd.Upload.Subbed = true;
                        }

                        foreach (var download in downloads)
                        {
                            dd.Links.Add(download.Key, download.Value);
                        }

                        list.Add(dd);
                    }
                    else
                    {
                        Console.WriteLine("SjInfo Parser: UploadData was null");
                    }
                }
            }
            return list;
        }


       /* private static List<DownloadData> ParseSiteOld(ShowData showData, string url, out string nextpageurl, out string firstcover, UploadCache uploadCache)
        {
            nextpageurl = "";
            firstcover = "";

            WebResponse resp = null;

            for (int i = 0; i <= 7; i++)
            {
                try
                {
                    HttpWebRequest req = HttpWebRequest.CreateHttp(url);
                    req.Timeout = 4000;
                    req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    resp = req.GetResponse();
                    break;
                }
                catch (WebException ex)
                {
                    if (ex.Status != WebExceptionStatus.Timeout)
                        throw;
                }
            }
            if (resp == null)
            {
                throw new TimeoutException();
            }

            BufferedStream buffStream = new BufferedStream(resp.GetResponseStream());
            url = resp.ResponseUri.ToString();
            StreamReader reader = new StreamReader(buffStream);

            bool inContent = false;
            bool inPost = false;
            bool inPostContent = false;
            List<DownloadData> list = new List<DownloadData>();
            Match m;
            SeasonData seasonData=null;
            UploadData uploadData=null;

            while (!reader.EndOfStream)
            {
                String line = reader.ReadLine();
                if (line.Contains("line-height") && line.Contains("&nbsp;")) //detect: <div style="line-height:5px;height:5px;background-color:lightgrey;">&nbsp;</div>
                {
                    continue; //I can only hope this will never occour in any other case..
                }
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
                                        if (val != null && !String.IsNullOrWhiteSpace(val) && val.Trim().StartsWith("http://"))
                                        {
                                            downloads.Add(key, val);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Warning: Invalid Download received while parsing " + showData.Name + ". Ignoring link");
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
                                dd.Title = title;

                                if (title.ToLower().Contains("subbed"))
                                {
                                    dd.Upload.Subbed = true;
                                }

                                foreach (var download in downloads)
                                {
                                    dd.Links.Add(download.Key,download.Value);
                                }

                                list.Add(dd);
                            }
                            else if ((m = new Regex("(?:(?:<p(?:\\s+style\\s*\\=\\s*\\\"[^\\\"]+\\\"\\s*)?>)|(?:<div\\s+class\\s*=\\s*\"info\">))\\s*(.*?(?:Dauer|Größe|Sprache|Format|Uploader).*?)\\s*(?:(?:</p>)|(?:</div>))").Match(line)).Success || 
                                ((m = new Regex("<p\\s+style\\s*\\=\\s*\\\"[^\\\"]+\\\"\\s*>").Match(line)).Success && ((line=reader.ReadLine())!="") && (m = new Regex("\\s*(.*?(?:Dauer|Größe|Sprache|Format|Uploader).*?)\\s*</p>").Match(line)).Success))
                            {
                               /* /*
                                 * Nice case:
                                 <p><strong>Dauer:</strong> 20:00 | <strong>Größe:</strong> 175 MB | <strong>Sprache:</strong> Englisch &amp; deutsche Untertitel | <strong>Format:</strong> XviD | <strong>HQ-Cover:</strong> <a href="http://justpic.info/?s=cover&amp;id=&amp;name=&amp;keyword=VGhlIEJpZyBCYW5nIFRoZW9yeQ,,">Download</a> | <strong>Uploader:</strong> block06</p>
                                 
                                 * Bad case: (note newline!!)
                                  <p style="background-color: #f0f0f0;">
                                    <strong>Dauer:</strong> 20:00 | <strong>Größe:</strong> 175 MB | <strong>Sprache:</strong> Englisch | <strong>Format:</strong> XviD | <strong>HQ-Cover:</strong> <a href="http://justpic.info/?s=cover&#038;id=&#038;name=&#038;keyword=VGhlIEJpZyBCYW5nIFRoZW9yeQ,,">Download</a> | <strong>Uploader:</strong> block06</p>
                                 *//*

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
                                        if (value.Contains("subbed"))
                                        {
                                            uploadData.Subbed = true;
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
            resp.Dispose();
            return list;
        }*/
    }
}

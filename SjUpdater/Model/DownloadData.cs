using System;
using System.Collections.Generic;

namespace SjUpdater.Model
{
    public class DownloadData
    {
        public DownloadData()
        {
            Title = "";
            Upload = null;
            Links = new Dictionary<string, string>();
        }
        public String Title { get; set; }
        public Dictionary<String,String> Links { get; internal set; }
        public UploadData Upload { get; set; }
    }
}

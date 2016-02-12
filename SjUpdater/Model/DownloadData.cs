using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SjUpdater.Model
{
    public class DownloadData : Database.IDatabaseCompatibility
    {
        public DownloadData()
        {
            Title = "";
            Upload = null;
            Links = new Dictionary<string, string>();
        }

        [Key]
        public int Id { get; set; }

        public String Title { get; set; }
        public Dictionary<String,String> Links { get; internal set; }
        public UploadData Upload { get; set; }

        // Used by DatabaseWriter because SQLCE doesn't seem to recognise Dictionary - Calvin 12-Feb-2016
        public string LinkString { get; set; }
        
        public void ConvertToDatabase()
        {
            string[] LinkKeys = new string[Links.Keys.Count];
            string[] LinkValues = new string[Links.Values.Count];

            LinkString = "";

            Links.Keys.CopyTo(LinkKeys, 0);
            Links.Values.CopyTo(LinkValues, 0);

            for (int i = 0; i < Links.Keys.Count; i++)
            {
                LinkString += LinkKeys[i] + "\t" + LinkValues[i] + "\n";
            }
        }

        public void ConvertFromDatabase()
        {
            Links.Clear();

            foreach (string keyValue in LinkString.Split('\n'))
            {
                if (keyValue.Length > 0)
                { 
                    string[] keyValueSplit = keyValue.Split('\t');
                    Links.Add(keyValueSplit[0], keyValueSplit[1]);
                }
            }

            LinkString = null;
        }
    }
}

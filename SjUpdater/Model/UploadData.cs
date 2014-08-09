using System;
using System.Collections.Generic;
using System.Linq;

namespace SjUpdater.Model
{
    [Flags]
    public enum UploadLanguage
    {
        German = 1,
        English = 2,
        Any = German + English
    }
    public class UploadData
    {
        public UploadData()
        {
            Uploader = "";
            Format = "";
            Size = "";
            Runtime = "";
            Language = 0;
            Season = null;
        }

        public String Uploader { get; set; }
        public String Format { get; set; }
        public String Size { get; set; }
        public String Runtime { get; set; }
        public UploadLanguage Language { get; set; }
        public SeasonData Season { get; set; }
        public static IEnumerable<UploadLanguage> LanguagesValues
        {
            get
            {
                return Enum.GetValues(typeof(UploadLanguage))
                    .Cast<UploadLanguage>();
            }
        }

    }
}

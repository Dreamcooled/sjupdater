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
            Favorized = false;
        }

        public String Uploader { get; set; }
        public String Format { get; set; }
        public String Size { get; set; }
        public String Runtime { get; set; }
        public UploadLanguage Language { get; set; }
        public SeasonData Season { get; set; }

        public bool Favorized { get; set; } //Todo: move to Fav* class, since it's user data


        public static IEnumerable<UploadLanguage> LanguagesValues
        {
            get
            {
                return Enum.GetValues(typeof(UploadLanguage))
                    .Cast<UploadLanguage>();
            }
        }

        public override bool Equals(object obj)
        {
            var u2 = obj as UploadData;
            if (u2 == null) return false;
            return Uploader == u2.Uploader && Format == u2.Format && Size == u2.Size && Runtime == u2.Runtime &&
                   Language == u2.Language &&
                   (Season == u2.Season || (Season != null && u2.Season != null && Season.Url == u2.Season.Url));
        }

        public override int GetHashCode()
        {
            return Uploader.GetHashCode() ^ Format.GetHashCode() ^ Size.GetHashCode() ^ Runtime.GetHashCode() ^
                Language.GetHashCode() ^ ((Season==null)?0:Season.Url.GetHashCode());
        }
    }
}

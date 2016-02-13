using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using SjUpdater.Model;
using SjUpdater.Provider;

// DB context for saving show data to SQL 
// - Calvin, 11-Feb-2016

namespace SjUpdater.Database
{
    class CustomDbContext : DbContext
    {
        static CustomDbContext() { }

        // Need to include all of the classes saved in the database here, and make sure to call Load() on all of them in DatabaseWriter.LoadFromDatabase - Calvin 12-Feb-2016
        public DbSet<FavShowData> FavShowData { get; set; }
        public DbSet<FavSeasonData> FavSeasonData { get; set; }
        public DbSet<FavEpisodeData> FavEpisodeData { get; set; }

        //public DbSet<ShowInformation> ShowInformation { get; set; } // Not currently used - Calvin 13-Feb-2016
        //public DbSet<SeasonInformation> SeasonInformation { get; set; } // Not currently used - Calvin 13-Feb-2016
        public DbSet<EpisodeInformation> EpisodeInformation { get; set; }

        public DbSet<DownloadData> DownloadData { get; set; }
        public DbSet<UploadData> UploadData { get; set; }

        public DbSet<SeasonData> SeasonData { get; set; }
        public DbSet<ShowData> ShowData { get; set; }
    }
}

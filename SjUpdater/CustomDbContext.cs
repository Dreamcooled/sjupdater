using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using SjUpdater.Model;

// DB context for saving show data to SQL 
// - Calvin, 11-Feb-2016

namespace SjUpdater.Database
{
    class CustomDbContext : DbContext
    {
        static CustomDbContext()
        {
            System.Data.Entity.Database.SetInitializer<CustomDbContext>(null);
        }

        public DbSet<FavShowData> FavShowData { get; set; }
    }
}

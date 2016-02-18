using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SjUpdater.Database
{
    /// <summary>
    /// Impliment if something needs to be written/loaded from database
    /// </summary>
    interface IDatabaseCompatibility
    {
        void AddToDatabase(CustomDbContext db);
        void RemoveFromDatabase(CustomDbContext db);
    }
}

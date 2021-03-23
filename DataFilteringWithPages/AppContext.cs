using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using DataFilteringWithPages.Models;

namespace DataFilteringWithPages
{
    public partial class AppContext : DbContext
    {
        public AppContext()
            : base("data source=localhost\\SQLEXPRESS;initial catalog=Test0322;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework")
        {
        }

        public virtual DbSet<User> Users { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

        }
    }
}

namespace FitbitWebApplication.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class FitbitDb2 : DbContext
    {
        public FitbitDb2()
            : base("name=FitbitDb2")
        {
        }

        public virtual DbSet<MembershipUser> MembershipUsers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}

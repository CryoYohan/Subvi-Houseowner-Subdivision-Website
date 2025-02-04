using Microsoft.EntityFrameworkCore;

namespace ELNET1_GROUP_PROJECT.Data
{
    public class MyAppDBContext : DbContext
    {
        public MyAppDBContext(DbContextOptions<MyAppDBContext> options) : base(options) { }

        public DbSet<ELNET1_GROUP_PROJECT.Models.User_Account> Subvi { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Announcement> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Bill> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Event_Calendar> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Facility> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Feedback> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Forum> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Payment> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Poll> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Poll_Choice> MyApp { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Reply> MyApp { get; set; }
    }
}

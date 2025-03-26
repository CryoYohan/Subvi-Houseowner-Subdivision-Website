using Microsoft.EntityFrameworkCore;
using ELNET1_GROUP_PROJECT.Models;

namespace ELNET1_GROUP_PROJECT.Data
{
    public class MyAppDBContext : DbContext
    {
        public MyAppDBContext(DbContextOptions<MyAppDBContext> options) : base(options) { }

        public DbSet<User_Account> User_Accounts { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Service_Request> Service_Request { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Facility> Facility { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Reservation> Reservations { get; set; }
   
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add a unique constraint for Email field
            modelBuilder.Entity<User_Account>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Facility>()
                .HasKey(f => f.FacilityId);

            modelBuilder.Entity<Service_Request>()
                .HasKey(s => s.ServiceRequestId);

            modelBuilder.Entity<Announcement>()
                .HasKey(s => s.AnnouncementId);

            modelBuilder.Entity<Bill>()
                .HasKey(s => s.BillId);

            modelBuilder.Entity<Payment>()
                .HasKey(s => s.PaymentId);

            modelBuilder.Entity<Forum>()
                .HasKey(s => s.PostId);

            modelBuilder.Entity<Replies>()
                .HasKey(s => s.ReplyId);

            modelBuilder.Entity<Like>()
                .HasKey(s => s.LikeId);
        }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Announcement> Announcement { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Bill> Bill { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Payment> Payment { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Event_Calendar> Event_Calendar { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Forum> Forum { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Replies> Replies { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Like> Like { get; set; }

        public DbSet<ELNET1_GROUP_PROJECT.Models.Feedback> Feedback { get; set; }
        
        public DbSet<ELNET1_GROUP_PROJECT.Models.Visitor_Pass> Visitor_Pass { get; set; }
        //public DbSet<ELNET1_GROUP_PROJECT.Models.Poll> Poll { get; set; }
        //public DbSet<ELNET1_GROUP_PROJECT.Models.Poll_Choice> Poll_Choice { get; set; }
        


        //public DbSet<ELNET1_GROUP_PROJECT.Models.Vehicle_Registration> Vehicle_Registration { get; set; }
        
        //public DbSet<ELNET1_GROUP_PROJECT.Models.Vote> Vote { get; set; }
    }
}

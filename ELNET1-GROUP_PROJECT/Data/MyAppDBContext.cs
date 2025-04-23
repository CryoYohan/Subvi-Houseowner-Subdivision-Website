using Microsoft.EntityFrameworkCore;
using ELNET1_GROUP_PROJECT.Models;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ELNET1_GROUP_PROJECT.Data
{
    public class MyAppDBContext : DbContext
    {
        public MyAppDBContext(DbContextOptions<MyAppDBContext> options) : base(options) { }

        public DbSet<User_Info> User_Info { get; set; }
        public DbSet<User_Account> User_Accounts { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Service_Request> Service_Request { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Facility> Facility { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Reservation> Reservations { get; set; }
   
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User_Account>()
                .HasOne(u => u.User_Info)
                .WithOne(i => i.User_Accounts)
                .HasForeignKey<User_Info>(i => i.UserAccountId);

            modelBuilder.Entity<Facility>()
                .HasKey(f => f.FacilityId);

            modelBuilder.Entity<Service_Request>()
                .HasKey(s => s.ServiceRequestId);

            modelBuilder.Entity<Announcement>()
                .HasKey(s => s.AnnouncementId);

            modelBuilder.Entity<Bill>()
                .HasKey(s => s.BillId);

            modelBuilder.Entity<Payments>()
                .HasKey(s => s.PaymentId);

            modelBuilder.Entity<Forum>()
                .HasKey(s => s.PostId);

            modelBuilder.Entity<Replies>()
                .HasKey(s => s.ReplyId);

            modelBuilder.Entity<Like>()
                .HasKey(s => s.LikeId);

            base.OnModelCreating(modelBuilder);
        }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Lot> Lot { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Applications> Applications { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Documents> Documents { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Announcement> Announcement { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Bill> Bill { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Payments> Payment { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Event_Calendar> Event_Calendar { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Forum> Forum { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Replies> Replies { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Like> Like { get; set; }

        public DbSet<ELNET1_GROUP_PROJECT.Models.Feedback> Feedback { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Feedback_Conversation> FeedbackConversation { get; set; }
        
        public DbSet<ELNET1_GROUP_PROJECT.Models.Visitor_Pass> Visitor_Pass { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.VehicleRegistration> Vehicle_Registration { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Poll> Poll { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Poll_Choice> Poll_Choice { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Vote> Vote { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.Notification> Notifications { get; set; }
        public DbSet<ELNET1_GROUP_PROJECT.Models.NotificationReads> Notification_Reads { get; set; }
    }
}

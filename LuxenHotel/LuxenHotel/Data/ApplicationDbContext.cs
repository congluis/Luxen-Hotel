using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.Entities.Identity;
using LuxenHotel.Models.Entities.Orders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets from BookingContext
        public DbSet<Accommodation> Accommodations { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Combo> Combos { get; set; }

        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrderService> OrderService { get; set; }
        public DbSet<OrderCombo> OrderCombo { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Call the separated configuration methods
            ConfigureIdentity(builder);
            ConfigureBooking(builder);
            ConfigureOrder(builder);
        }

        private void ConfigureIdentity(ModelBuilder builder)
        {
            // Rename Identity tables for clarity
            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");

            // Ignore unused Identity entities
            builder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims", table => table.ExcludeFromMigrations());
            builder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins", table => table.ExcludeFromMigrations());
            builder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens", table => table.ExcludeFromMigrations());
            builder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims", table => table.ExcludeFromMigrations());

            // Configure IdentityUser entity
            builder.Entity<User>(entity =>
            {
                // Required properties
                entity.Property(u => u.PasswordHash).HasColumnName("Password");

                // Ignored properties
                entity.Ignore(e => e.PhoneNumberConfirmed);
                entity.Ignore(e => e.EmailConfirmed);
                entity.Ignore(e => e.LockoutEnabled);
                entity.Ignore(e => e.LockoutEnd);
                entity.Ignore(e => e.AccessFailedCount);
                entity.Ignore(e => e.TwoFactorEnabled);
            });
        }

        private void ConfigureBooking(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Accommodation)
                .WithMany(a => a.Services)
                .HasForeignKey(s => s.AccommodationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Combo>()
                .HasMany(c => c.ComboServices)
                .WithMany(s => s.ComboServices)
                .UsingEntity<Dictionary<string, object>>(
                    j => j
                        .HasOne<Service>()
                        .WithMany()
                        .HasForeignKey("ServiceId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j => j
                        .HasOne<Combo>()
                        .WithMany()
                        .HasForeignKey("ComboId")
                        .OnDelete(DeleteBehavior.Restrict)
                ).ToTable("ComboService");
        }

        private void ConfigureOrder(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Orders>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Orders>()
                .HasOne(o => o.Accommodation)
                .WithMany()
                .HasForeignKey(o => o.AccommodationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderService>()
                    .HasOne(os => os.Order)
                    .WithMany(o => o.OrderServices)
                    .HasForeignKey(os => os.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.Service)
                .WithMany()
                .HasForeignKey(os => os.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderCombo>()
                .HasOne(oc => oc.Order)
                .WithMany(o => o.OrderCombos)
                .HasForeignKey(oc => oc.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderCombo>()
                .HasOne(oc => oc.Combo)
                .WithMany()
                .HasForeignKey(oc => oc.ComboId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Orders)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique();

            modelBuilder.Entity<Orders>()
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Orders>()
                .Property(o => o.PaymentMethod)
                .HasConversion<string>();

            modelBuilder.Entity<Orders>()
                .Property(o => o.PaymentStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Orders>()
                .Property(o => o.OrderCode)
                .IsRequired()
                .HasMaxLength(20);

            modelBuilder.Entity<Payment>()
                .Property(p => p.TransactionId)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentProvider)
                .IsRequired()
                .HasMaxLength(50);
        }
    }
}
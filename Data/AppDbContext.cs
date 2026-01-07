using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Entities;

namespace PersonalJournalApp.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        // DbSets for all entities
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure JournalEntry-Tag many-to-many relationship
            modelBuilder.Entity<JournalEntry>()
                .HasMany(je => je.Tags)
                .WithMany(t => t.JournalEntries)
                .UsingEntity(j => j.ToTable("JournalEntryTags"));

            // Configure User-JournalEntry relationship with cascade delete
            modelBuilder.Entity<JournalEntry>()
                .HasOne(je => je.User)
                .WithMany(u => u.JournalEntries)
                .HasForeignKey(je => je.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User-Tag relationship with cascade delete
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tags)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User-Category relationship with cascade delete
            modelBuilder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User-UserSettings 1:1 relationship with cascade delete
            modelBuilder.Entity<UserSettings>()
                .HasOne(us => us.User)
                .WithOne(u => u.UserSettings)
                .HasForeignKey<UserSettings>(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

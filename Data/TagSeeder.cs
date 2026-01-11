using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PersonalJournalApp.Entities;

namespace PersonalJournalApp.Data
{
    public static class TagSeeder
    {
        private static readonly List<string> PrebuiltTags = new()
        {
            "Work", "Career", "Studies", "Family", "Friends", "Relationships",
            "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies",
            "Travel", "Nature", "Finance", "Spirituality", "Birthday", "Holiday",
            "Vacation", "Celebration", "Exercise", "Reading", "Writing", "Cooking",
            "Meditation", "Yoga", "Music", "Shopping", "Parenting", "Projects",
            "Planning", "Reflection"
        };

        public static async Task SeedTagsForUserAsync(AppDbContext context, string userId)
        {
            // Check if user already has pre-built tags
            var existingPrebuiltTags = context.Tags
                .Where(t => t.UserId == userId && t.IsPrebuilt)
                .ToList();

            if (existingPrebuiltTags.Any())
            {
                return; // Already seeded
            }

            // Create pre-built tags for the user
            var tagsToAdd = PrebuiltTags.Select(tagName => new Tag
            {
                Name = tagName,
                UserId = userId,
                IsPrebuilt = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            context.Tags.AddRange(tagsToAdd);
            await context.SaveChangesAsync();
        }
    }
}
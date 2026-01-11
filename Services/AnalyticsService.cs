using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Common;
using PersonalJournalApp.Data;
using PersonalJournalApp.Models.Display;

namespace PersonalJournalApp.Services
{
    public class AnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        // Get comprehensive analytics for user
        public async Task<ServiceResult<AnalyticsDisplayModel>> GetAnalyticsAsync(string userId)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId)
                    .ToListAsync();

                if (!entries.Any())
                {
                    return ServiceResult<AnalyticsDisplayModel>.SuccessResult(new AnalyticsDisplayModel());
                }

                var analytics = new AnalyticsDisplayModel
                {
                    TotalEntries = entries.Count,
                    CurrentStreak = CalculateCurrentStreak(entries),
                    TotalWords = entries.Sum(e => CountWords(e.Content)),
                    EntriesThisMonth = entries.Count(e => e.CreatedDate.Month == DateTime.UtcNow.Month && e.CreatedDate.Year == DateTime.UtcNow.Year)
                };

                // Mood distribution
                var moodGroups = entries.GroupBy(e => e.PrimaryMood)
                    .Select(g => new { Mood = g.Key, Count = g.Count() })
                    .ToList();

                analytics.MoodCounts = moodGroups.ToDictionary(x => x.Mood, x => x.Count);
                analytics.MoodPercentages = moodGroups.ToDictionary(
                    x => x.Mood,
                    x => Math.Round((double)x.Count / entries.Count * 100, 1)
                );

                // Most common mood
                analytics.MostCommonMood = moodGroups
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault()?.Mood ?? "calm";

                // Top tags
                var tagUsage = entries
                    .SelectMany(e => e.Tags)
                    .GroupBy(t => t.Name)
                    .Select(g => new TagUsageDisplayModel
                    {
                        TagName = g.Key,
                        UsageCount = g.Count()
                    })
                    .OrderByDescending(t => t.UsageCount)
                    .Take(5)
                    .ToList();

                analytics.TopTags = tagUsage;

                // Entries by month (last 6 months)
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                var monthlyEntries = entries
                    .Where(e => e.CreatedDate >= sixMonthsAgo)
                    .GroupBy(e => e.CreatedDate.ToString("MMM yyyy"))
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Month)
                    .ToDictionary(x => x.Month, x => x.Count);

                analytics.EntriesByMonth = monthlyEntries;

                // Most active month
                analytics.MostActiveMonth = monthlyEntries
                    .OrderByDescending(x => x.Value)
                    .FirstOrDefault().Key ?? "N/A";

                // Average words per entry
                analytics.AverageWordsPerEntry = Math.Round(
                    (double)analytics.TotalWords / analytics.TotalEntries, 1
                );

                return ServiceResult<AnalyticsDisplayModel>.SuccessResult(analytics);
            }
            catch (Exception ex)
            {
                return ServiceResult<AnalyticsDisplayModel>.FailureResult($"Failed to retrieve analytics: {ex.Message}");
            }
        }

        // Calculate current streak (consecutive days with entries)
        private int CalculateCurrentStreak(List<Entities.JournalEntry> entries)
        {
            if (!entries.Any())
                return 0;

            var sortedDates = entries
                .Select(e => e.CreatedDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            int streak = 0;
            var today = DateTime.UtcNow.Date;
            var expectedDate = today;

            // Check if there's an entry today or yesterday to start counting
            if (sortedDates.First() != today && sortedDates.First() != today.AddDays(-1))
                return 0;

            foreach (var date in sortedDates)
            {
                if (date == expectedDate || date == expectedDate.AddDays(-1))
                {
                    streak++;
                    expectedDate = date.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        // Count words in content
        private int CountWords(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            return content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}

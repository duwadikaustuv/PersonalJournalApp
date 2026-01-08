using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalJournalApp.Models.Display
{
    public class AnalyticsDisplayModel
    {
        public int TotalEntries { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalWords { get; set; }
        public int EntriesThisMonth { get; set; }

        // Mood distribution for charts
        public Dictionary<string, int> MoodCounts { get; set; } = new();
        public Dictionary<string, double> MoodPercentages { get; set; } = new();

        // Tag usage
        public List<TagUsageDisplayModel> TopTags { get; set; } = new();

        // Entry frequency
        public Dictionary<string, int> EntriesByMonth { get; set; } = new();

        // Insights
        public string MostCommonMood { get; set; } = string.Empty;
        public string MostActiveMonth { get; set; } = string.Empty;
        public double AverageWordsPerEntry { get; set; }
    }

    public class TagUsageDisplayModel
    {
        public string TagName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }
}

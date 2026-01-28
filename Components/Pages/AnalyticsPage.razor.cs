using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Auth;
using PersonalJournalApp.Data;
using PersonalJournalApp.Models.Display;
using PersonalJournalApp.Services;

namespace PersonalJournalApp.Components.Pages;

public partial class AnalyticsPage : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private CustomAuthStateProvider CustomAuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private TagService TagService { get; set; } = default!;
    [Inject] private AppDbContext DbContext { get; set; } = default!;
    [Inject] private PdfExportService PdfExportService { get; set; } = default!;

    private bool isAuthorized = false;
    private bool isLoading = true;
    private string? currentUserId;
    private int selectedPeriod = 90; // Default: Last 3 months

    private AnalyticsData analytics = new();
    private List<JournalEntryDisplayModel> allEntries = new();

    // Mood categories
    private readonly List<string> positiveMoods = new() { "happy", "excited", "relaxed", "grateful", "confident" };
    private readonly List<string> neutralMoods = new() { "calm", "thoughtful", "curious", "nostalgic", "bored" };
    private readonly List<string> negativeMoods = new() { "sad", "angry", "anxious", "stressed", "tired" };

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        isAuthorized = true;
        currentUserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            currentUserId = CustomAuthStateProvider.GetCurrentUserId();
        }

        await LoadAnalytics();
        isLoading = false;
    }

    private async Task OnPeriodChanged()
    {
        isLoading = true;
        StateHasChanged();
        await LoadAnalytics();
        isLoading = false;
        StateHasChanged();
    }

    private async Task LoadAnalytics()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        // Get all entries for the user
        var result = await JournalService.GetAllEntriesAsync(currentUserId);
        if (!result.Success || result.Data == null)
        {
            allEntries = new List<JournalEntryDisplayModel>();
        }
        else
        {
            allEntries = result.Data;
        }

        // Filter by selected period
        var filteredEntries = FilterEntriesByPeriod(allEntries);

        // Calculate all analytics
        analytics = CalculateAnalytics(filteredEntries, allEntries);
    }

    private List<JournalEntryDisplayModel> FilterEntriesByPeriod(List<JournalEntryDisplayModel> entries)
    {
        if (selectedPeriod == 0) return entries; // All time

        var cutoffDate = DateTime.UtcNow.AddDays(-selectedPeriod);
        return entries.Where(e => e.CreatedDate >= cutoffDate).ToList();
    }

    private AnalyticsData CalculateAnalytics(List<JournalEntryDisplayModel> filteredEntries, List<JournalEntryDisplayModel> allEntries)
    {
        var analytics = new AnalyticsData();

        // Basic stats
        analytics.TotalEntries = filteredEntries.Count;
        analytics.TotalWords = filteredEntries.Sum(e => e.WordCount);
        analytics.AverageWordsPerEntry = filteredEntries.Any() ? filteredEntries.Average(e => e.WordCount) : 0;

        // Calculate streaks using all entries
        CalculateStreaks(allEntries, analytics);

        // Completion rate (entries / days in period)
        var daysInPeriod = selectedPeriod == 0
            ? (allEntries.Any() ? (DateTime.UtcNow - allEntries.Min(e => e.CreatedDate)).Days + 1 : 1)
            : selectedPeriod;
        analytics.CompletionRate = daysInPeriod > 0 ? Math.Min((int)(filteredEntries.Count * 100.0 / daysInPeriod), 100) : 0;

        // Days journaling
        analytics.DaysJournaling = filteredEntries.Select(e => e.CreatedDate.Date).Distinct().Count();

        // Mood distribution
        analytics.MoodCounts = new Dictionary<string, int>();
        foreach (var entry in filteredEntries)
        {
            foreach (var mood in entry.AllMoods)
            {
                if (!analytics.MoodCounts.ContainsKey(mood))
                    analytics.MoodCounts[mood] = 0;
                analytics.MoodCounts[mood]++;
            }
        }

        // Most common mood
        analytics.MostCommonMood = analytics.MoodCounts.Any()
            ? analytics.MoodCounts.OrderByDescending(m => m.Value).First().Key
            : "";

        // Mood category percentages
        var totalMoodEntries = filteredEntries.Count;
        if (totalMoodEntries > 0)
        {
            var positiveCount = filteredEntries.Count(e => positiveMoods.Contains(e.PrimaryMood));
            var neutralCount = filteredEntries.Count(e => neutralMoods.Contains(e.PrimaryMood));
            var negativeCount = filteredEntries.Count(e => negativeMoods.Contains(e.PrimaryMood));

            analytics.PositiveMoodPercentage = (int)(positiveCount * 100.0 / totalMoodEntries);
            analytics.NeutralMoodPercentage = (int)(neutralCount * 100.0 / totalMoodEntries);
            analytics.NegativeMoodPercentage = (int)(negativeCount * 100.0 / totalMoodEntries);
        }

        // Category distribution
        analytics.CategoryCounts = new Dictionary<string, int>();
        foreach (var entry in filteredEntries)
        {
            var categoryName = entry.CategoryName ?? "";
            if (!analytics.CategoryCounts.ContainsKey(categoryName))
                analytics.CategoryCounts[categoryName] = 0;
            analytics.CategoryCounts[categoryName]++;
        }
        analytics.UniqueCategories = analytics.CategoryCounts.Count(c => !string.IsNullOrEmpty(c.Key));

        // Weekly frequency
        analytics.WeeklyFrequency = new Dictionary<string, int>();
        var weeks = filteredEntries
            .GroupBy(e => GetWeekLabel(e.CreatedDate))
            .OrderBy(g => g.Min(e => e.CreatedDate));
        foreach (var week in weeks)
        {
            analytics.WeeklyFrequency[week.Key] = week.Count();
        }

        // Top tags
        var tagCounts = new Dictionary<string, int>();
        foreach (var entry in filteredEntries)
        {
            foreach (var tag in entry.TagNames)
            {
                if (!tagCounts.ContainsKey(tag))
                    tagCounts[tag] = 0;
                tagCounts[tag]++;
            }
        }
        analytics.TopTags = tagCounts
            .OrderByDescending(t => t.Value)
            .Select(t => new TagUsageDisplayModel { TagName = t.Key, UsageCount = t.Value })
            .ToList();
        analytics.UniqueTags = tagCounts.Count;

        // Word count trend (by week)
        analytics.WordCountTrend = new Dictionary<string, int>();
        var wordsByWeek = filteredEntries
            .GroupBy(e => GetWeekLabel(e.CreatedDate))
            .OrderBy(g => g.Min(e => e.CreatedDate));
        foreach (var week in wordsByWeek)
        {
            analytics.WordCountTrend[week.Key] = (int)week.Average(e => e.WordCount);
        }

        // Word count growth
        if (analytics.WordCountTrend.Count >= 2)
        {
            var firstAvg = analytics.WordCountTrend.First().Value;
            var lastAvg = analytics.WordCountTrend.Last().Value;
            analytics.WordCountGrowth = lastAvg - firstAvg;
        }

        // Time distribution
        analytics.TimeDistribution = new Dictionary<string, int>
        {
            { "Morning", 0 },
            { "Afternoon", 0 },
            { "Evening", 0 },
            { "Night", 0 }
        };
        foreach (var entry in filteredEntries)
        {
            var hour = entry.CreatedDate.ToLocalTime().Hour;
            var timeSlot = hour switch
            {
                >= 5 and < 12 => "Morning",
                >= 12 and < 17 => "Afternoon",
                >= 17 and < 21 => "Evening",
                _ => "Night"
            };
            analytics.TimeDistribution[timeSlot]++;
        }
        analytics.MostActiveTimeSlot = analytics.TimeDistribution.Any()
            ? analytics.TimeDistribution.OrderByDescending(t => t.Value).First().Key
            : "";

        return analytics;
    }

    private void CalculateStreaks(List<JournalEntryDisplayModel> entries, AnalyticsData analytics)
    {
        if (!entries.Any())
        {
            analytics.CurrentStreak = 0;
            analytics.LongestStreak = 0;
            return;
        }

        var entryDates = entries
            .Select(e => e.CreatedDate.ToLocalTime().Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var today = DateTime.Now.Date;
        var yesterday = today.AddDays(-1);

        // Current streak
        int currentStreak = 0;
        var checkDate = entryDates.Contains(today) ? today : yesterday;

        if (entryDates.Contains(today) || entryDates.Contains(yesterday))
        {
            while (entryDates.Contains(checkDate))
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }
        }
        analytics.CurrentStreak = currentStreak;

        // Longest streak
        int longestStreak = 0;
        int tempStreak = 1;
        var sortedDates = entryDates.OrderBy(d => d).ToList();

        for (int i = 1; i < sortedDates.Count; i++)
        {
            if ((sortedDates[i] - sortedDates[i - 1]).Days == 1)
            {
                tempStreak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, tempStreak);
                tempStreak = 1;
            }
        }
        analytics.LongestStreak = Math.Max(longestStreak, tempStreak);
    }

    private string GetWeekLabel(DateTime date)
    {
        var weekStart = date.Date.AddDays(-(int)date.DayOfWeek);
        return weekStart.ToString("MMM d");
    }

    private string GetPeriodLabel()
    {
        return selectedPeriod switch
        {
            7 => "Last 7 days",
            30 => "Last 30 days",
            90 => "Last 90 days",
            365 => "This year",
            _ => "All time"
        };
    }

    private string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToUpper(text[0]) + text.Substring(1);
    }

    private string GetMoodInsightClass(string mood)
    {
        if (positiveMoods.Contains(mood)) return "positive";
        if (negativeMoods.Contains(mood)) return "negative";
        return "neutral";
    }

    private string GetMoodInsightText(string mood, int count, int total)
    {
        var percentage = total > 0 ? (int)(count * 100.0 / total) : 0;

        if (positiveMoods.Contains(mood))
            return $"{CapitalizeFirst(mood)} appears in {percentage}% of your entries. Keep up the positive energy!";
        if (negativeMoods.Contains(mood))
            return $"{CapitalizeFirst(mood)} appears in {percentage}% of your entries. Consider what might help improve your mood.";
        return $"{CapitalizeFirst(mood)} appears in {percentage}% of your entries.";
    }

    private async Task ExportReport()
    {
        try
        {
            var reportData = new AnalyticsReportData
            {
                PeriodLabel = GetPeriodLabel(),
                TotalEntries = analytics.TotalEntries,
                TotalWords = analytics.TotalWords,
                AverageWordsPerEntry = analytics.AverageWordsPerEntry,
                CurrentStreak = analytics.CurrentStreak,
                LongestStreak = analytics.LongestStreak,
                DaysJournaling = analytics.DaysJournaling,
                UniqueTags = analytics.UniqueTags,
                MoodCounts = analytics.MoodCounts,
                TopTags = analytics.TopTags
            };

            var filePath = await PdfExportService.ExportAnalyticsReportAsync(reportData);

            if (!string.IsNullOrEmpty(filePath))
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Export Successful",
                    $"Your analytics report has been saved as PDF to:\n{filePath}",
                    "OK"
                );
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Export Cancelled",
                    "The export was cancelled.",
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "Failed to export report: " + ex.Message, "OK");
        }
    }

    private string GenerateReportText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Journal Analytics Report ===");
        sb.AppendLine($"Generated: {DateTime.Now:MMMM dd, yyyy}");
        sb.AppendLine($"Period: {GetPeriodLabel()}");
        sb.AppendLine();
        sb.AppendLine("--- Overview ---");
        sb.AppendLine($"Total Entries: {analytics.TotalEntries}");
        sb.AppendLine($"Current Streak: {analytics.CurrentStreak} days");
        sb.AppendLine($"Longest Streak: {analytics.LongestStreak} days");
        sb.AppendLine($"Average Words/Entry: {analytics.AverageWordsPerEntry:F0}");
        sb.AppendLine($"Total Words Written: {analytics.TotalWords:N0}");
        sb.AppendLine();
        sb.AppendLine("--- Mood Distribution ---");
        foreach (var mood in analytics.MoodCounts.OrderByDescending(m => m.Value))
        {
            sb.AppendLine($"{CapitalizeFirst(mood.Key)}: {mood.Value}");
        }
        return sb.ToString();
    }

    // Analytics data class
    private class AnalyticsData
    {
        public int TotalEntries { get; set; }
        public int TotalWords { get; set; }
        public double AverageWordsPerEntry { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int CompletionRate { get; set; }
        public int DaysJournaling { get; set; }
        public int UniqueTags { get; set; }
        public int UniqueCategories { get; set; }
        public int WordCountGrowth { get; set; }

        public int PositiveMoodPercentage { get; set; }
        public int NeutralMoodPercentage { get; set; }
        public int NegativeMoodPercentage { get; set; }

        public string MostCommonMood { get; set; } = "";
        public string MostActiveTimeSlot { get; set; } = "";

        public Dictionary<string, int> MoodCounts { get; set; } = new();
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public Dictionary<string, int> WeeklyFrequency { get; set; } = new();
        public Dictionary<string, int> WordCountTrend { get; set; } = new();
        public Dictionary<string, int> TimeDistribution { get; set; } = new();
        public List<TagUsageDisplayModel> TopTags { get; set; } = new();
    }
}

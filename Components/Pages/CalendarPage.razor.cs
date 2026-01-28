using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Auth;
using PersonalJournalApp.Data;
using PersonalJournalApp.Models.Display;
using PersonalJournalApp.Services;

namespace PersonalJournalApp.Components.Pages;

public partial class CalendarPage : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private CustomAuthStateProvider CustomAuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private PdfExportService PdfExportService { get; set; } = default!;
    [Inject] private AppDbContext DbContext { get; set; } = default!;

    private bool isAuthorized = false;
    private string? currentUserId;

    // Calendar state
    private DateTime currentDate = DateTime.Now;
    private DateTime? selectedDate;
    private List<DateTime> calendarDays = new();

    // Entries data
    private Dictionary<DateTime, JournalEntryDisplayModel> entriesByDate = new();
    private JournalEntryDisplayModel? selectedEntry;

    // Monthly stats
    private MonthlyStats monthlyStats = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        currentUserId = userIdClaim.Value;

        var userExists = await DbContext.Users.AnyAsync(u => u.Id == currentUserId);
        if (!userExists)
        {
            await CustomAuthStateProvider.MarkUserAsLoggedOut();
            Navigation.NavigateTo("/login");
            return;
        }

        isAuthorized = true;

        // Initialize calendar with today selected
        selectedDate = DateTime.Now.Date;
        await LoadMonthData();
    }

    private async Task LoadMonthData()
    {
        GenerateCalendarDays();
        await LoadEntriesForMonth();
        CalculateMonthlyStats();

        // Load selected entry if date is selected
        if (selectedDate.HasValue && entriesByDate.ContainsKey(selectedDate.Value.Date))
        {
            selectedEntry = entriesByDate[selectedDate.Value.Date];
        }
        else
        {
            selectedEntry = null;
        }
    }

    private void GenerateCalendarDays()
    {
        calendarDays.Clear();

        var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Get the starting day (Sunday of the week containing the 1st)
        var startDate = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

        // Generate 42 days (6 weeks) to fill the calendar grid
        for (int i = 0; i < 42; i++)
        {
            calendarDays.Add(startDate.AddDays(i));
        }
    }

    private async Task LoadEntriesForMonth()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        entriesByDate.Clear();

        var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        var result = await JournalService.GetEntriesByDateRangeAsync(currentUserId, firstDayOfMonth, lastDayOfMonth);

        if (result.Success && result.Data != null)
        {
            foreach (var entry in result.Data)
            {
                var entryDate = entry.CreatedDate.ToLocalTime().Date;
                if (!entriesByDate.ContainsKey(entryDate))
                {
                    entriesByDate[entryDate] = entry;
                }
            }
        }
    }

    private void CalculateMonthlyStats()
    {
        var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var today = DateTime.Now.Date;
        var lastRelevantDay = currentDate.Month == today.Month && currentDate.Year == today.Year
            ? today
            : new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));

        var daysInMonth = (lastRelevantDay - firstDayOfMonth).Days + 1;
        var entriesCount = entriesByDate.Count;
        var missedDays = Math.Max(0, daysInMonth - entriesCount);
        var completionPercentage = daysInMonth > 0 ? (int)Math.Round((double)entriesCount / daysInMonth * 100) : 0;

        // Find most common mood
        string? mostCommonMood = null;
        if (entriesByDate.Count > 0)
        {
            mostCommonMood = entriesByDate.Values
                .GroupBy(e => e.PrimaryMood)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
        }

        monthlyStats = new MonthlyStats
        {
            EntriesCount = entriesCount,
            MissedDays = missedDays,
            CompletionPercentage = completionPercentage,
            MostCommonMood = mostCommonMood
        };
    }

    private async Task SelectDate(DateTime date)
    {
        if (date.Month != currentDate.Month) return;

        selectedDate = date.Date;

        if (entriesByDate.ContainsKey(date.Date))
        {
            selectedEntry = entriesByDate[date.Date];
        }
        else
        {
            selectedEntry = null;
        }

        await Task.CompletedTask;
    }

    private async Task PreviousMonth()
    {
        currentDate = currentDate.AddMonths(-1);
        selectedDate = null;
        selectedEntry = null;
        await LoadMonthData();
    }

    private async Task NextMonth()
    {
        currentDate = currentDate.AddMonths(1);
        selectedDate = null;
        selectedEntry = null;
        await LoadMonthData();
    }

    private async Task GoToToday()
    {
        currentDate = DateTime.Now;
        selectedDate = DateTime.Now.Date;
        await LoadMonthData();
    }

    private async Task GoToLastMonth()
    {
        await PreviousMonth();
    }

    private async Task GoToNextMonth()
    {
        await NextMonth();
    }

    private void ViewSelectedEntry()
    {
        if (selectedEntry != null)
        {
            Navigation.NavigateTo($"/entry/{selectedEntry.Id}");
        }
    }

    private async Task ExportCurrentMonth()
    {
        try
        {
            if (monthlyStats.EntriesCount == 0)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "No Entries",
                    "There are no journal entries for this month to export.",
                    "OK"
                );
                return;
            }

            var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Get all entries for the current month
            var entriesToExport = entriesByDate.Values.OrderBy(e => e.CreatedDate).ToList();

            if (entriesToExport.Count == 0)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "No Entries",
                    "There are no journal entries for this month to export.",
                    "OK"
                );
                return;
            }

            // Use the PdfExportService to export entries by date range
            var filePath = await PdfExportService.ExportEntriesByDateRangeAsync(
                entriesToExport,
                firstDayOfMonth,
                lastDayOfMonth
            );

            if (!string.IsNullOrEmpty(filePath))
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Export Successful",
                    $"Your journal entries for {currentDate:MMMM yyyy} have been exported as PDF to:\n{filePath}",
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
            await Application.Current!.MainPage!.DisplayAlert(
                "Export Error",
                $"Failed to export entries: {ex.Message}",
                "OK"
            );
        }
    }

    private string GetDayClass(DateTime day)
    {
        var classes = new List<string>();

        if (day.Month != currentDate.Month)
        {
            classes.Add("other-month");
        }

        if (day.Date == DateTime.Now.Date)
        {
            classes.Add("today");
        }

        return string.Join(" ", classes);
    }

    private bool IsToday(DateTime date)
    {
        return date.ToLocalTime().Date == DateTime.Now.Date;
    }

    private string FormatMoodName(string mood)
    {
        if (string.IsNullOrEmpty(mood)) return "";
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mood.ToLower());
    }

    private string GetMoodColor(string mood)
    {
        return mood?.ToLower() switch
        {
            // Positive moods
            "happy" => "#a78bfa",
            "excited" => "#c4b5fd",
            "relaxed" => "#8b5cf6",
            "grateful" => "#a78bfa",
            "confident" => "#c4b5fd",

            // Neutral moods - grays
            "calm" => "#9ca3af",
            "thoughtful" => "#6b7280",
            "curious" => "#9ca3af",
            "nostalgic" => "#6b7280",
            "bored" => "#d1d5db",

            // Negative moods - darker grays
            "sad" => "#6b7280",
            "angry" => "#4b5563",
            "anxious" => "#9ca3af",
            "stressed" => "#6b7280",
            "tired" => "#9ca3af",
            "lonely" => "#6b7280",

            _ => "#6366f1"
        };
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Trim();
    }

    private class MonthlyStats
    {
        public int EntriesCount { get; set; }
        public int MissedDays { get; set; }
        public int CompletionPercentage { get; set; }
        public string? MostCommonMood { get; set; }
    }
}
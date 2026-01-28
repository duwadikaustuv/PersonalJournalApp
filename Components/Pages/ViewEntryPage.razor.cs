using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Auth;
using PersonalJournalApp.Data;
using PersonalJournalApp.Models.Display;
using PersonalJournalApp.Services;

namespace PersonalJournalApp.Components.Pages;

public partial class ViewEntryPage : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private CustomAuthStateProvider CustomAuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private AppDbContext DbContext { get; set; } = default!;
    [Inject] private PdfExportService PdfExportService { get; set; } = default!;

    [Parameter]
    public int EntryId { get; set; }

    private bool isAuthorized = false;
    private bool isLoading = true;
    private string? currentUserId;
    private JournalEntryDisplayModel? entry;

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
        await LoadEntry();
    }

    private async Task LoadEntry()
    {
        isLoading = true;
        var result = await JournalService.GetEntryByIdAsync(currentUserId!, EntryId);

        if (result.Success && result.Data != null)
        {
            entry = result.Data;
        }
        else
        {
            entry = null;
        }

        isLoading = false;
    }

    private bool IsToday(DateTime dateTime)
    {
        return dateTime.ToLocalTime().Date == DateTime.Now.Date;
    }

    private int GetReadingTime()
    {
        if (entry == null) return 0;
        var wordsPerMinute = 200;
        var readingTime = (int)Math.Ceiling((double)entry.WordCount / wordsPerMinute);
        return readingTime < 1 ? 1 : readingTime;
    }

    private async Task ExportAsPdf()
    {
        if (entry == null) return;

        try
        {
            var filePath = await PdfExportService.ExportSingleEntryAsync(entry);

            if (!string.IsNullOrEmpty(filePath))
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Export Successful",
                    $"Your journal entry has been saved as PDF to:\n{filePath}",
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
            await Application.Current!.MainPage!.DisplayAlert("Error", "Failed to export: " + ex.Message, "OK");
        }
    }

    private string GeneratePdfHtml()
    {
        if (entry == null) return string.Empty;

        var moods = FormatMoodName(entry.PrimaryMood);
        if (!string.IsNullOrEmpty(entry.SecondaryMood1))
            moods += ", " + FormatMoodName(entry.SecondaryMood1);
        if (!string.IsNullOrEmpty(entry.SecondaryMood2))
            moods += ", " + FormatMoodName(entry.SecondaryMood2);

        var tags = entry.TagNames.Count > 0 ? string.Join(", ", entry.TagNames) : "None";
        var category = !string.IsNullOrEmpty(entry.CategoryName) ? entry.CategoryName : "None";
        var title = string.IsNullOrEmpty(entry.Title) ? "Untitled Entry" : entry.Title;
        var encodedTitle = System.Net.WebUtility.HtmlEncode(title);
        var formattedDate = entry.CreatedDate.ToLocalTime().ToString("dddd, MMMM d, yyyy");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<title>" + encodedTitle + "</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 800px; margin: 0 auto; padding: 40px; color: #333; line-height: 1.6; }");
        sb.AppendLine("h1 { font-size: 28px; color: #111; margin-bottom: 8px; border-bottom: 2px solid #6366f1; padding-bottom: 12px; }");
        sb.AppendLine(".meta { color: #666; font-size: 14px; margin-bottom: 24px; }");
        sb.AppendLine(".info-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; background: #f8f9fa; padding: 16px; border-radius: 8px; margin-bottom: 24px; }");
        sb.AppendLine(".info-item { display: flex; flex-direction: column; }");
        sb.AppendLine(".info-label { font-size: 12px; color: #666; text-transform: uppercase; letter-spacing: 0.5px; }");
        sb.AppendLine(".info-value { font-size: 14px; color: #333; font-weight: 500; }");
        sb.AppendLine(".content { font-size: 16px; line-height: 1.8; }");
        sb.AppendLine(".footer { margin-top: 40px; padding-top: 16px; border-top: 1px solid #e5e7eb; font-size: 12px; color: #999; text-align: center; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>" + encodedTitle + "</h1>");
        sb.AppendLine("<div class=\"meta\">" + formattedDate + " at " + entry.FormattedTime + "</div>");
        sb.AppendLine("<div class=\"info-grid\">");
        sb.AppendLine("<div class=\"info-item\"><span class=\"info-label\">Mood</span><span class=\"info-value\">" + moods + "</span></div>");
        sb.AppendLine("<div class=\"info-item\"><span class=\"info-label\">Word Count</span><span class=\"info-value\">" + entry.WordCount + " words</span></div>");
        sb.AppendLine("<div class=\"info-item\"><span class=\"info-label\">Category</span><span class=\"info-value\">" + category + "</span></div>");
        sb.AppendLine("<div class=\"info-item\"><span class=\"info-label\">Tags</span><span class=\"info-value\">" + tags + "</span></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"content\">" + entry.Content + "</div>");
        sb.AppendLine("<div class=\"footer\">Exported from Personal Journal App</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private async Task DeleteEntry()
    {
        bool confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Delete Entry",
            "Are you sure you want to delete this entry? This action cannot be undone.",
            "Delete",
            "Cancel"
        );

        if (!confirmed) return;

        var result = await JournalService.DeleteEntryAsync(currentUserId!, EntryId);

        if (result.Success)
        {
            await Application.Current!.MainPage!.DisplayAlert("Success", "Entry deleted successfully.", "OK");
            Navigation.NavigateTo("/timeline");
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.ErrorMessage ?? "Failed to delete entry.", "OK");
        }
    }

    private string FormatMoodName(string mood)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mood.ToLower());
    }

    private string GetMoodIconPath(string mood)
    {
        return mood.ToLower() switch
        {
            "happy" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M8 14s1.5 2 4 2 4-2 4-2\"></path><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "excited" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M8 14s1.5 2 4 2 4-2 4-2\"></path><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "relaxed" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M8 14s1.5 2 4 2 4-2 4-2\"></path><line x1=\"9\" y1=\"10\" x2=\"9.01\" y2=\"10\"></line><line x1=\"15\" y1=\"10\" x2=\"15.01\" y2=\"10\"></line>",
            "grateful" => "<path d=\"M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z\"></path>",
            "confident" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M8 14s1.5 2 4 2 4-2 4-2\"></path><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "calm" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"8\" y1=\"12\" x2=\"16\" y2=\"12\"></line><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "thoughtful" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3\"></path><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"></line>",
            "curious" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3\"></path><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"></line>",
            "nostalgic" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><polyline points=\"12 6 12 12 16 14\"></polyline>",
            "bored" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"8\" y1=\"15\" x2=\"16\" y2=\"15\"></line><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "sad" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M16 16s-1.5-2-4-2-4 2-4 2\"></path><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "angry" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M16 16s-1.5-2-4-2-4 2-4 2\"></path><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "anxious" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M16 16s-1.5-2-4-2-4 2-4 2\"></path><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "stressed" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"8\" y1=\"15\" x2=\"16\" y2=\"15\"></line><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>",
            "tired" => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"8\" y1=\"15\" x2=\"16\" y2=\"15\"></line><line x1=\"8\" y1=\"9\" x2=\"10\" y2=\"9\"></line><line x1=\"14\" y1=\"9\" x2=\"16\" y2=\"9\"></line>",
            _ => "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M8 14s1.5 2 4 2 4-2 4-2\"></path><line x1=\"9\" y1=\"9\" x2=\"9.01\" y2=\"9\"></line><line x1=\"15\" y1=\"9\" x2=\"15.01\" y2=\"9\"></line>"
        };
    }
}

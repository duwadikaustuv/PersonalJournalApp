using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Auth;
using PersonalJournalApp.Data;
using PersonalJournalApp.Models.Display;
using PersonalJournalApp.Services;

namespace PersonalJournalApp.Components.Pages;

public partial class TimelinePage : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private CustomAuthStateProvider CustomAuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private TagService TagService { get; set; } = default!;
    [Inject] private CategoryService CategoryService { get; set; } = default!;
    [Inject] private AppDbContext DbContext { get; set; } = default!;
    [Inject] private PdfExportService PdfExportService { get; set; } = default!;

    private bool isAuthorized = false;
    private bool isLoading = true;
    private bool showFilters = false;
    private string? currentUserId;

    // Selection mode
    private bool isSelectMode = false;
    private HashSet<int> selectedEntryIds = new();

    // All entries
    private List<JournalEntryDisplayModel> allEntries = new();
    private List<JournalEntryDisplayModel> filteredEntries = new();
    private List<JournalEntryDisplayModel> paginatedEntries = new();

    // Filter data
    private List<PersonalJournalApp.Entities.Tag> availableTags = new();
    private List<PersonalJournalApp.Entities.Category> availableCategories = new();

    // Search and filters
    private string searchQuery = string.Empty;
    private DateTime? filterStartDate;
    private DateTime? filterEndDate;
    private string filterMood = string.Empty;
    private string filterTag = string.Empty;
    private string filterCategory = string.Empty;
    private string sortOrder = "newest";

    // Export state
    private bool showExportMenu = false;
    private bool showDateRangeModal = false;
    private DateTime? exportStartDate;
    private DateTime? exportEndDate;
    private bool isExporting = false;
    private string exportError = "";
    private bool showExportSuccess = false;

    // Pagination
    private int currentPage = 1;
    private const int pageSize = 10;
    private int totalPages = 0;

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

        // Verify user exists
        var userExists = await DbContext.Users.AnyAsync(u => u.Id == currentUserId);
        if (!userExists)
        {
            await CustomAuthStateProvider.MarkUserAsLoggedOut();
            Navigation.NavigateTo("/login");
            return;
        }

        await LoadData();
        isAuthorized = true;
    }

    private async Task LoadData()
    {
        isLoading = true;

        // Load all entries
        var entriesResult = await JournalService.GetAllEntriesAsync(currentUserId!);
        if (entriesResult.Success && entriesResult.Data != null)
        {
            allEntries = entriesResult.Data;
        }

        // Load tags for filter dropdown
        var tagsResult = await TagService.GetAllTagsAsync(currentUserId!);
        if (tagsResult.Success && tagsResult.Data != null)
        {
            availableTags = tagsResult.Data.OrderBy(t => t.Name).ToList();
        }

        // Load categories for filter dropdown
        var categoriesResult = await CategoryService.GetAllCategoriesAsync(currentUserId!);
        if (categoriesResult.Success && categoriesResult.Data != null)
        {
            availableCategories = categoriesResult.Data;
        }

        ApplyFilters();
        isLoading = false;
    }

    private void ToggleFilters()
    {
        showFilters = !showFilters;
    }

    private void OnSearchChanged()
    {
        ApplyFilters();
    }

    private void ClearSearch()
    {
        searchQuery = string.Empty;
        ApplyFilters();
    }

    private void ClearFilters()
    {
        searchQuery = string.Empty;
        filterStartDate = null;
        filterEndDate = null;
        filterMood = string.Empty;
        filterTag = string.Empty;
        filterCategory = string.Empty;
        sortOrder = "newest";
        ApplyFilters();
    }

    private void StartSelectMode()
    {
        isSelectMode = true;
        selectedEntryIds.Clear();
    }

    private void CancelSelectMode()
    {
        isSelectMode = false;
        selectedEntryIds.Clear();
    }

    private void ToggleEntrySelection(int entryId)
    {
        if (selectedEntryIds.Contains(entryId))
            selectedEntryIds.Remove(entryId);
        else
            selectedEntryIds.Add(entryId);
    }

    private void ToggleSelectAll()
    {
        if (selectedEntryIds.Count == filteredEntries.Count)
        {
            selectedEntryIds.Clear();
        }
        else
        {
            selectedEntryIds = filteredEntries.Select(e => e.Id).ToHashSet();
        }
    }

    private void ToggleExportMenu()
    {
        showExportMenu = !showExportMenu;
    }

    private void CloseExportMenu()
    {
        showExportMenu = false;
    }

    private async Task ExportAllEntries()
    {
        CloseExportMenu();
        await ExportEntries(allEntries, "All Entries");
    }

    private async Task ExportFilteredEntries()
    {
        CloseExportMenu();
        await ExportEntries(filteredEntries, "Filtered Entries");
    }

    private async Task ExportEntries(List<JournalEntryDisplayModel> entries, string label)
    {
        if (entries.Count == 0) return;

        isExporting = true;
        StateHasChanged();

        try
        {
            var filePath = await PdfExportService.ExportMultipleEntriesAsync(entries);
            if (!string.IsNullOrEmpty(filePath))
            {
                showExportSuccess = true;
                StateHasChanged();

                await Task.Delay(3000);
                showExportSuccess = false;
            }
        }
        catch (Exception ex)
        {
            exportError = "Failed to export entries.";
        }
        finally
        {
            isExporting = false;
            StateHasChanged();
        }
    }

    private void OpenDateRangeExport()
    {
        CloseExportMenu();
        exportStartDate = DateTime.Now.AddMonths(-1);
        exportEndDate = DateTime.Now;
        exportError = "";
        showDateRangeModal = true;
    }

    private void CloseDateRangeModal()
    {
        showDateRangeModal = false;
        exportError = "";
    }

    private List<JournalEntryDisplayModel> GetEntriesInDateRange(DateTime start, DateTime end)
    {
        return allEntries.Where(e =>
            e.CreatedDate.Date >= start.Date &&
            e.CreatedDate.Date <= end.Date
        ).ToList();
    }

    private async Task ExportByDateRange()
    {
        if (!exportStartDate.HasValue || !exportEndDate.HasValue) return;

        if (exportStartDate.Value > exportEndDate.Value)
        {
            exportError = "Start date must be before end date.";
            return;
        }

        var entriesInRange = GetEntriesInDateRange(exportStartDate.Value, exportEndDate.Value);

        if (entriesInRange.Count == 0)
        {
            exportError = "No entries found in the selected date range.";
            return;
        }

        isExporting = true;
        exportError = "";
        StateHasChanged();

        try
        {
            var filePath = await PdfExportService.ExportEntriesByDateRangeAsync(
                entriesInRange,
                exportStartDate.Value,
                exportEndDate.Value
            );

            if (!string.IsNullOrEmpty(filePath))
            {
                CloseDateRangeModal();
                showExportSuccess = true;
                StateHasChanged();

                await Task.Delay(3000);
                showExportSuccess = false;
            }
            else
            {
                exportError = "Failed to generate PDF.";
            }
        }
        catch (Exception ex)
        {
            exportError = "An error occurred during export.";
        }
        finally
        {
            isExporting = false;
            StateHasChanged();
        }
    }

    private async Task ExportSelectedEntries()
    {
        if (selectedEntryIds.Count == 0) return;

        try
        {
            var selectedEntries = allEntries
                .Where(e => selectedEntryIds.Contains(e.Id))
                .OrderByDescending(e => e.CreatedDate)
                .ToList();

            var filePath = await PdfExportService.ExportMultipleEntriesAsync(selectedEntries);

            if (!string.IsNullOrEmpty(filePath))
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Export Successful",
                    $"{selectedEntries.Count} entries have been saved as PDF to:\n{filePath}",
                    "OK"
                );
                CancelSelectMode();
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
            await Application.Current!.MainPage!.DisplayAlert("Error", "Failed to export entries: " + ex.Message, "OK");
        }
    }

    private void ApplyFilters()
    {
        filteredEntries = allEntries.ToList();

        // Search by title or content
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var query = searchQuery.ToLower();
            filteredEntries = filteredEntries.Where(e =>
                e.Title.ToLower().Contains(query) ||
                StripHtml(e.Content).ToLower().Contains(query)
            ).ToList();
        }

        // Filter by date range
        if (filterStartDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.CreatedDate.Date >= filterStartDate.Value.Date).ToList();
        }
        if (filterEndDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.CreatedDate.Date <= filterEndDate.Value.Date).ToList();
        }

        // Filter by mood
        if (!string.IsNullOrWhiteSpace(filterMood))
        {
            filteredEntries = filteredEntries.Where(e =>
                e.PrimaryMood.Equals(filterMood, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(e.SecondaryMood1) && e.SecondaryMood1.Equals(filterMood, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.SecondaryMood2) && e.SecondaryMood2.Equals(filterMood, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        // Filter by tag
        if (!string.IsNullOrWhiteSpace(filterTag) && int.TryParse(filterTag, out int tagId))
        {
            var tagName = availableTags.FirstOrDefault(t => t.Id == tagId)?.Name;
            if (!string.IsNullOrEmpty(tagName))
            {
                filteredEntries = filteredEntries.Where(e => e.TagNames.Contains(tagName)).ToList();
            }
        }

        // Filter by category
        if (!string.IsNullOrWhiteSpace(filterCategory) && int.TryParse(filterCategory, out int categoryId))
        {
            var categoryName = availableCategories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            if (!string.IsNullOrEmpty(categoryName))
            {
                filteredEntries = filteredEntries.Where(e => e.CategoryName == categoryName).ToList();
            }
        }

        // Sort
        filteredEntries = sortOrder switch
        {
            "oldest" => filteredEntries.OrderBy(e => e.CreatedDate).ToList(),
            "words-desc" => filteredEntries.OrderByDescending(e => e.WordCount).ToList(),
            "words-asc" => filteredEntries.OrderBy(e => e.WordCount).ToList(),
            _ => filteredEntries.OrderByDescending(e => e.CreatedDate).ToList() // newest (default)
        };

        // Reset to page 1 when filters change
        currentPage = 1;
        UpdatePagination();
    }

    private void UpdatePagination()
    {
        totalPages = (int)Math.Ceiling((double)filteredEntries.Count / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (currentPage > totalPages) currentPage = totalPages;

        var skip = (currentPage - 1) * pageSize;
        paginatedEntries = filteredEntries.Skip(skip).Take(pageSize).ToList();
    }

    private void GoToPage(int page)
    {
        currentPage = page;
        UpdatePagination();
    }

    private void PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            UpdatePagination();
        }
    }

    private void NextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            UpdatePagination();
        }
    }

    private bool IsToday(DateTime dateTime)
    {
        return dateTime.ToLocalTime().Date == DateTime.Now.Date;
    }

    private void ViewEntry(int entryId)
    {
        Navigation.NavigateTo($"/entry/{entryId}");
    }

    private async Task DeleteEntry(int entryId)
    {
        bool confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Delete Entry",
            "Are you sure you want to delete this entry? This action cannot be undone.",
            "Delete",
            "Cancel"
        );

        if (!confirmed) return;

        var result = await JournalService.DeleteEntryAsync(currentUserId!, entryId);

        if (result.Success)
        {
            await Application.Current!.MainPage!.DisplayAlert("Success", "Entry deleted successfully.", "OK");
            await LoadData(); // Reload data
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

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // Simple HTML stripping - remove tags
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Trim();
    }
}
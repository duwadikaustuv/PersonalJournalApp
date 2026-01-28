using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Models.Input;
using PersonalJournalApp.Services;
using PersonalJournalApp.Auth;
using PersonalJournalApp.Data;
using PersonalJournalApp.Entities;

namespace PersonalJournalApp.Components.Pages;

public partial class TodayPage : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private CustomAuthStateProvider CustomAuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private CategoryService CategoryService { get; set; } = default!;
    [Inject] private TagService TagService { get; set; } = default!;
    [Inject] private AppDbContext DbContext { get; set; } = default!;

    private bool isAuthorized = false;
    private bool isFullscreen = false;
    private bool isSaving = false;
    private int wordCount = 0;
    private int characterCount = 0;
    private DateTime? lastSaved;
    private string? currentUserId;
    private int currentStreak = 0;

    private JournalEntryInputModel entryModel = new();
    private List<PersonalJournalApp.Entities.Category> categories = new();
    private List<PersonalJournalApp.Entities.Tag> prebuiltTags = new();
    private List<PersonalJournalApp.Entities.Tag> customTags = new();
    private List<string> secondaryMoods = new();
    private int? todayEntryId = null;

    private Dictionary<string, List<(string Value, string Label)>> moodsByCategory = new()
    {
        {
            "Positive", new List<(string, string)>
            {
                ("happy", "Happy"),
                ("excited", "Excited"),
                ("relaxed", "Relaxed"),
                ("grateful", "Grateful"),
                ("confident", "Confident")
            }
        },
        {
            "Neutral", new List<(string, string)>
            {
                ("calm", "Calm"),
                ("thoughtful", "Thoughtful"),
                ("curious", "Curious"),
                ("nostalgic", "Nostalgic"),
                ("bored", "Bored")
            }
        },
        {
            "Negative", new List<(string, string)>
            {
                ("sad", "Sad"),
                ("angry", "Angry"),
                ("stressed", "Stressed"),
                ("lonely", "Lonely"),
                ("anxious", "Anxious")
            }
        }
    };

    private async Task CalculateCurrentStreak()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        var allEntriesResult = await JournalService.GetAllEntriesAsync(currentUserId);
        if (!allEntriesResult.Success || allEntriesResult.Data == null || !allEntriesResult.Data.Any())
        {
            currentStreak = 0;
            return;
        }

        var entryDates = allEntriesResult.Data
            .Select(e => e.CreatedDate.ToLocalTime().Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var today = DateTime.Now.Date;
        var yesterday = today.AddDays(-1);

        int streak = 0;
        var checkDate = entryDates.Contains(today) ? today : yesterday;

        if (entryDates.Contains(today) || entryDates.Contains(yesterday))
        {
            while (entryDates.Contains(checkDate))
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
        }
        currentStreak = streak;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
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

        // handle the case where the database was recreated but old userId is in Preferences
        var userExists = await DbContext.Users.AnyAsync(u => u.Id == currentUserId);
        if (!userExists)
        {
            // User doesn't exist in database - clear auth and redirect to login
            await CustomAuthStateProvider.MarkUserAsLoggedOut();
            Navigation.NavigateTo("/login");
            return;
        }

        // Load categories
        var categoriesResult = await CategoryService.GetAllCategoriesAsync(currentUserId);
        if (categoriesResult.Success && categoriesResult.Data != null)
        {
            categories = categoriesResult.Data;

            // Seed default categories if none exist
            if (categories.Count == 0)
            {
                try
                {
                    var defaultCategories = new[] { "Personal", "Work", "Health", "Travel", "Goals" };
                    foreach (var categoryName in defaultCategories)
                    {
                        await CategoryService.CreateCategoryAsync(currentUserId, new CategoryInputModel { Name = categoryName });
                    }
                    // Reload categories after seeding
                    categoriesResult = await CategoryService.GetAllCategoriesAsync(currentUserId);
                    if (categoriesResult.Success && categoriesResult.Data != null)
                    {
                        categories = categoriesResult.Data;
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the error gracefully
                    Console.WriteLine($"Failed to seed categories: {ex.Message}");
                }
            }
        }

        // Load tags and separate pre-built from custom
        var tagsResult = await TagService.GetAllTagsAsync(currentUserId);
        if (tagsResult.Success && tagsResult.Data != null)
        {
            var allTags = tagsResult.Data;

            // Seed pre-built tags if none exist
            if (!allTags.Any(t => t.IsPrebuilt))
            {
                try
                {
                    await TagSeeder.SeedTagsForUserAsync(DbContext, currentUserId);
                    // Reload tags after seeding
                    tagsResult = await TagService.GetAllTagsAsync(currentUserId);
                    if (tagsResult.Success && tagsResult.Data != null)
                    {
                        allTags = tagsResult.Data;
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the error gracefully
                    Console.WriteLine($"Failed to seed tags: {ex.Message}");
                }
            }

            prebuiltTags = allTags.Where(t => t.IsPrebuilt).OrderBy(t => t.Name).ToList();
            customTags = allTags.Where(t => !t.IsPrebuilt).OrderBy(t => t.Name).ToList();
        }

        // Load today's entry
        await LoadTodaysEntryAsync();

        isAuthorized = true;
        await CalculateCurrentStreak();
    }

    private async Task LoadTodaysEntryAsync()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        // Use UTC date range for the entire day
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1); // End of day

        var result = await JournalService.GetEntriesByDateRangeAsync(currentUserId, todayStart, todayEnd);

        if (result.Success && result.Data != null && result.Data.Any())
        {
            var todaysEntry = result.Data.First();
            todayEntryId = todaysEntry.Id;
            entryModel.Title = todaysEntry.Title;
            entryModel.Content = todaysEntry.Content;
            entryModel.PrimaryMood = todaysEntry.PrimaryMood;
            entryModel.SecondaryMood1 = todaysEntry.SecondaryMood1;
            entryModel.SecondaryMood2 = todaysEntry.SecondaryMood2;

            // Load the category ID if exists
            var fullEntry = await DbContext.JournalEntries
                .Include(e => e.Category)
                .Include(e => e.Tags)
                .FirstOrDefaultAsync(e => e.Id == todaysEntry.Id);

            if (fullEntry != null)
            {
                entryModel.CategoryId = fullEntry.CategoryId;
                entryModel.SelectedTagIds = fullEntry.Tags.Select(t => t.Id).ToList();
            }

            // Populate secondary moods list
            secondaryMoods.Clear();
            if (!string.IsNullOrEmpty(todaysEntry.SecondaryMood1))
                secondaryMoods.Add(todaysEntry.SecondaryMood1);
            if (!string.IsNullOrEmpty(todaysEntry.SecondaryMood2))
                secondaryMoods.Add(todaysEntry.SecondaryMood2);

            // Calculate word count
            wordCount = todaysEntry.WordCount;
            characterCount = todaysEntry.Content.Length;
        }
        else
        {
            // No entry for today - initialize with default values
            entryModel = new JournalEntryInputModel { PrimaryMood = "neutral" };
            secondaryMoods.Clear();
            todayEntryId = null;
            wordCount = 0;
            characterCount = 0;
        }

        isAuthorized = true;
    }

    private void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
    }

    private void HandleContentChange(string htmlContent)
    {
        entryModel.Content = htmlContent;
        characterCount = htmlContent.Length;
    }

    private void HandleWordCountChange(int count)
    {
        wordCount = count;
    }

    private void ToggleTag(int tagId, bool isChecked)
    {
        if (isChecked)
        {
            if (!entryModel.SelectedTagIds.Contains(tagId))
                entryModel.SelectedTagIds.Add(tagId);
        }
        else
        {
            entryModel.SelectedTagIds.Remove(tagId);
        }
    }

    private void ToggleSecondaryMood(string moodValue, bool isChecked)
    {
        if (isChecked)
        {
            if (secondaryMoods.Count < 2 && !secondaryMoods.Contains(moodValue))
            {
                secondaryMoods.Add(moodValue);
            }
        }
        else
        {
            secondaryMoods.Remove(moodValue);
        }

        // Update model
        entryModel.SecondaryMood1 = secondaryMoods.Count > 0 ? secondaryMoods[0] : null;
        entryModel.SecondaryMood2 = secondaryMoods.Count > 1 ? secondaryMoods[1] : null;
    }

    private async Task SaveEntry()
    {
        if (string.IsNullOrWhiteSpace(entryModel.Content))
        {
            await Application.Current!.MainPage!.DisplayAlert("Validation Error", "Please write some content for your entry.", "OK");
            return;
        }

        isSaving = true;
        var userId = CustomAuthStateProvider.GetCurrentUserId();

        try
        {
            Common.ServiceResult result;

            if (todayEntryId.HasValue)
            {
                // Update existing entry
                result = await JournalService.UpdateEntryAsync(userId!, todayEntryId.Value, entryModel);
            }
            else
            {
                // Create new entry
                var createResult = await JournalService.CreateEntryAsync(userId!, entryModel);
                result = createResult;
                if (createResult.Success)
                {
                    todayEntryId = createResult.Data;
                }
            }

            if (result.Success)
            {
                lastSaved = DateTime.Now;
                await Application.Current!.MainPage!.DisplayAlert("Success", "Entry saved successfully!", "OK");
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", result.ErrorMessage ?? "Failed to save entry.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task DeleteEntry()
    {
        if (!todayEntryId.HasValue) return;

        bool confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Confirm Delete",
            "Are you sure you want to delete today's entry? This action cannot be undone.",
            "Delete",
            "Cancel"
        );

        if (!confirmed) return;

        var userId = CustomAuthStateProvider.GetCurrentUserId();
        var result = await JournalService.DeleteEntryAsync(userId!, todayEntryId.Value);

        if (result.Success)
        {
            await Application.Current!.MainPage!.DisplayAlert("Success", "Entry deleted successfully.", "OK");

            // Clear the form
            todayEntryId = null;
            entryModel = new() { PrimaryMood = "calm" };
            secondaryMoods.Clear();
            wordCount = 0;
            characterCount = 0;
            lastSaved = null;
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.ErrorMessage ?? "Failed to delete entry.", "OK");
        }
    }

    private string GetPrimaryMoodIcon(string moodCategory)
    {
        return moodCategory switch
        {
            "positive" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><path d=""M8 14s1.5 2 4 2 4-2 4-2""></path><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>",
            "neutral" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><line x1=""8"" y1=""15"" x2=""16"" y2=""15""></line><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>",
            "negative" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><path d=""M16 16s-1.5-2-4-2-4 2-4 2""></path><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>",
            _ => @"<circle cx=""12"" cy=""12"" r=""10""></circle><line x1=""8"" y1=""15"" x2=""16"" y2=""15""></line><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>"
        };
    }

    private string GetMoodIconPath(string moodValue)
    {
        return moodValue switch
        {
            "happy" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><path d=""M8 14s1.5 2 4 2 4-2 4-2""></path><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>",
            "sad" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><path d=""M16 16s-1.5-2-4-2-4 2-4 2""></path><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>",
            "calm" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><line x1=""8"" y1=""15"" x2=""16"" y2=""15""></line><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>",
            "anxious" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><path d=""M8 15h8""></path><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>",
            "excited" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><path d=""M8 14s1.5 2 4 2 4-2 4-2""></path><circle cx=""9"" cy=""9"" r=""1""></circle><circle cx=""15"" cy=""9"" r=""1""></circle>",
            "stressed" => @"<circle cx=""12"" cy=""12"" r=""10""></circle><line x1=""8"" y1=""15"" x2=""16"" y2=""15""></line><line x1=""7"" y1=""9"" x2=""11"" y2=""9""></line><line x1=""13"" y1=""9"" x2=""17"" y2=""9""></line>",
            _ => @"<circle cx=""12"" cy=""12"" r=""10""></circle><line x1=""8"" y1=""15"" x2=""16"" y2=""15""></line><line x1=""9"" y1=""9"" x2=""9.01"" y2=""9""></line><line x1=""15"" y1=""9"" x2=""15.01"" y2=""9""></line>"
        };
    }
}
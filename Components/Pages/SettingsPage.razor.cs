using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Auth;
using PersonalJournalApp.Data;
using PersonalJournalApp.Models.Input;
using PersonalJournalApp.Services;

namespace PersonalJournalApp.Components.Pages;

public partial class SettingsPage : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private CustomAuthStateProvider CustomAuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ThemeService ThemeService { get; set; } = default!;
    [Inject] private AppLockService AppLockService { get; set; } = default!;
    [Inject] private SettingsService SettingsService { get; set; } = default!;
    [Inject] private TagService TagService { get; set; } = default!;
    [Inject] private CategoryService CategoryService { get; set; } = default!;
    [Inject] private AppDbContext DbContext { get; set; } = default!;

    private bool isAuthorized = false;
    private string? currentUserId;
    private string currentUserEmail = "";
    private int totalEntries = 0;

    // Theme settings
    private string currentTheme = "Light";
    private string currentFontSize = "Medium";

    // Security settings
    private bool isAppLockEnabled = false;
    private bool hasPinSet = false;
    private bool lockOnSwitchApp = true;
    private bool lockOnMinimize = true;

    // Tag/Category management
    private bool showTagManagement = false;
    private bool showCategoryManagement = false;
    private string newTagName = "";
    private string newCategoryName = "";
    private List<PersonalJournalApp.Entities.Tag> customTags = new();
    private List<PersonalJournalApp.Entities.Category> categories = new();

    // PIN Modal
    private bool showPinModal = false;
    private int pinStep = 1;
    private string currentPinInput = "";
    private string newPinInput = "";
    private string confirmPinInput = "";
    private string pinError = "";
    private bool isProcessingPin = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        currentUserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            currentUserId = CustomAuthStateProvider.GetCurrentUserId();
        }

        if (string.IsNullOrEmpty(currentUserId))
        {
            Navigation.NavigateTo("/login");
            return;
        }

        isAuthorized = true;
        await LoadSettings();
    }

    private async Task LoadSettings()
    {
        // Load theme settings
        currentTheme = ThemeService.CurrentTheme;
        currentFontSize = ThemeService.CurrentFontSize;

        // Load security settings
        isAppLockEnabled = AppLockService.IsAppLockEnabled;
        hasPinSet = await AppLockService.HasPinSetAsync(currentUserId!);
        lockOnSwitchApp = AppLockService.LockOnSwitchApp;
        lockOnMinimize = AppLockService.LockOnMinimize;

        // Load user info
        var dbUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
        if (dbUser != null)
        {
            currentUserEmail = dbUser.Email ?? dbUser.UserName ?? "Unknown";
        }

        // Load entry count
        totalEntries = await DbContext.JournalEntries.CountAsync(e => e.UserId == currentUserId);

        // Load tags and categories
        await LoadTagsAndCategories();
    }

    private void ToggleLockOnSwitchApp()
    {
        lockOnSwitchApp = !lockOnSwitchApp;
        AppLockService.LockOnSwitchApp = lockOnSwitchApp;
        StateHasChanged();
    }

    private void ToggleLockOnMinimize()
    {
        lockOnMinimize = !lockOnMinimize;
        AppLockService.LockOnMinimize = lockOnMinimize;
        StateHasChanged();
    }

    private async Task LoadTagsAndCategories()
    {
        var tagsResult = await TagService.GetAllTagsAsync(currentUserId!);
        if (tagsResult.Success && tagsResult.Data != null)
        {
            customTags = tagsResult.Data.Where(t => !t.IsPrebuilt).OrderBy(t => t.Name).ToList();
        }

        var categoriesResult = await CategoryService.GetAllCategoriesAsync(currentUserId!);
        if (categoriesResult.Success && categoriesResult.Data != null)
        {
            categories = categoriesResult.Data.OrderBy(c => c.Name).ToList();
        }
    }

    private void SetTheme(string theme)
    {
        currentTheme = theme;
        ThemeService.CurrentTheme = theme;
    }

    private void OnFontSizeChanged(ChangeEventArgs e)
    {
        currentFontSize = e.Value?.ToString() ?? "Medium";
        ThemeService.CurrentFontSize = currentFontSize;
    }

    private async Task OnAppLockToggleClicked()
    {
        if (!hasPinSet)
        {
            await Application.Current!.MainPage!.DisplayAlert("PIN Required", "Please set a PIN first before enabling App Lock.", "OK");
            return;
        }

        // Toggle the state
        isAppLockEnabled = !isAppLockEnabled;
        AppLockService.IsAppLockEnabled = isAppLockEnabled;

        if (!isAppLockEnabled)
        {
            AppLockService.UnlockApp();
        }

        StateHasChanged();
    }

    private void OpenPinModal()
    {
        showPinModal = true;
        pinStep = 1;
        currentPinInput = "";
        newPinInput = "";
        confirmPinInput = "";
        pinError = "";
    }

    private void ClosePinModal()
    {
        showPinModal = false;
        pinStep = 1;
        currentPinInput = "";
        newPinInput = "";
        confirmPinInput = "";
        pinError = "";
    }

    private string GetPinButtonText()
    {
        if (hasPinSet)
        {
            return pinStep switch
            {
                1 => "Verify",
                2 => "Next",
                3 => "Save PIN",
                _ => "Continue"
            };
        }
        else
        {
            return pinStep switch
            {
                1 => "Next",
                2 => "Save PIN",
                _ => "Continue"
            };
        }
    }

    private async Task ProcessPinStep()
    {
        pinError = "";
        isProcessingPin = true;

        try
        {
            if (hasPinSet)
            {
                // Change PIN flow
                if (pinStep == 1)
                {
                    // Validate current PIN input
                    if (string.IsNullOrEmpty(currentPinInput) || currentPinInput.Length < 4)
                    {
                        pinError = "Please enter your current PIN.";
                        return;
                    }

                    // Verify current PIN
                    var isValid = await AppLockService.ValidatePinAsync(currentUserId!, currentPinInput);
                    if (!isValid)
                    {
                        pinError = "Current PIN is incorrect.";
                        currentPinInput = "";
                        return;
                    }
                    pinStep = 2;
                    currentPinInput = ""; // Clear for security
                }
                else if (pinStep == 2)
                {
                    // Validate new PIN format
                    if (string.IsNullOrEmpty(newPinInput))
                    {
                        pinError = "Please enter a new PIN.";
                        return;
                    }
                    if (newPinInput.Length < 4 || newPinInput.Length > 6)
                    {
                        pinError = "PIN must be 4-6 digits.";
                        return;
                    }
                    if (!newPinInput.All(char.IsDigit))
                    {
                        pinError = "PIN must contain only numbers.";
                        return;
                    }
                    pinStep = 3;
                }
                else if (pinStep == 3)
                {
                    // Confirm new PIN
                    if (string.IsNullOrEmpty(confirmPinInput))
                    {
                        pinError = "Please confirm your new PIN.";
                        return;
                    }
                    if (newPinInput != confirmPinInput)
                    {
                        pinError = "PINs do not match. Please try again.";
                        confirmPinInput = "";
                        return;
                    }

                    var success = await AppLockService.SetPinAsync(currentUserId!, newPinInput);
                    if (success)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Success", $"Your {newPinInput.Length}-digit PIN has been changed successfully.", "OK");
                        ClosePinModal();
                    }
                    else
                    {
                        pinError = "Failed to save PIN. Please try again.";
                    }
                }
            }
            else
            {
                // Set new PIN flow
                if (pinStep == 1)
                {
                    // Validate new PIN format
                    if (string.IsNullOrEmpty(newPinInput))
                    {
                        pinError = "Please enter a PIN.";
                        return;
                    }
                    if (newPinInput.Length < 4 || newPinInput.Length > 6)
                    {
                        pinError = "PIN must be 4-6 digits.";
                        return;
                    }
                    if (!newPinInput.All(char.IsDigit))
                    {
                        pinError = "PIN must contain only numbers.";
                        return;
                    }
                    pinStep = 2;
                }
                else if (pinStep == 2)
                {
                    // Confirm new PIN
                    if (string.IsNullOrEmpty(confirmPinInput))
                    {
                        pinError = "Please confirm your PIN.";
                        return;
                    }
                    if (newPinInput != confirmPinInput)
                    {
                        pinError = "PINs do not match. Please try again.";
                        confirmPinInput = "";
                        return;
                    }

                    var success = await AppLockService.SetPinAsync(currentUserId!, newPinInput);
                    if (success)
                    {
                        hasPinSet = true;
                        await Application.Current!.MainPage!.DisplayAlert("Success", $"Your {newPinInput.Length}-digit PIN has been set. You can now enable App Lock.", "OK");
                        ClosePinModal();
                    }
                    else
                    {
                        pinError = "Failed to save PIN. Please try again.";
                    }
                }
            }
        }
        finally
        {
            isProcessingPin = false;
            StateHasChanged();
        }
    }

    private async Task RemovePin()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Remove PIN",
            "Are you sure you want to remove your PIN? This will disable App Lock.",
            "Remove",
            "Cancel");

        if (!confirm) return;

        await AppLockService.RemovePinAsync(currentUserId!);
        hasPinSet = false;
        isAppLockEnabled = false;
        await Application.Current!.MainPage!.DisplayAlert("Success", "PIN has been removed.", "OK");
    }

    private void ToggleTagManagement()
    {
        showTagManagement = !showTagManagement;
    }

    private void ToggleCategoryManagement()
    {
        showCategoryManagement = !showCategoryManagement;
    }

    private async Task AddCustomTag()
    {
        if (string.IsNullOrWhiteSpace(newTagName)) return;

        var result = await TagService.CreateTagAsync(currentUserId!, new TagInputModel { Name = newTagName.Trim() });

        if (result.Success)
        {
            newTagName = "";
            await LoadTagsAndCategories();
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.ErrorMessage ?? "Failed to add tag.", "OK");
        }
    }

    private async Task DeleteTag(int tagId)
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert("Delete Tag", "Are you sure you want to delete this tag?", "Delete", "Cancel");
        if (!confirm) return;

        var result = await TagService.DeleteTagAsync(currentUserId!, tagId);
        if (result.Success)
        {
            await LoadTagsAndCategories();
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.ErrorMessage ?? "Failed to delete tag.", "OK");
        }
    }

    private async Task AddCategory()
    {
        if (string.IsNullOrWhiteSpace(newCategoryName)) return;

        var result = await CategoryService.CreateCategoryAsync(currentUserId!, new CategoryInputModel { Name = newCategoryName.Trim() });

        if (result.Success)
        {
            newCategoryName = "";
            await LoadTagsAndCategories();
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.ErrorMessage ?? "Failed to add category.", "OK");
        }
    }

    private async Task DeleteCategory(int categoryId)
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert("Delete Category", "Are you sure you want to delete this category?", "Delete", "Cancel");
        if (!confirm) return;

        var result = await CategoryService.DeleteCategoryAsync(currentUserId!, categoryId);
        if (result.Success)
        {
            await LoadTagsAndCategories();
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", result.ErrorMessage ?? "Failed to delete category.", "OK");
        }
    }

    private async Task Logout()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert("Logout", "Are you sure you want to logout?", "Logout", "Cancel");
        if (!confirm) return;

        AppLockService.LockApp();
        await CustomAuthStateProvider.MarkUserAsLoggedOut();
        Navigation.NavigateTo("/login", forceLoad: true);
    }
}
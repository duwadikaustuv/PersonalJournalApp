using Microsoft.AspNetCore.Components;
using PersonalJournalApp.Auth;
using PersonalJournalApp.Models.Input;
using PersonalJournalApp.Services;

namespace PersonalJournalApp.Components.Pages;

public partial class LoginPage : ComponentBase
{
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private CustomAuthStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private LoginInputModel loginModel = new();
    private bool isLoading = false;
    private bool showPassword = false;
    private string errorMessage = string.Empty;
    private string themeClass = "theme-light";

    // Keys for storing credentials
    private const string SavedUsernameKey = "saved_username";
    private const string SavedPasswordKey = "saved_password";
    private const string RememberMeKey = "remember_me";

    protected override void OnInitialized()
    {
        // Detect system theme for login page
        themeClass = ThemeService.GetSystemThemeClass();

        // Load saved credentials if Remember Me was checked
        LoadSavedCredentials();
    }

    private void LoadSavedCredentials()
    {
        var rememberMe = Preferences.Get(RememberMeKey, false);
        if (rememberMe)
        {
            loginModel.UserName = Preferences.Get(SavedUsernameKey, string.Empty);
            loginModel.Password = Preferences.Get(SavedPasswordKey, string.Empty);
            loginModel.RememberMe = true;
        }
    }

    private void SaveCredentials()
    {
        if (loginModel.RememberMe)
        {
            Preferences.Set(SavedUsernameKey, loginModel.UserName);
            Preferences.Set(SavedPasswordKey, loginModel.Password);
            Preferences.Set(RememberMeKey, true);
        }
        else
        {
            // Clear saved credentials
            Preferences.Remove(SavedUsernameKey);
            Preferences.Remove(SavedPasswordKey);
            Preferences.Set(RememberMeKey, false);
        }
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = string.Empty;

        var result = await AuthService.LoginAsync(loginModel);

        if (result.Success && result.Data != null)
        {
            // Save or clear credentials based on Remember Me
            SaveCredentials();

            await AuthStateProvider.MarkUserAsAuthenticated(result.Data.Id);
            Navigation.NavigateTo("/today");
        }
        else
        {
            errorMessage = result.ErrorMessage ?? "Login failed. Please try again.";
        }

        isLoading = false;
    }
}

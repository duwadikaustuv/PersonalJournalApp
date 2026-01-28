using Microsoft.AspNetCore.Components;
using PersonalJournalApp.Models.Input;
using PersonalJournalApp.Services;

namespace PersonalJournalApp.Components.Pages;

public partial class RegisterPage : ComponentBase
{
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private RegisterInputModel registerModel = new();
    private bool isLoading = false;
    private bool showPassword = false;
    private bool showConfirmPassword = false;
    private string errorMessage = string.Empty;
    private string successMessage = string.Empty;
    private string themeClass = "theme-light";

    protected override void OnInitialized()
    {
        // Detect system theme for register page
        themeClass = ThemeService.GetSystemThemeClass();
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private void ToggleConfirmPasswordVisibility()
    {
        showConfirmPassword = !showConfirmPassword;
    }

    private async Task HandleRegister()
    {
        isLoading = true;
        errorMessage = string.Empty;
        successMessage = string.Empty;

        var result = await AuthService.RegisterAsync(registerModel);

        if (result.Success)
        {
            successMessage = "Account created successfully! Redirecting to login...";
            await Task.Delay(1500);
            Navigation.NavigateTo("/login");
        }
        else
        {
            errorMessage = result.ErrorMessage ?? "Registration failed. Please try again.";
        }

        isLoading = false;
    }
}

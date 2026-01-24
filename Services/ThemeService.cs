using System;

namespace PersonalJournalApp.Services
{
    public class ThemeService
    {
        private const string ThemeKey = "app_theme";
        private const string FontSizeKey = "app_fontsize";

        public event Action? OnThemeChanged;

        public string CurrentTheme
        {
            get => Preferences.Get(ThemeKey, "System"); // Default to System
            set
            {
                Preferences.Set(ThemeKey, value);
                OnThemeChanged?.Invoke();
            }
        }

        public string CurrentFontSize
        {
            get => Preferences.Get(FontSizeKey, "Medium");
            set
            {
                Preferences.Set(FontSizeKey, value);
                OnThemeChanged?.Invoke();
            }
        }

        // Gets whether the effective theme is dark mode (considering System preference)
        public bool IsDarkMode
        {
            get
            {
                if (CurrentTheme == "System")
                {
                    return GetSystemThemeIsDark();
                }
                return CurrentTheme == "Dark";
            }
        }

        // Detects if the system is using dark mode
        public static bool GetSystemThemeIsDark()
        {
            // Use MAUI's AppInfo to detect current app theme
            return Application.Current?.RequestedTheme == AppTheme.Dark;
        }

        public string GetThemeClass()
        {
            if (CurrentTheme == "System")
            {
                return GetSystemThemeIsDark() ? "theme-dark" : "theme-light";
            }

            return CurrentTheme switch
            {
                "Dark" => "theme-dark",
                "Light" => "theme-light",
                _ => "theme-light"
            };
        }

        // Gets theme class based on system preference (for login/register pages)
        public static string GetSystemThemeClass()
        {
            return GetSystemThemeIsDark() ? "theme-dark" : "theme-light";
        }

        public string GetFontSizeClass()
        {
            return CurrentFontSize switch
            {
                "Small" => "font-small",
                "Large" => "font-large",
                _ => "font-medium"
            };
        }
    }
}
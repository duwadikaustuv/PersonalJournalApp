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
            get => Preferences.Get(ThemeKey, "Light");
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

        public bool IsDarkMode => CurrentTheme == "Dark";

        public string GetThemeClass()
        {
            return CurrentTheme switch
            {
                "Dark" => "theme-dark",
                "Light" => "theme-light",
                _ => "" // Auto - handled by system
            };
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
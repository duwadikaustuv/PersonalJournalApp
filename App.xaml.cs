using PersonalJournalApp.Services;

namespace PersonalJournalApp
{
    public partial class App : Application
    {
        private readonly AppLockService? _appLockService;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "PersonalJournalApp" };

            // Get AppLockService from the handler's service provider
            window.Created += (s, e) =>
            {
                if (Handler?.MauiContext?.Services != null)
                {
                    var appLockService = Handler.MauiContext.Services.GetService<AppLockService>();
                    if (appLockService != null)
                    {
                        SetupLifecycleHandlers(window, appLockService);
                    }
                }
            };

            return window;
        }

        private void SetupLifecycleHandlers(Window window, AppLockService appLockService)
        {
            // Lock app when user switches to another app (if enabled)
            window.Deactivated += (s, e) =>
            {
                if (appLockService.IsAppLockEnabled && appLockService.LockOnSwitchApp)
                {
                    appLockService.LockApp();
                }
            };

            // Lock app when it's stopped/backgrounded (if enabled)
            window.Stopped += (s, e) =>
            {
                if (appLockService.IsAppLockEnabled && appLockService.LockOnMinimize)
                {
                    appLockService.LockApp();
                }
            };

            // Always lock when app is being destroyed
            window.Destroying += (s, e) =>
            {
                if (appLockService.IsAppLockEnabled)
                {
                    appLockService.LockApp();
                }
            };
        }

        protected override void OnSleep()
        {
            base.OnSleep();

            // Lock when app goes to sleep
            if (Handler?.MauiContext?.Services != null)
            {
                var appLockService = Handler.MauiContext.Services.GetService<AppLockService>();
                if (appLockService?.IsAppLockEnabled == true && appLockService.LockOnMinimize)
                {
                    appLockService.LockApp();
                }
            }
        }
    }
}
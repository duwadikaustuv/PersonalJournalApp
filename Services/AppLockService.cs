using System;
using System.Threading.Tasks;
using PersonalJournalApp.Data;
using Microsoft.EntityFrameworkCore;

namespace PersonalJournalApp.Services
{
    public class AppLockService
    {
        private const string AppLockEnabledKey = "app_lock_enabled";
        private const string IsUnlockedKey = "app_is_unlocked";
        private readonly AppDbContext _context;

        public event Action? OnLockStateChanged;

        public AppLockService(AppDbContext context)
        {
            _context = context;
        }

        public bool IsAppLockEnabled
        {
            get => Preferences.Get(AppLockEnabledKey, false);
            set => Preferences.Set(AppLockEnabledKey, value);
        }

        public bool IsUnlocked
        {
            get => Preferences.Get(IsUnlockedKey, false);
            private set
            {
                Preferences.Set(IsUnlockedKey, value);
                OnLockStateChanged?.Invoke();
            }
        }

        public void LockApp()
        {
            IsUnlocked = false;
        }

        public void UnlockApp()
        {
            IsUnlocked = true;
        }

        public async Task<bool> ValidatePinAsync(string userId, string enteredPin)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(enteredPin))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            return user.PIN == enteredPin;
        }

        public async Task<bool> HasPinSetAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user != null && !string.IsNullOrEmpty(user.PIN);
        }

        public async Task<bool> SetPinAsync(string userId, string newPin)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            user.PIN = newPin;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemovePinAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            user.PIN = null;
            IsAppLockEnabled = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // Called when app starts or resumes
        public void ResetLockOnAppStart()
        {
            if (IsAppLockEnabled)
            {
                IsUnlocked = false;
            }
        }
    }
}
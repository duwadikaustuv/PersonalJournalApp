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
        private const string PinLengthKey = "app_pin_length";
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


        // Gets the length of the stored PIN (4, 5, or 6 digits)
        public int GetPinLength()
        {
            return Preferences.Get(PinLengthKey, 4); // Default to 4 if not set
        }

        // Sets the PIN length in preferences
        private void SetPinLength(int length)
        {
            Preferences.Set(PinLengthKey, length);
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
            if (user == null || string.IsNullOrEmpty(user.PIN))
                return false;

            // Compare the entered PIN with the stored PIN
            return user.PIN == enteredPin;
        }

        public async Task<bool> HasPinSetAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user != null && !string.IsNullOrEmpty(user.PIN);
        }

        // Gets the length of the PIN stored for the user
        public async Task<int> GetUserPinLengthAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return 4;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || string.IsNullOrEmpty(user.PIN))
                return 4;

            return user.PIN.Length;
        }

        public async Task<bool> SetPinAsync(string userId, string newPin)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            // Validate PIN format
            if (string.IsNullOrEmpty(newPin) || newPin.Length < 4 || newPin.Length > 6 || !newPin.All(char.IsDigit))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            user.PIN = newPin;
            await _context.SaveChangesAsync();

            // Store the PIN length in preferences for quick access
            SetPinLength(newPin.Length);

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

            // Clear the stored PIN length
            Preferences.Remove(PinLengthKey);

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
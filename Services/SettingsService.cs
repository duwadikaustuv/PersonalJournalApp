using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Common;
using PersonalJournalApp.Data;
using PersonalJournalApp.Entities;
using PersonalJournalApp.Models.Input;

namespace PersonalJournalApp.Services
{
    public class SettingsService
    {
        private readonly AppDbContext _context;

        public SettingsService(AppDbContext context)
        {
            _context = context;
        }

        // Get user settings
        public async Task<ServiceResult<UserSettings>> GetSettingsAsync(string userId)
        {
            try
            {
                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    // Create default settings if none exist
                    settings = await CreateDefaultSettingsAsync(userId);
                }

                return ServiceResult<UserSettings>.SuccessResult(settings);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserSettings>.FailureResult($"Failed to retrieve settings: {ex.Message}");
            }
        }

        // Update user settings
        public async Task<ServiceResult> UpdateSettingsAsync(string userId, UserSettingsInputModel model)
        {
            try
            {
                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    settings = new UserSettings
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserSettings.Add(settings);
                }

                settings.Theme = model.Theme;
                settings.FontSize = model.FontSize;
                settings.BiometricUnlock = model.BiometricUnlock;
                settings.AutoBackup = model.AutoBackup;
                settings.ModifiedAt = DateTime.UtcNow;

                // Update PIN in User entity if provided
                if (!string.IsNullOrEmpty(model.PIN))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.PIN = model.PIN;
                    }
                }

                await _context.SaveChangesAsync();
                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to update settings: {ex.Message}");
            }
        }

        // Create default settings for new user
        public async Task<UserSettings> CreateDefaultSettingsAsync(string userId)
        {
            var settings = new UserSettings
            {
                UserId = userId,
                Theme = "Light",
                FontSize = "Medium",
                BiometricUnlock = false,
                AutoBackup = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();

            return settings;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PersonalJournalApp.Common;
using PersonalJournalApp.Entities;
using PersonalJournalApp.Models.Input;

namespace PersonalJournalApp.Services
{
    public class AuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SettingsService _settingsService;
        private readonly CategoryService _categoryService;

        public AuthService(
            UserManager<User> userManager,
            SettingsService settingsService,
            CategoryService categoryService)
        {
            _userManager = userManager;
            _settingsService = settingsService;
            _categoryService = categoryService;
        }

        // Register new user
        public async Task<ServiceResult> RegisterAsync(RegisterInputModel model)
        {
            try
            {
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ServiceResult.FailureResult($"Registration failed: {errors}");
                }

                // Create default settings
                await _settingsService.CreateDefaultSettingsAsync(user.Id);

                // Create default categories
                var defaultCategories = new[] { "Personal", "Work", "Health", "Travel", "Goals" };
                foreach (var categoryName in defaultCategories)
                {
                    await _categoryService.CreateCategoryAsync(user.Id, new CategoryInputModel { Name = categoryName });
                }

                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Registration failed: {ex.Message}");
            }
        }

        // Login user
        public async Task<ServiceResult<User>> LoginAsync(LoginInputModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user == null)
                    return ServiceResult<User>.FailureResult("Invalid username or password");

                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!passwordValid)
                    return ServiceResult<User>.FailureResult("Invalid username or password");

                return ServiceResult<User>.SuccessResult(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.FailureResult($"Login failed: {ex.Message}");
            }
        }

        // Update user PIN
        public async Task<ServiceResult> UpdatePINAsync(string userId, string pin)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return ServiceResult.FailureResult("User not found");

                user.PIN = pin;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ServiceResult.FailureResult($"Failed to update PIN: {errors}");
                }

                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to update PIN: {ex.Message}");
            }
        }

        // Get current user
        public async Task<ServiceResult<User>> GetCurrentUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return ServiceResult<User>.FailureResult("User not found");

                return ServiceResult<User>.SuccessResult(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.FailureResult($"Failed to retrieve user: {ex.Message}");
            }
        }
    }
}
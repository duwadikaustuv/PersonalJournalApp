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
    public class CategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        // Get all categories for user
        public async Task<ServiceResult<List<Category>>> GetAllCategoriesAsync(string userId)
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return ServiceResult<List<Category>>.SuccessResult(categories);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Category>>.FailureResult($"Failed to retrieve categories: {ex.Message}");
            }
        }

        // Create new category
        public async Task<ServiceResult<int>> CreateCategoryAsync(string userId, CategoryInputModel model)
        {
            try
            {
                // Check if category already exists for this user
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.Name.ToLower() == model.Name.ToLower());

                if (existingCategory != null)
                    return ServiceResult<int>.FailureResult("A category with this name already exists");

                var category = new Category
                {
                    Name = model.Name,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return ServiceResult<int>.SuccessResult(category.Id);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.FailureResult($"Failed to create category: {ex.Message}");
            }
        }

        // Update category
        public async Task<ServiceResult> UpdateCategoryAsync(string userId, int categoryId, CategoryInputModel model)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

                if (category == null)
                    return ServiceResult.FailureResult("Category not found");

                // Check if another category with same name exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.Id != categoryId && c.Name.ToLower() == model.Name.ToLower());

                if (existingCategory != null)
                    return ServiceResult.FailureResult("A category with this name already exists");

                category.Name = model.Name;
                await _context.SaveChangesAsync();

                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to update category: {ex.Message}");
            }
        }

        // Delete category
        public async Task<ServiceResult> DeleteCategoryAsync(string userId, int categoryId)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

                if (category == null)
                    return ServiceResult.FailureResult("Category not found");

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to delete category: {ex.Message}");
            }
        }

        // Get category entry count (how many entries use this category)
        public async Task<ServiceResult<int>> GetCategoryEntryCountAsync(string userId, int categoryId)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.JournalEntries)
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

                if (category == null)
                    return ServiceResult<int>.FailureResult("Category not found");

                return ServiceResult<int>.SuccessResult(category.JournalEntries.Count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.FailureResult($"Failed to get category entry count: {ex.Message}");
            }
        }
    }
}

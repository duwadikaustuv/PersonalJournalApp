using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalJournalApp.Common;
using PersonalJournalApp.Data;
using PersonalJournalApp.Entities;
using PersonalJournalApp.Models.Display;
using PersonalJournalApp.Models.Input;

namespace PersonalJournalApp.Services
{
    public class JournalService
    {
        private readonly AppDbContext _context;

        public JournalService(AppDbContext context)
        {
            _context = context;
        }

        // Create new journal entry
        public async Task<ServiceResult<int>> CreateEntryAsync(string userId, JournalEntryInputModel model)
        {
            try
            {
                var entry = new JournalEntry
                {
                    Title = model.Title,
                    Content = model.Content,
                    Mood = model.Mood,
                    UserId = userId,
                    CategoryId = model.CategoryId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.JournalEntries.Add(entry);
                await _context.SaveChangesAsync();

                // Add tags if any selected
                if (model.SelectedTagIds?.Any() == true)
                {
                    var tags = await _context.Tags
                        .Where(t => model.SelectedTagIds.Contains(t.Id) && t.UserId == userId)
                        .ToListAsync();

                    entry.Tags = tags;
                    await _context.SaveChangesAsync();
                }

                return ServiceResult<int>.SuccessResult(entry.Id);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.FailureResult($"Failed to create entry: {ex.Message}");
            }
        }

        // Update existing entry
        public async Task<ServiceResult> UpdateEntryAsync(string userId, int entryId, JournalEntryInputModel model)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

                if (entry == null)
                    return ServiceResult.FailureResult("Entry not found");

                entry.Title = model.Title;
                entry.Content = model.Content;
                entry.Mood = model.Mood;
                entry.CategoryId = model.CategoryId;
                entry.ModifiedDate = DateTime.UtcNow;

                // Update tags
                entry.Tags.Clear();
                if (model.SelectedTagIds?.Any() == true)
                {
                    var tags = await _context.Tags
                        .Where(t => model.SelectedTagIds.Contains(t.Id) && t.UserId == userId)
                        .ToListAsync();
                    entry.Tags = tags;
                }

                await _context.SaveChangesAsync();
                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to update entry: {ex.Message}");
            }
        }

        // Delete entry
        public async Task<ServiceResult> DeleteEntryAsync(string userId, int entryId)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

                if (entry == null)
                    return ServiceResult.FailureResult("Entry not found");

                _context.JournalEntries.Remove(entry);
                await _context.SaveChangesAsync();
                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to delete entry: {ex.Message}");
            }
        }

        // Get single entry by ID
        public async Task<ServiceResult<JournalEntryDisplayModel>> GetEntryByIdAsync(string userId, int entryId)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Category)
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

                if (entry == null)
                    return ServiceResult<JournalEntryDisplayModel>.FailureResult("Entry not found");

                var displayModel = MapToDisplayModel(entry);
                return ServiceResult<JournalEntryDisplayModel>.SuccessResult(displayModel);
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntryDisplayModel>.FailureResult($"Failed to retrieve entry: {ex.Message}");
            }
        }

        // Get all entries for user
        public async Task<ServiceResult<List<JournalEntryDisplayModel>>> GetAllEntriesAsync(string userId)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Category)
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId)
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();

                var displayModels = entries.Select(MapToDisplayModel).ToList();
                return ServiceResult<List<JournalEntryDisplayModel>>.SuccessResult(displayModels);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntryDisplayModel>>.FailureResult($"Failed to retrieve entries: {ex.Message}");
            }
        }

        // Get entries by date range
        public async Task<ServiceResult<List<JournalEntryDisplayModel>>> GetEntriesByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Category)
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId && e.CreatedDate >= startDate && e.CreatedDate <= endDate)
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();

                var displayModels = entries.Select(MapToDisplayModel).ToList();
                return ServiceResult<List<JournalEntryDisplayModel>>.SuccessResult(displayModels);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntryDisplayModel>>.FailureResult($"Failed to retrieve entries: {ex.Message}");
            }
        }

        // Search entries by keyword
        public async Task<ServiceResult<List<JournalEntryDisplayModel>>> SearchEntriesAsync(string userId, string keyword)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Category)
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId &&
                           (e.Title.Contains(keyword) || e.Content.Contains(keyword)))
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();

                var displayModels = entries.Select(MapToDisplayModel).ToList();
                return ServiceResult<List<JournalEntryDisplayModel>>.SuccessResult(displayModels);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntryDisplayModel>>.FailureResult($"Failed to search entries: {ex.Message}");
            }
        }

        // Filter entries by mood
        public async Task<ServiceResult<List<JournalEntryDisplayModel>>> GetEntriesByMoodAsync(string userId, string mood)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Category)
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId && e.Mood == mood)
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();

                var displayModels = entries.Select(MapToDisplayModel).ToList();
                return ServiceResult<List<JournalEntryDisplayModel>>.SuccessResult(displayModels);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntryDisplayModel>>.FailureResult($"Failed to filter entries: {ex.Message}");
            }
        }

        // Get entries by category
        public async Task<ServiceResult<List<JournalEntryDisplayModel>>> GetEntriesByCategoryAsync(string userId, int categoryId)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Category)
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId && e.CategoryId == categoryId)
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();

                var displayModels = entries.Select(MapToDisplayModel).ToList();
                return ServiceResult<List<JournalEntryDisplayModel>>.SuccessResult(displayModels);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntryDisplayModel>>.FailureResult($"Failed to retrieve entries: {ex.Message}");
            }
        }

        // Get entries by tag
        public async Task<ServiceResult<List<JournalEntryDisplayModel>>> GetEntriesByTagAsync(string userId, int tagId)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Category)
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId && e.Tags.Any(t => t.Id == tagId))
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();

                var displayModels = entries.Select(MapToDisplayModel).ToList();
                return ServiceResult<List<JournalEntryDisplayModel>>.SuccessResult(displayModels);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntryDisplayModel>>.FailureResult($"Failed to retrieve entries: {ex.Message}");
            }
        }

        // Map entity to display model
        private JournalEntryDisplayModel MapToDisplayModel(JournalEntry entry)
        {
            return new JournalEntryDisplayModel
            {
                Id = entry.Id,
                Title = entry.Title,
                Content = entry.Content,
                Mood = entry.Mood,
                CreatedDate = entry.CreatedDate,
                ModifiedDate = entry.ModifiedDate,
                CategoryName = entry.Category?.Name,
                TagNames = entry.Tags.Select(t => t.Name).ToList()
            };
        }
    }
}

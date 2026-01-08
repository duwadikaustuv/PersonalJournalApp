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
    public class TagService
    {
        private readonly AppDbContext _context;

        public TagService(AppDbContext context)
        {
            _context = context;
        }

        // Get all tags for user
        public async Task<ServiceResult<List<Tag>>> GetAllTagsAsync(string userId)
        {
            try
            {
                var tags = await _context.Tags
                    .Where(t => t.UserId == userId)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                return ServiceResult<List<Tag>>.SuccessResult(tags);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Tag>>.FailureResult($"Failed to retrieve tags: {ex.Message}");
            }
        }

        // Create new tag
        public async Task<ServiceResult<int>> CreateTagAsync(string userId, TagInputModel model)
        {
            try
            {
                // Check if tag already exists for this user
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.Name.ToLower() == model.Name.ToLower());

                if (existingTag != null)
                    return ServiceResult<int>.FailureResult("A tag with this name already exists");

                var tag = new Tag
                {
                    Name = model.Name,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();

                return ServiceResult<int>.SuccessResult(tag.Id);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.FailureResult($"Failed to create tag: {ex.Message}");
            }
        }

        // Update tag
        public async Task<ServiceResult> UpdateTagAsync(string userId, int tagId, TagInputModel model)
        {
            try
            {
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

                if (tag == null)
                    return ServiceResult.FailureResult("Tag not found");

                // Check if another tag with same name exists
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.Id != tagId && t.Name.ToLower() == model.Name.ToLower());

                if (existingTag != null)
                    return ServiceResult.FailureResult("A tag with this name already exists");

                tag.Name = model.Name;
                await _context.SaveChangesAsync();

                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to update tag: {ex.Message}");
            }
        }

        // Delete tag
        public async Task<ServiceResult> DeleteTagAsync(string userId, int tagId)
        {
            try
            {
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

                if (tag == null)
                    return ServiceResult.FailureResult("Tag not found");

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();

                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Failed to delete tag: {ex.Message}");
            }
        }

        // Get tag usage count (how many entries use this tag)
        public async Task<ServiceResult<int>> GetTagUsageCountAsync(string userId, int tagId)
        {
            try
            {
                var tag = await _context.Tags
                    .Include(t => t.JournalEntries)
                    .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

                if (tag == null)
                    return ServiceResult<int>.FailureResult("Tag not found");

                return ServiceResult<int>.SuccessResult(tag.JournalEntries.Count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.FailureResult($"Failed to get tag usage count: {ex.Message}");
            }
        }
    }
}

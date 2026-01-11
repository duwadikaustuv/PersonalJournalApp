using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalJournalApp.Models.Display
{
    public class JournalEntryDisplayModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        // Updated mood fields
        public string PrimaryMood { get; set; } = "calm";
        public string? SecondaryMood1 { get; set; }
        public string? SecondaryMood2 { get; set; }

        public string Mood => PrimaryMood;

        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CategoryName { get; set; }
        public List<string> TagNames { get; set; } = new();

        // Computed properties for UI
        public string FormattedCreatedDate => CreatedDate.ToLocalTime().ToString("MMM dd, yyyy");
        public string FormattedTime => CreatedDate.ToLocalTime().ToString("h:mm tt");
        public string PreviewContent => Content.Length > 150 ? Content.Substring(0, 150) + "..." : Content;
        public int WordCount => Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        public bool IsModified => ModifiedDate.HasValue;

        // Helper to get all moods as a list
        public List<string> AllMoods
        {
            get
            {
                var moods = new List<string> { PrimaryMood };
                if (!string.IsNullOrEmpty(SecondaryMood1)) moods.Add(SecondaryMood1);
                if (!string.IsNullOrEmpty(SecondaryMood2)) moods.Add(SecondaryMood2);
                return moods;
            }
        }
    }
}
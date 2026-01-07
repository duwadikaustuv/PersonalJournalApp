using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalJournalApp.Entities
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Mood { get; set; } = "calm"; // happy, sad, calm, anxious, excited, tired
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Foreign keys
        public string UserId { get; set; } = string.Empty;
        public int? CategoryId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Category? Category { get; set; }
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}

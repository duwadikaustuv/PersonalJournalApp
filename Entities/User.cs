using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace PersonalJournalApp.Entities
{
    public class User : IdentityUser
    {
        // Navigation properties
        public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public UserSettings? UserSettings { get; set; }
        
        // Additional user properties
        public string? PIN { get; set; } // For app lock feature
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

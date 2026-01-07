using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalJournalApp.Entities
{
    public class UserSettings
    {
        public int Id { get; set; }
        public string Theme { get; set; } = "Light"; // Light, Dark, Auto
        public string FontSize { get; set; } = "Medium"; // Small, Medium, Large
        public bool BiometricUnlock { get; set; } = false;
        public bool AutoBackup { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        // Foreign key (1:1 relationship)
        public string UserId { get; set; } = string.Empty;

        // Navigation property
        public User User { get; set; } = null!;
    }
}

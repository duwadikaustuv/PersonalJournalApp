using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace PersonalJournalApp.Models.Input
{
    public class UserSettingsInputModel
    {
        [Required]
        public string Theme { get; set; } = "Light";

        [Required]
        public string FontSize { get; set; } = "Medium";

        public bool BiometricUnlock { get; set; }

        public bool AutoBackup { get; set; }

        [StringLength(6, MinimumLength = 4, ErrorMessage = "PIN must be 4-6 digits")]
        [RegularExpression(@"^\d+$", ErrorMessage = "PIN must contain only digits")]
        public string? PIN { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace PersonalJournalApp.Models.Input
{
    public class JournalEntryInputModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(10000, ErrorMessage = "Content cannot exceed 10,000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mood is required")]
        public string Mood { get; set; } = "calm";

        public int? CategoryId { get; set; }

        public List<int> SelectedTagIds { get; set; } = new();
    }
}

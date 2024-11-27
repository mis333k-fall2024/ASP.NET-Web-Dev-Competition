using System;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    public class Review
    {
        // Primary key
        public Int32 ReviewID { get; set; }

        // Foreign keys and navigation properties
        public String CustomerID { get; set; }
        public AppUser Customer { get; set; }

        public Int32 PropertyID { get; set; }
        public Property Property { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public Int32 Rating { get; set; }

        [StringLength(280, ErrorMessage = "Review text cannot exceed 280 characters")]
        [Display(Name = "Review")]
        public String ReviewText { get; set; }

        // Host Comments range??
        [StringLength(500, ErrorMessage = "Host Comments cannot exceed 500 characters.")]
        public string? HostComments { get; set; }

        // Dispute Status with required validation
        [Required]
        public bool DisputeStatus { get; set; }
    }
}
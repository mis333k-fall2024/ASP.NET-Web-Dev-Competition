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

        [Display(Name = "Host Comments")]
        public String HostComments { get; set; }

        [Display(Name = "Dispute Description")]
        public String DisputeDescription { get; set; }

        public Boolean DisputeStatus { get; set; }
    }
}
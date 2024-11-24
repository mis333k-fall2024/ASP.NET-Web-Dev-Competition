using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    /// <summary>
    /// Represents a property listing in the BevoBnB system.
    /// Properties are created by Hosts and can be booked by Customers.
    /// </summary>
    public class Property
    {
        // Constant for generating property numbers
        private const Int32 FIRST_PROPERTY_NUMBER = 3001;

        // Primary key for the Property entity
        public Int32 PropertyID { get; set; }

        // Auto-generated number for property identification
        [Display(Name = "Property Number")]
        public Int32 PropertyNumber { get; set; }

        // Property name for searching
        [Required(ErrorMessage = "Property name is required")]
        [Display(Name = "Property Name")]
        public String PropertyName { get; set; }

        // Location details
        [Required(ErrorMessage = "Street address is required")]
        [Display(Name = "Street Address")]
        public String Street { get; set; }

        [Required(ErrorMessage = "City is required")]
        public String City { get; set; }

        [Required(ErrorMessage = "State is required")]
        public String State { get; set; }

        [Required(ErrorMessage = "ZIP code is required")]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code format")]
        public String Zip { get; set; }

        // Property characteristics
        [Required(ErrorMessage = "Number of bedrooms is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of bedrooms must be greater than 0")]
        public Int32 Bedrooms { get; set; }

        [Required(ErrorMessage = "Number of bathrooms is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of bathrooms must be greater than 0")]
        public Int32 Bathrooms { get; set; }

        [Required(ErrorMessage = "Guest limit is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Guest limit must be greater than 0")]
        [Display(Name = "Maximum Guests Allowed")]
        public Int32 GuestsAllowed { get; set; }

        // Property amenities
        [Display(Name = "Pets Allowed")]
        public Boolean PetsAllowed { get; set; }

        [Display(Name = "Free Parking")]
        public Boolean FreeParking { get; set; }

        // Pricing information
        [Required(ErrorMessage = "Weekday price is required")]
        [Display(Name = "Weekday Price")]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal WeekdayPrice { get; set; }

        [Required(ErrorMessage = "Weekend price is required")]
        [Display(Name = "Weekend Price")]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal WeekendPrice { get; set; }

        [Required(ErrorMessage = "Cleaning fee is required")]
        [Display(Name = "Cleaning Fee")]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal CleaningFee { get; set; }

        // Optional discount settings
        [Display(Name = "Discount Rate (%)")]
        [DisplayFormat(DataFormatString = "{0:P0}")]
        public Decimal? DiscountRate { get; set; }

        [Display(Name = "Minimum Nights for Discount")]
        public Int32? MinNightsForDiscount { get; set; }

        // Property availability and status
        [Display(Name = "Unavailable Dates")]
        public String UnavailableDates { get; set; }

        [Display(Name = "Property Status")]
        public Boolean PropertyStatus { get; set; }

        // Admin approval tracking
        [Display(Name = "Approval Status")]
        public Boolean AdminApproved { get; set; }

        // Navigational Properties
        // These establish relationships between the Property and other entities:

        // Many-to-One: Each property belongs to one category
        public Category Category { get; set; }

        // Many-to-One: Each property is owned by one host
        public AppUser Host { get; set; }

        // One-to-Many: One property can have many reviews
        public List<Review> Reviews { get; set; }

        // One-to-Many: One property can have many reservations
        public List<Reservation> Reservations { get; set; }

        /// <summary>
        /// Constructor to initialize collections and set default values
        /// </summary>
        public Property()
        {
            // Initialize lists to prevent null reference exceptions
            Reviews = new List<Review>();
            Reservations = new List<Reservation>();

            // Set default property status to active
            PropertyStatus = true;

            // Properties start as unapproved by default
            AdminApproved = false;
        }
    }
}
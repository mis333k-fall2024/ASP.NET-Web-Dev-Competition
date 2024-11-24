using System;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    public class Reservation
    {
        private const decimal TAX_RATE = 0.07m;

        public Int32 ReservationID { get; set; }

        // Foreign keys and navigation properties
        public Int32 PropertyID { get; set; }
        public Property Property { get; set; }

        public String CustomerID { get; set; }
        public AppUser Customer { get; set; }

        [Required]
        [Display(Name = "Check-in Date")]
        public DateTime CheckIn { get; set; }

        [Required]
        [Display(Name = "Check-out Date")]
        public DateTime CheckOut { get; set; }

        [Required]
        [Display(Name = "Number of Guests")]
        [Range(1, int.MaxValue)]
        public Int32 NumOfGuests { get; set; }

        [Required]
        [Display(Name = "Weekday Price")]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal WeekdayPrice { get; set; }

        [Required]
        [Display(Name = "Weekend Price")]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal WeekendPrice { get; set; }

        [Required]
        [Display(Name = "Cleaning Fee")]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal CleaningFee { get; set; }

        [Display(Name = "Discount Rate")]
        [DisplayFormat(DataFormatString = "{0:P0}")]
        public Decimal DiscountRate { get; set; }

        [Required]
        [Display(Name = "Confirmation Number")]
        public Int32 ConfirmationNumber { get; set; }

        [Required]
        [Display(Name = "Status")]
        public Boolean ReservationStatus { get; set; }

        // Computed Properties
        [Display(Name = "Tax Amount")]
        public Decimal Tax => (SubTotal) * TAX_RATE;

        [Display(Name = "Subtotal")]
        public Decimal SubTotal
        {
            get
            {
                // Calculate base cost
                Decimal baseTotal = CalculateBaseTotal();

                // Apply discount if applicable
                if (DiscountRate > 0)
                {
                    baseTotal = baseTotal * (1 - DiscountRate);
                }

                // Add cleaning fee
                return baseTotal + CleaningFee;
            }
        }

        [Display(Name = "Total")]
        public Decimal Total => SubTotal + Tax;

        private Decimal CalculateBaseTotal()
        {
            int totalDays = (CheckOut - CheckIn).Days;
            int weekendDays = CountWeekendDays();
            int weekDays = totalDays - weekendDays;

            return (weekDays * WeekdayPrice) + (weekendDays * WeekendPrice);
        }

        private int CountWeekendDays()
        {
            int weekendDays = 0;
            for (DateTime date = CheckIn; date < CheckOut; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
                {
                    weekendDays++;
                }
            }
            return weekendDays;
        }
    }
}
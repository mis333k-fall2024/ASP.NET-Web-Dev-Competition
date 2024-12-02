using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FinalProject.Models
{
    public class PropertyReportDetail
    {
        public int PropertyID { get; set; }
        public string PropertyAddress { get; set; }
        public decimal StayRevenue { get; set; }
        public decimal CleaningFees { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CompletedReservations { get; set; }
    }

    public class ReportViewModel
    {
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // Common Properties
        public decimal TotalRevenue { get; set; }
        public int TotalCompletedReservations { get; set; }

        public bool IsAllTime { get; set; }


        // Host-specific Properties
        public decimal TotalStayRevenue { get; set; }
        public decimal TotalCleaningFees { get; set; }
        public List<PropertyReportDetail> PropertyDetails { get; set; }

        // Admin-specific Properties
        public decimal TotalCommission { get; set; }
        public decimal AverageCommissionPerReservation { get; set; }
        public int TotalProperties { get; set; }

        public ReportViewModel()
        {
            PropertyDetails = new List<PropertyReportDetail>();
        }

    }
}
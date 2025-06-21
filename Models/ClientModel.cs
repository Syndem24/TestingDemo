using System.ComponentModel.DataAnnotations;

namespace TestingDemo.Models
{
    public class ClientModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Client Name is required")]
        [Display(Name = "Client Name")]
        public string ClientName { get; set; }

        [Required(ErrorMessage = "Contact Number is required")]
        [Display(Name = "Contact Number")]
        [Phone]
        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Urgency Level is required")]
        [Display(Name = "Urgency Level")]
        public string UrgencyLevel { get; set; } = "Normal";

        [Required(ErrorMessage = "Permit Type is required")]
        [Display(Name = "Permit Type")]
        public string PermitType { get; set; }

        [Display(Name = "Tax ID")]
        public string? TaxId { get; set; }

        [Display(Name = "Financial Year")]
        public string? FinancialYear { get; set; }

        [Display(Name = "Financial Notes")]
        public string? FinancialNotes { get; set; }

        [Display(Name = "Document References")]
        public string? DocumentReferences { get; set; }

        [Display(Name = "Planning Return Note")]
        public string? PlanningReturnNote { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
    }
}
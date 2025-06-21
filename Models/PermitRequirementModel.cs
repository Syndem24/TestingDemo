using System.ComponentModel.DataAnnotations;

namespace TestingDemo.Models
{
    public class PermitRequirementModel
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        [Required(ErrorMessage = "Requirement name is required")]
        [Display(Name = "Requirement Name")]
        public string RequirementName { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Display(Name = "Is Required")]
        public bool IsRequired { get; set; } = true;

        [Display(Name = "Is Completed")]
        public bool IsCompleted { get; set; } = false;

        [Display(Name = "Is Present")]
        public bool IsPresent { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        public ClientModel Client { get; set; }
    }
}
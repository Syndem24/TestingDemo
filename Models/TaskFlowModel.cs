using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace TestingDemo.Models
{
    public class TaskFlowModel
    {
        [Key]
        public int Id { get; set; }

        public string ClientName { get; set; }
        public string Permit { get; set; }

        // Store all possible requirements as a comma-separated string
        public string Requirements { get; set; } = "";

        // Store only the completed requirements (checked ones)
        public string DoneRequirements { get; set; } = "";

        public int Progress { get; set; }
        public string Priority { get; set; }
        public DateTime DateIssued { get; set; }
        public bool IsDone { get; set; }
        public bool IsArchived { get; set; }
        public bool IsMovedToHistory { get; set; } // ✅ Added to handle history movement
        public DateTime? DateCompleted { get; set; }

        // CompletedAt is for documentation TimeStamp
        public DateTime? CompletedAt { get; set; }

        // ✅ Convert stored comma-separated values to a List<string>
        [NotMapped]
        public List<string> RequirementList
        {
            get => string.IsNullOrEmpty(Requirements)
                ? new List<string>()
                : Requirements.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            set => Requirements = value != null
                ? string.Join(",", value)
                : "";
        }

        [NotMapped]
        public List<string> CheckedRequirements
        {
            get => string.IsNullOrEmpty(DoneRequirements)
                ? new List<string>()
                : DoneRequirements.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            set => DoneRequirements = value != null
                ? string.Join(",", value)
                : "";
        }
    }
}


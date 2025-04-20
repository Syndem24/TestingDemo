using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class AddTaskViewModel
{
    [Required]
    public string ClientName { get; set; }

    [Required]
    public string Permit { get; set; }

    public List<string> Requirements { get; set; } = new();

    [Required]
    public string Priority { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MqSocial.Careers.Dto;

public class CreateCareerDto
{
    [Required]
    public string Name { get; set; }
}

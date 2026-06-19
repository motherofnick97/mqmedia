using System.ComponentModel.DataAnnotations;

namespace MqSocial.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}
using System.ComponentModel.DataAnnotations;

public class LoginModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}

public class RegisterModel
{
    [Required]
    public string Firstname { get; set; }

    [Required]
    public string Lastname { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Address { get; set; }

    [Required]
    public string Phonenumber { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; }
}

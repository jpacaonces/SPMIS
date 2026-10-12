using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Spmis.Web.Models.Admin;

public class UserPasswordResetViewModel
{
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Enter the new temporary password.")]
    [DataType(DataType.Password)]
    [Display(Name = "New temporary password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repeat the password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Repeat password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public ReasonInput Reason { get; set; } = new();

    // Shown on the page; filled by the server, never posted.
    [BindNever, ValidateNever]
    public string FullName { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public string Email { get; set; } = string.Empty;
}
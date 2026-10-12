using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Spmis.Web.Models.Admin;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Enter the email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the first name.")]
    [StringLength(60)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Middle name")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Enter the last name.")]
    [StringLength(60)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a role.")]
    public string Role { get; set; } = string.Empty;

    // Used only for some roles. The service checks which ones apply.
    [StringLength(20)]
    [Display(Name = "Student number")]
    public string? StudentNo { get; set; }

    [StringLength(20)]
    [Display(Name = "Employee number")]
    public string? EmployeeNo { get; set; }

    [Display(Name = "Program")]
    public int? ProgramId { get; set; }

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Enter a temporary password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repeat the password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Repeat password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Dropdown contents. Filled by the server, never read from the form.
    [BindNever, ValidateNever]
    public List<LookupItem> Programs { get; set; } = new();

    [BindNever, ValidateNever]
    public List<LookupItem> Departments { get; set; } = new();
}
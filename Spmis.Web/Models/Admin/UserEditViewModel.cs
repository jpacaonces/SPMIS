using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Spmis.Web.Models.Admin;

public class UserEditViewModel
{
    public Guid UserId { get; set; }

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

    // The version of the row this form was built from (see UpdateAsync).
    public string ConcurrencyStamp { get; set; } = string.Empty;

    public ReasonInput Reason { get; set; } = new();

    // Everything below is filled by the SERVER on every render, never posted.
    [BindNever, ValidateNever]
    public List<string> Roles { get; set; } = new();

    [BindNever, ValidateNever]
    public bool IsStudent { get; set; }

    [BindNever, ValidateNever]
    public bool IsFaculty { get; set; }

    [BindNever, ValidateNever]
    public bool IsProgramHead { get; set; }

    [BindNever, ValidateNever]
    public List<LookupItem> Programs { get; set; } = new();

    [BindNever, ValidateNever]
    public List<LookupItem> Departments { get; set; } = new();
}
using System.ComponentModel.DataAnnotations;

namespace Spmis.Web.Models;

// What the dialog posts back. Any form that uses the dialog must have a
// property named "Reason" of this type on its view model.
public class ReasonInput
{
    public int? ReasonId { get; set; }

    [StringLength(400, ErrorMessage = "Details can be at most 400 characters.")]
    public string? Details { get; set; }
}
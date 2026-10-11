using Spmis.Web.Data.Entities;

namespace Spmis.Web.Models;

public class ReasonDialogViewModel
{
    public string FormId { get; set; } = string.Empty;
    public IReadOnlyList<ChangeReason> Options { get; set; } = new List<ChangeReason>();
    public ReasonInput Selected { get; set; } = new();
    public string? ReasonError { get; set; }
    public string? DetailsError { get; set; }
}
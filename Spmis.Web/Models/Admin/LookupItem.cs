namespace Spmis.Web.Models.Admin;

// One entry of a dropdown: a database id and the text to show.
public class LookupItem
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
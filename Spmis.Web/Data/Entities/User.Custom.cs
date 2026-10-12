using System.ComponentModel.DataAnnotations.Schema;

namespace Spmis.Web.Data.Entities;

// Hand-written half of the scaffolded User entity.
// Kept in its own file so re-scaffolding the database cannot erase it.
public partial class User
{
    // True = the person must choose a new password before using the system.
    [Column("must_change_password")]
    public bool MustChangePassword { get; set; }
}
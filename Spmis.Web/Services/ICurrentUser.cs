namespace Spmis.Web.Services;

// Answers "who is making this request?" without every class
// having to dig through HttpContext itself.
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Role { get; }
}
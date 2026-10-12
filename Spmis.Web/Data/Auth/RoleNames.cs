namespace Spmis.Web.Data.Auth;

// Must match the "name" column of the roles table exactly.
public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string Faculty = "Faculty";
    public const string Student = "Student";
    public const string ProgramHead = "ProgramHead";

    public static readonly string[] All =
        { Administrator, ProgramHead, Faculty, Student };
}
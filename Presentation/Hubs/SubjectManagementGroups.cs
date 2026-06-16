namespace Presentation.Hubs;

public static class SubjectManagementGroups
{
    public const string Index = "subject-management:index";

    public static string ForMembers(int subjectId)
    {
        return $"subject-management:members:{subjectId}";
    }
}

namespace ServiceFlow.Api.Security;

public static class ServiceFlowRoles
{
    public const string Admin = "Admin";
    public const string Agent = "Agent";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Agent, Viewer];
}

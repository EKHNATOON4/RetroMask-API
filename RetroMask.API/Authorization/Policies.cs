using Microsoft.AspNetCore.Authorization;

namespace RetroMask.API.Authorization;

public static class Policies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireSuperAdmin = "RequireSuperAdmin";

    public static void AddRetroMaskPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequireAdmin, policy =>
            policy.RequireRole("Admin", "SuperAdmin"));

        options.AddPolicy(RequireSuperAdmin, policy =>
            policy.RequireRole("SuperAdmin"));
    }
}

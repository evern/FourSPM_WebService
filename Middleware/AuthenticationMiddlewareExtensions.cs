namespace FourSPM_WebService.Middleware;

/// <summary>
/// Extension methods for registering authentication middleware
/// </summary>
public static class AuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Adds the authentication middleware to the application pipeline
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}

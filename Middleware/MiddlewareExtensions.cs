namespace FourSPM_WebService.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseJwtValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
    
    public static IApplicationBuilder UseTokenTypeDetection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenTypeMiddleware>();
    }
}

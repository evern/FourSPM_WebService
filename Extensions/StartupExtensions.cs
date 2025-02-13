using FourSPM_WebService.Interfaces;
namespace FourSPM_WebService.Extensions;

internal static class StartupExtensions
{
    internal static void ConfigureInstallers(this WebApplicationBuilder builder)
    {
        var installers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(type =>
                typeof(IStartupInstaller).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<IStartupInstaller>();

        foreach (var startupInstaller in installers)
        {
            startupInstaller.Configure(builder);
        }
    }
}
using Microsoft.Extensions.Hosting;
using Uno.UI.Hosting;

namespace UnoApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}

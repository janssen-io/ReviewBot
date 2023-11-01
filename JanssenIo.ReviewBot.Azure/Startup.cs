using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(JanssenIo.ReviewBot.Azure.Startup))]
namespace JanssenIo.ReviewBot.Azure;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
    }
}
    }

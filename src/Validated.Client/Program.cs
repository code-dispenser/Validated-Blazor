using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Validated.Core.Factories;

namespace Validated.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            /*
                * This is for the multi-tenant/configuration based validators 
            */
            builder.Services.AddSingleton<IValidatorFactoryProvider, ValidatorFactoryProvider>();

            await builder.Build().RunAsync();
        }
    }
}

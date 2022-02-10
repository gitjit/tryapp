using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;

[assembly: FunctionsStartup(typeof(TryFunctionApp.Startup))]
namespace TryFunctionApp
{
    public class Startup : FunctionsStartup
    {
        string VERSION = "1.0.0";

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var userAssignedClientId = "7d53794d-edb9-4a1f-b8f6-41f91c2f02ba";

            Console.WriteLine($"!!!!!!!!!!!!--StartUP Version = {VERSION} !!!!!!!!!!!!-----------------------");
            builder.Services.AddLogging();
            builder.Services.AddAzureClients(clientBuilder =>
            {

                clientBuilder.AddBlobServiceClient(new Uri("https://try01storage.blob.core.windows.net"))
                                .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }));
                clientBuilder.AddQueueServiceClient(new Uri("https://try01storage.queue.core.windows.net"))
                              .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }))
                             .ConfigureOptions(c => c.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64);
            });
            try
            {
                var cosmosClient = new CosmosClient("https://try01cosmos.documents.azure.com:443/", new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }));
                if (cosmosClient == null)
                {
                    Console.WriteLine("#### Cosmos Client Failed to Create");
                }
                builder.Services.AddSingleton(cosmosClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception !!!!");
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("******** StartUP Ended ****************");
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            base.ConfigureAppConfiguration(builder);

            //FunctionsHostBuilderContext context = builder.GetContext();



            //builder.ConfigurationBuilder
            //    .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), true, false)
            //    .AddEnvironmentVariables()
            //    //.AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            //    .Build();

            //            builder.Configuration.AddJsonFile("appsettings.json");
            //#if DEBUG
            //            builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());
            //            ConfigureMSIEnvironment(builder);
            //#endif
            //            builder.Configuration.AddEnvironmentVariables();
            //            builder.Configuration.AddAzureAppConfiguration(opions =>
            //            {
            //                opions.Connect(new Uri("https://hpsmart-config.azconfig.io"), new DefaultAzureCredential());
            //            });

            //            builder.Configuration.AddAzureKeyVault(
            //                   new Uri($"https://smartexkeyvault.vault.azure.net/"),
            //                   new DefaultAzureCredential());
        }


    }
}

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
using Microsoft.Extensions.Configuration;
using System.IO;

[assembly: FunctionsStartup(typeof(TryFunctionApp.Startup))]
namespace TryFunctionApp
{
    public class Startup : FunctionsStartup
    {
        string VERSION = "1.0.0";

        public override void Configure(IFunctionsHostBuilder builder)
        {

            var config = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var blobEndpoint = config["blob_endpoint"]; 
            var queueEndpoint = config["queue_endpoint"];
            var cosmosEndpoint = config["cosmos_ep"];
            var logsContainer = config["logs_container"]; 
            var sessionsQ = config["session_q"]; 
            var crashesQ = config["crashes_q"]; 
            var cosmosDb = config["cosmos_db"]; 
            var cosmosContainer = config["cosmso_container"];
            var userAssignedClientId = config["uaid_client_id"]; 

            Console.WriteLine($"!!!!!!!!!!!!--StartUP Version = {VERSION} !!!!!!!!!!!!-----------------------");
            builder.Services.AddLogging();
            builder.Services.AddAzureClients(clientBuilder =>
            {

                clientBuilder.AddBlobServiceClient(new Uri(blobEndpoint))
                                .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }));
                clientBuilder.AddQueueServiceClient(new Uri(queueEndpoint))
                              .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }))
                             .ConfigureOptions(c => c.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64);
            });
            try
            {
                var cosmosClient = new CosmosClient(cosmosEndpoint, new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }));
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

        }

            //builder.Configuration.AddJsonFile("appsettings.json");
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
//        }


    }
}

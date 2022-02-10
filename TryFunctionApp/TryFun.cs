using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TryFunctionApp
{
    public class TryFun
    {
        private readonly BlobServiceClient blbClient;
        private readonly QueueServiceClient qClient;
        private readonly CosmosClient cosmosClient;

        public TryFun(BlobServiceClient blbClient, QueueServiceClient qClient, CosmosClient cosmosClient)
        {
            this.blbClient = blbClient;
            this.qClient = qClient;
            this.cosmosClient = cosmosClient;
        }

        [FunctionName("TryFun")]
        public async Task Run([TimerTrigger("*/1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var containerClient = blbClient.GetBlobContainerClient("logs");
            BlobClient blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString());

            using (var ms = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(ms);
                writer.Write(DateTime.Now.ToString());
                writer.Flush();
                ms.Position = 0;
                await blobClient.UploadAsync(ms);
            }

            int count = 0;

            await foreach (BlobItem item in containerClient.GetBlobsAsync())
            {
                count++;
                Console.WriteLine(item.Name);
            }

            var sq = qClient.GetQueueClient("sessions");
            await sq.SendMessageAsync("a new one " + DateTime.Now.ToString());

            var container = cosmosClient.GetContainer("tryDb", "tries");
            var payload = new CosmosModel
            {
                Id = Guid.NewGuid().ToString(),
                Pk = "123",
                Name = "Jithesh",
                Time = DateTime.Now.ToShortDateString()
            };
            await container.CreateItemAsync(payload);
        }
    }

    class CosmosModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "pk")]
        public string Pk { get; set; }
        public string Name { get; set; }
        public string Time { get; set; }
    }
}

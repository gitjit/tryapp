using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;

string Version = "1.0.1";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.AddJsonFile("appsettings.json");

var blobEndpoint =  builder.Configuration.GetValue<string>("Az:blob_endpoint");
var queueEndpoint = builder.Configuration.GetValue<string>("Az:queue_endpoint");
var cosmosEndpoint = builder.Configuration.GetValue<string>("Az:cosmos_ep");
var logsContainer = builder.Configuration.GetValue<string>("Az:logs_container");
var sessionsQ = builder.Configuration.GetValue<string>("Az:session_q");
var crashesQ = builder.Configuration.GetValue<string>("Az:crashes_q");
var cosmosDb = builder.Configuration.GetValue<string>("Az:cosmos_db");
var cosmosContainer = builder.Configuration.GetValue<string>("Az:cosmso_container");


var userAssignedClientId = builder.Configuration.GetValue<string>("Az:uaid_client_id");


builder.Services.AddAzureClients(clientBuilder =>
{

    clientBuilder.AddBlobServiceClient(new Uri(blobEndpoint))
                    .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }));
    clientBuilder.AddQueueServiceClient(new Uri(queueEndpoint))
                  .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }))
                 .ConfigureOptions(c => c.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64);
});
var cosmosClient = new CosmosClient(cosmosEndpoint, new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }));
if (cosmosClient == null)
{
    Console.WriteLine("#### Cosmos Client Failed to Create");
}
builder.Services.AddSingleton(cosmosClient);


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
       new WeatherForecast
       (
           DateTime.Now.AddDays(index),
           Random.Shared.Next(-20, 55),
           summaries[Random.Shared.Next(summaries.Length)]
       ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/blob", async (BlobServiceClient blb, QueueServiceClient q) =>
{
    var containerClient = blb.GetBlobContainerClient(logsContainer);
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

    var sq = q.GetQueueClient(sessionsQ);
    await sq.SendMessageAsync("a new one " + DateTime.Now.ToString());

    return Results.Ok(count);
})
.WithName("GetBlob");

app.MapGet("/version", () =>
{
    return Results.Ok(Version);

}).WithName("Version");

app.MapGet("/cosmos", async (CosmosClient cosmosClient) =>
{
    try
    {
        var container = cosmosClient.GetContainer("tryDb", "tries");
        var payload = new CosmosModel
        {
            Id = Guid.NewGuid().ToString(),
            Pk = "123",
            Name = "Jithesh",
            Time = DateTime.Now.ToShortDateString()
        };
        await container.CreateItemAsync(payload);
        return Results.Ok(payload);
    }
    catch (Exception ex)
    {
        return Results.Ok(ex.Message);
    }
}).WithName("Cosmos");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

class CosmosModel
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "pk")]
    public string Pk { get;set; }
    public string Name { get; set; }
    public string Time { get; set; }
}
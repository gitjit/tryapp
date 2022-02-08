using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddAzureClients(clientBuilder =>
{
    var userAssignedClientId = "7d53794d-edb9-4a1f-b8f6-41f91c2f02ba";

    clientBuilder.AddBlobServiceClient(new Uri("https://try01storage.blob.core.windows.net"))
                    .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }));
    clientBuilder.AddQueueServiceClient(new Uri("https://try01storage.queue.core.windows.net"))
                  .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId }))
                 .ConfigureOptions(c => c.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64);
});

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
    var containerClient = blb.GetBlobContainerClient("logs");
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

    var sq = q.GetQueueClient("sessions");
    await sq.SendMessageAsync("a new one " + DateTime.Now.ToString());

    return Results.Ok(count);
})
.WithName("GetBlob");


app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
using MassTransit;
using MongoDB.Driver;
using Stock.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configurator =>
{

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration["RabbitMQ"]);

    });
});

builder.Services.AddSingleton<MongoDbService>();

var app = builder.Build();

using var scope = builder.Services.BuildServiceProvider().CreateScope();
var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();

// Burada, projeksiyon tipini a��k�a belirterek derleyiciye niyetimizi a��kl�yoruz.
var stocksCollection = mongoDbService.GetCollection<Stock.API.Models.Stock>();

// FindAsync metodunu t�m belgeleri e�le�tiren bir filtre ile kullan�yoruz ve d�n�� tipini a��k�a belirtiyoruz.
if (!await (await stocksCollection.FindAsync<Stock.API.Models.Stock>(Builders<Stock.API.Models.Stock>.Filter.Empty)).AnyAsync())
{
    var stockInitializationList = new List<Stock.API.Models.Stock>
    {
        new() { ProductId = 1, Count = 200 },
        new() { ProductId = 2, Count = 300 },
        new() { ProductId = 3, Count = 50 },
        new() { ProductId = 4, Count = 10 },
        new() { ProductId = 5, Count = 60 }
    };

    // Birden fazla d�k�man� daha verimli bir �ekilde eklemek i�in.
    stocksCollection.InsertMany(stockInitializationList);
}

app.Run();

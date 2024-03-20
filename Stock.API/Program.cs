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

// Burada, projeksiyon tipini açýkça belirterek derleyiciye niyetimizi açýklýyoruz.
var stocksCollection = mongoDbService.GetCollection<Stock.API.Models.Stock>();

// FindAsync metodunu tüm belgeleri eþleþtiren bir filtre ile kullanýyoruz ve dönüþ tipini açýkça belirtiyoruz.
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

    // Birden fazla dökümaný daha verimli bir þekilde eklemek için.
    stocksCollection.InsertMany(stockInitializationList);
}

app.Run();

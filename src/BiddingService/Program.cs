using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MongoDB.Entities;
using MongoDB.Driver;
using BiddingService;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();

builder.Services.AddMassTransit(x => 
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("bids", false));

    x.UsingRabbitMq((context, cfg) => 
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", host =>
        {
            host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
            host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
        });
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => {
        opts.Authority = builder.Configuration["IdentityServiceUrl"];
        opts.RequireHttpsMetadata = false;
        opts.TokenValidationParameters.ValidateAudience = false;
        opts.TokenValidationParameters.NameClaimType = "username";
    });


var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await DB.InitAsync("BidDb", MongoClientSettings
            // .FromConnectionString(builder.Configuration.GetConnectionString("BidDbConnection")));
            .FromConnectionString(builder.Configuration["ConnectionStrings:BidDbConnection"]));
app.Run();

using Confluent.Kafka;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stingray.Application.Commands;
using Stingray.Application.DTOs;
using Stingray.Application.Interfaces;
using Stingray.Application.Queries;
using Stingray.Domain.Interfaces;
using Stingray.Services.UserService.Infrastructure;
using Stingray.Storage.InMemory;
using Stingray.Storage.InMemory.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateUserCommand).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateUserCommand).Assembly);

// Add DbContext
builder.Services.AddDbContext<StingrayDbContext>(options =>
    options.UseInMemoryDatabase("UserServiceDb"));

// Add Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Configure Kafka Producer
var producerConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092"
};
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
    new ProducerBuilder<string, string>(producerConfig).Build());

// Add Event Publisher
builder.Services.AddScoped<IEventPublisher, KafkaEventPublisher>();


var app = builder.Build();

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

// Minimal API endpoints
app.MapPost("/users", async (CreateUserRequest request, IMediator mediator, IValidator<CreateUserCommand> validator) =>
    {
        var command = new CreateUserCommand
        {
            Email = request.Email,
            Name = request.Name
        };

        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid) return Results.ValidationProblem(validationResult.ToDictionary());

        var result = await mediator.Send(command);
        return Results.Created($"/users/{result.Id}", result);
    })
    .WithName("CreateUser")
    .WithOpenApi();

app.MapGet("/users/{id:guid}", async (Guid id, IMediator mediator) =>
    {
        var query = new GetUserByIdQuery { UserId = id };
        var result = await mediator.Send(query);
        return result != null ? Results.Ok(result) : Results.NotFound();
    })
    .WithName("GetUser")
    .WithOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "UserService" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
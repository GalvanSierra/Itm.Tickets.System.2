using Itm.Event.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/events/{id}", (int id) =>
{
    // Lógica para obtener la lista de eventos
});

app.MapPost("/api/events/reserve", () =>
{
    // Lógica para reservar un evento
});

app.MapPost("/api/events/release", () =>
{
    // Lógica para liberar una reserva de evento
});

app.Run();

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

var events = new List<Event>
{
    new Event(1, "Concierto ITM", 50000, 100),
    new Event(2, "Obra de Teatro ITM", 30000, 50),
    new Event(3, "Conferencia ITM", 20000, 200),
    new Event(4, "Feria de Emprendimiento ITM", 10000, 150)
};


app.MapGet("/api/events/{id}", (int id) =>
{
    var evento = events.FirstOrDefault(e => e.Id == id);
    return evento is not null ? Results.Ok(evento) : Results.NotFound();
});

app.MapPost("/api/events/reserve", (EventDto request) => 
{
    var evento = events.FirstOrDefault(e => e.Id == request.EventId);
    if (evento is null) return Results.NotFound("Evento no encontrado");

    if (evento.AvailableSeats < request.Quantity)
        return Results.BadRequest("No hay suficientes sillas disponibles");

    // Lógica para reservar las sillas (actualizar la cantidad disponible en eventos)
    var updatedEvent = evento with { AvailableSeats = evento.AvailableSeats - request.Quantity };

    // Actualizamos la lista de eventos con el nuevo estado
    events.Remove(evento);
    events.Add(updatedEvent);

    return Results.Ok(
        new
        {
            Message = $"Reserva de {request.Quantity} sillas para el evento '{evento.Name}' realizada con éxito",
            CurrentQuantity = updatedEvent.AvailableSeats
        }
        );
});

app.MapPost("/api/events/release", (EventDto request) => {
    var evento = events.FirstOrDefault(e => e.Id == request.EventId);
    if (evento is null) return Results.NotFound("Evento no encontrado");

    // Lógica para agregar las sillas (actualizar la cantidad disponible en eventos)
    var updatedEvent = evento with { AvailableSeats = evento.AvailableSeats + request.Quantity };

    // Actualizamos la lista de eventos con el nuevo estado
    events.Remove(evento);
    events.Add(updatedEvent);

    return Results.Ok(
        new
        {
            Message = $"Se han agregado {request.Quantity} sillas para el evento '{evento.Name}'",
            CurrentQuantity = updatedEvent.AvailableSeats
        }
        );
});

app.Run();

public record Event(int Id, string Name, decimal BasePrice, int AvailableSeats);
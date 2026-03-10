using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FASE 2: HttpClientFactory con Resiliencia
builder.Services.AddHttpClient("EventClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5295"); 
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("DiscountClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5088");
})
.AddStandardResilienceHandler();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/bookings", async (BookingRequest request, IHttpClientFactory factory) =>
{
    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    // PASO 1: Validación paralela
    var eventTask = eventClient.GetAsync($"/api/events/{request.EventId}");
    var discountTask = discountClient.GetAsync($"/api/discounts/{request.DiscountCode}");

    await Task.WhenAll(eventTask, discountTask);

    var eventResponse = await eventTask;
    var discountResponse = await discountTask;

    if (!eventResponse.IsSuccessStatusCode)
        return Results.NotFound("El evento no existe.");

    var eventDto = await eventResponse.Content.ReadFromJsonAsync<EventDto>();

    // Descuento opcional: 404 = sin descuento, cualquier otro error = fallo real
    DiscountDto? discountDto = null;
    if (discountResponse.StatusCode == HttpStatusCode.OK)
        discountDto = await discountResponse.Content.ReadFromJsonAsync<DiscountDto>();
    else if (discountResponse.StatusCode != HttpStatusCode.NotFound)
        return Results.Problem("Error consultando el servicio de descuentos.");

    // PASO 2: Matemáticas
    var subtotal = eventDto!.BasePrice * request.Tickets;
    var descuento = subtotal * (discountDto?.Percentage ?? 0m);
    var total = subtotal - descuento;

    // PASO 3: Reservar sillas — inicio SAGA
    var reserveResponse = await eventClient.PostAsJsonAsync("/api/events/reserve",
        new { EventId = request.EventId, Quantity = request.Tickets });

    if (!reserveResponse.IsSuccessStatusCode)
        return Results.BadRequest("No hay sillas disponibles o el evento no existe.");

    try
    {
        // PASO 4: Simulación de pago
        bool paymentSuccess = new Random().Next(1, 11) > 5;
        if (!paymentSuccess)
            throw new Exception("Fondos insuficientes en la tarjeta de crédito.");

        return Results.Ok(new
        {
            Status = "Éxito",
            Message = "¡Disfruta el concierto ITM!",
            Factura = new
            {
                Evento = eventDto.Name,
                Tickets = request.Tickets,
                Subtotal = subtotal,
                Descuento = descuento,
                Total = total,
                CodigoUsado = discountDto?.Code ?? "Sin descuento"
            }
        });
    }
    catch (Exception ex)
    {
        // PASO 5: Compensación SAGA — Ctrl+Z
        Console.WriteLine($"[SAGA] Error: {ex.Message}. Liberando sillas...");

        await eventClient.PostAsJsonAsync("/api/events/release",
            new { EventId = request.EventId, Quantity = request.Tickets });

        return Results.Problem(
            "Tu pago fue rechazado. No te preocupes, no te cobramos y tus sillas fueron liberadas.");
    }
})
.WithName("CreateBooking")
.WithOpenApi();

app.Run();

// DTOs — deben coincidir exactamente con los records de los otros servicios
public record BookingRequest(int EventId, int Tickets, string? DiscountCode);
public record EventDto(int Id, string Name, decimal BasePrice, int AvailableSeats);
public record DiscountDto(string Code, decimal Percentage);
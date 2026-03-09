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

// Base de datos simulada
var discounts = new List<Discount>
{
    new Discount("ITM50", 0.5m),
    new Discount("ITM30", 0.3m)
};

app.MapGet("/api/discounts/{code}", (string code) =>
{
    var discount = discounts.FirstOrDefault(d =>
        d.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    return discount is null
        ? Results.NotFound(new { message = $"Discount code '{code}' not found." })
        : Results.Ok(discount);
});

app.Run();

public record Discount(string Code, decimal Percentage);

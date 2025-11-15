using Linqraft;
using Linqraft.ApiSample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet(
    "/sample",
    () =>
    {
        var Orders = new List<Order>();
        return Orders
            .AsQueryable()
            .SelectExpr<Order, OrderDtoMinimal>(s => new
            {
                Id = s.Id,
                CustomerName = s.Customer?.Name,
                CustomerCountry = s.Customer?.Address?.Country?.Name,
                CustomerCity = s.Customer?.Address?.City?.Name,
                Items = s
                    .OrderItems.Select(oi => new
                    {
                        ProductName = oi.Product?.Name,
                        Quantity = oi.Quantity,
                    })
                    .ToList(),
            })
            .ToList();
    }
);

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

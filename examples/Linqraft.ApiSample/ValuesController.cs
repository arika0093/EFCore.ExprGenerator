using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linqraft.ApiSample;

[Route("api/[controller]")]
[ApiController]
public partial class ValuesController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<OrderDto>> Get()
    {
        var Orders = new List<Order>();
        return Orders
            .AsQueryable()
            .SelectExpr<Order, OrderDto>(s => new
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
}

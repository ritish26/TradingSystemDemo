namespace OrderService.DataAcessLayer;

public static class OrderStore
{
    private static readonly List<OrderDto> Orders = new()
    {
        new OrderDto { OrderId = "O1", Symbol = "AAPL", Status = "EXECUTED" },
        new OrderDto { OrderId = "O2", Symbol = "TSLA", Status = "PENDING" },
        new OrderDto { OrderId = "O3", Symbol = "MSFT", Status = "REJECTED" }
    };

    public static List<OrderDto> GetAll() => Orders;

    public static OrderDto? GetById(string id)
        => Orders.FirstOrDefault(x => x.OrderId == id);
}
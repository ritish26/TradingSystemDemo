using AutoMapper;
using OrderService.Models;

namespace OrderService.Command.Mapper;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderRequest, OrderCreatedCommand>();
    }
}
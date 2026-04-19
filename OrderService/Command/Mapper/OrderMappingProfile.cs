using AutoMapper;
using OrderService2.Models;

namespace OrderService2.Command.Mapper;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderRequest, OrderCreatedCommand>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}
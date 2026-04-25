using AutoMapper;
using OrderService.API.Controllers.Contracts;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Command.Mapper;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderRequest, OrderCreatedCommand>();
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Symbol,
                opt => opt.MapFrom(src => src.InstrumentId)); 
    }
}
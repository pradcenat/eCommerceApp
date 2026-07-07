using AutoMapper;
using OrderService.Application.DTO;
using OrderService.Application.RequestResponse;
using OrderService.Domain.Entity;

namespace OrderService.Application.Mappers
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(dest => dest.TotalPrice,
                    opt => opt.MapFrom(src => src.UnitPrice * src.Quantity));

            CreateMap<CreateOrderRequest, Order>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => OrderStatus.Pending))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());

            CreateMap<CreateOrderItemRequest, OrderItem>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.OrderId, opt => opt.Ignore());
        }
    }
}

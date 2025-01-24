using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Consumers;

namespace SearchService.RequestHelpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<AuctionCreated, Item>();
            CreateMap<AuctionUpdated, Item>();
        }
    }
}
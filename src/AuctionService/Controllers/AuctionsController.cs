using AuctionService.Data;
using AuctionService.Data.DTOs;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/auctions")]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;
        public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
        }
        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
        {

            var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

            if(!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }

            // var auctions = await _context.Auctions
            // .Include(x => x.Item)
            // .OrderBy(x => x.Item.Make)
            // .ToListAsync();

            // return _mapper.Map<List<AuctionDto>>(auctions);

            return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();

        }
        
        [HttpGet("{auctionId}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid auctionId)
        {
            var auction = await _context.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == auctionId);
            // .Where(x => x.Id == auctionId);

            if(auction is null) return NotFound();

            return _mapper.Map<AuctionDto>(auction);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        {
            var auction = _mapper.Map<Auction>(auctionDto);
            
            auction.Seller = User.Identity.Name;

            _context.Auctions.Add(auction);


            var newAuction = _mapper.Map<AuctionDto>(auction);

            await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Could not save changes to the DB");

            return CreatedAtAction(nameof(GetAuctionById), 
                new {auctionId = auction.Id},newAuction
            );
        }
        
        [Authorize]
        [HttpPut("{auctionId}")]
        public async Task<ActionResult> UpdateAuction(Guid auctionId, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await _context.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == auctionId);

            if(auction is null) return NotFound();

            bool canEdit = auction.Seller == User.Identity.Name; 
            if(!canEdit) return Forbid();

            Console.WriteLine(canEdit);
            Console.WriteLine(auction.Seller);
            Console.WriteLine(User.Identity.Name);

            //TO-Do: add auth check, seller==user
            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

            var auctionUpdatedMsg = _mapper.Map<AuctionUpdated>(auction);

            await _publishEndpoint.Publish(auctionUpdatedMsg);

            var result = await _context.SaveChangesAsync () > 0;
            
            if(result) return Ok();

            return BadRequest("Problem saving Changes");
        }
        
        [Authorize]
        [HttpDelete("{auctionId}")]
        public async Task<ActionResult> DeleteAuction(Guid auctionId)
        {
            var auction = await _context.Auctions.FindAsync(auctionId);
            if(auction is null) return NotFound();
            
            bool canDelete = auction.Seller == User.Identity.Name; 
            if(!canDelete) return Forbid();

            _context.Auctions.Remove(auction);

            var result = await _context.SaveChangesAsync() > 0;
            if(!result) return BadRequest("Could not update DB");

            return Ok();
        }   
    }
}
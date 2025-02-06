
using BiddingService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace BiddingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BidsController : ControllerBase
    {
        // private readonly IMapper _mapper;
        // private readonly IPublishEndpoint _publishEndpoint;
        // private readonly GrpcAuctionClient _grpcClient;

        // public BidsController(IMapper mapper, IPublishEndpoint publishEndpoint,
        //     GrpcAuctionClient grpcClient)
        // {
        //     _mapper = mapper;
        //     _publishEndpoint = publishEndpoint;
        //     _grpcClient = grpcClient;
        // }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Bid>> PlaceBid(string auctionId, int amount)
        {
            var auction = await DB.Find<Auction>().OneAsync(auctionId);

            if (auction is null)
            {
                //TODO: check with auctionService if tit has auciton
                return NotFound();
            }

            if (auction.Seller == User.Identity.Name)
            {
                return BadRequest("You cannot bid on your own auction");
            }
            var bid = new Bid
            {
                Amount = amount,
                AuctionId = auctionId,
                Bidder = User.Identity.Name
            };

            if (auction.AuctionEnd < DateTime.UtcNow)
            {
                bid.BidStatus = BidStatus.Finished;
            }
            else
            {
                var highBid = await DB.Find<Bid>()
                           .Match(a => a.AuctionId == auctionId)
                           .Sort(b => b.Descending(x => x.Amount))
                           .ExecuteFirstAsync();

                if (highBid is null || highBid is not null && amount > highBid.Amount)
                {
                    bid.BidStatus = amount > auction.ReservePrice
                    ? BidStatus.Accepted
                    : BidStatus.AcceptedBelowReserve;
                }
                if (highBid is not null && bid.Amount <= highBid.Amount)
                {
                    bid.BidStatus = BidStatus.TooLow;
                }
            }


            await DB.SaveAsync(bid);

            // await _publishEndpoiny.Publish(_mapper.Map<BidPlaced>bid));

            return Ok(bid);
        }
        [HttpGet("auctionId")]
        public async Task<ActionResult<List<Bid>>> GetBidsForAuction(string auctionId)
        {
            var bids = await DB.Find<Bid>()
            .Match(a => a.AuctionId == auctionId)
            .Sort(b => b.Descending(a => a.BidTime))
            .ExecuteAsync();

            return bids;
        }
    }
}
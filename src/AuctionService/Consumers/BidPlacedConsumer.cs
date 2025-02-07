using AuctionService.Data;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers
{
    public class BidPlacedConsumer : IConsumer<BidPlaced>
    {
        private readonly AuctionDbContext _dbContext;
        public BidPlacedConsumer(AuctionDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Consume(ConsumeContext<BidPlaced> context)
        {
            Console.WriteLine("--> Consuming Bid Placed");

            //Message.AuctionId es una string y el mongoMethod necesita un Guid
            Guid guidAuctionId = Guid.Parse(context.Message.AuctionId);
            var auction = await _dbContext.Auctions.FindAsync(guidAuctionId);

            bool newBidIsHigher = context.Message.BidStatus.Contains("Accepted") && context.Message.Amount > auction.CurrentHighBid;
            if (auction.CurrentHighBid is null || newBidIsHigher)
            {
                auction.CurrentHighBid = context.Message.Amount;
                await _dbContext.SaveChangesAsync();
            }

        }
    }
}
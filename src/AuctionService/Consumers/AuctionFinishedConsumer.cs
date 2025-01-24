using AuctionService.Data;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers
{
    public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
    {
        private readonly AuctionDbContext _dbContext;
        public AuctionFinishedConsumer(AuctionDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Consume(ConsumeContext<AuctionFinished> dbContext)
        {
            Console.WriteLine("--> Consuming Auction Finished");

            var auction = await _dbContext.Auctions.FindAsync(dbContext.Message.AuctionId);
            if(dbContext.Message.ItemSold)
            {
                auction.Winner = dbContext.Message.Winner;
                auction.SoldAmount = dbContext.Message.Amount;
            }
            auction.Status = auction.SoldAmount > auction.ReservePrice ? Status.Finished : Status.ReserveNotMet;

            await _dbContext.SaveChangesAsync();
        }
    }
}
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService;

namespace AuctionService.Consumers
{
    public class BidPlacedConsumer : IConsumer<BidPlaced>
    {
        //mongo db has acces gives us access via static methodsm, no need for dependency injection
        public async Task Consume(ConsumeContext<BidPlaced> context)
        {
            Console.WriteLine("--> Consuming Bid Placed");
            
            var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);
            
            bool newBidIsHigher = context.Message.BidStatus.Contains("Accepted") && context.Message.Amount > auction.CurrentHighBid;
            if (auction.CurrentHighBid is null || newBidIsHigher)
            {
                auction.CurrentHighBid = context.Message.Amount;
                await auction.SaveAsync();
            }

        }
    }
}
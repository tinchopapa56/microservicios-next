
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.RequestHelpers;

namespace SearchService.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {

        [HttpGet]
        // public async Task<ActionResult<List<Item>>> SearchItems(string searchParam, int pageNumber = 1, int pageSize = 4)
        public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams)
        {
            // var query = DB.Find<Item>();
            // var query = DB.PagedSearch<Item>();
            var query = DB.PagedSearch<Item, Item>();//for the OrdserBy

            // query.Sort(x => x.Ascending(a => a.Make));

            if(!string.IsNullOrEmpty(searchParams.SearchTerm))
            {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }

            query = searchParams.OrderBy switch
            {
                "make" => query.Sort(x => x.Ascending(auction => auction.Make))
                    .Sort(x => x.Ascending(a => a.Model)),
                "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
                _ => query.Sort(x => x.Ascending(a => a.AuctionEnd)),
            };
            query = searchParams.FilterBy switch
            {
                "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
                "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) 
                    && x.AuctionEnd > DateTime.UtcNow ),
                _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow),
            };

            if(!string.IsNullOrEmpty(searchParams.Seller))
            {
                query.Match(x => x.Seller == searchParams.Seller);
            }
            if(!string.IsNullOrEmpty(searchParams.Winner))
            {
                query.Match(x => x.Winner == searchParams.Seller);
            }

            query.PageNumber(searchParams.PageNumber);
            query.PageSize(searchParams.PageSize);

            var result = await query.ExecuteAsync();
            var paginatedResult = new 
            {
                results = result.Results, 
                pageCount = result.PageCount, 
                totalCount = result.TotalCount
            };

            return Ok(paginatedResult);
        }


    }
}
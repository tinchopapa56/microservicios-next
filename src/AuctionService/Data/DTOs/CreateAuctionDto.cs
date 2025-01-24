using System.ComponentModel.DataAnnotations;

namespace AuctionService.Data.DTOs
{
    public class CreateAuctionDto
    {
        [Required]
        public string Make { get; set; }

        [Required]
        public int Reserveprice { get; set;}
        
        [Required]
        public DateTime AuctionEnd { get; set;}

        /* Item - Car*/
        [Required]
        public string Model { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string Color { get; set; }

        [Required]
        public int Mileage { get; set; }

        [Required]
        public string ImageUrl { get; set; }

    }
}
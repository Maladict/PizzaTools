using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pizzeria.Models
{
    public class Order
    {
        [ForeignKey("Basket")]
        public int OrderId { get; set; }

        public bool Paid { get; set; } = false;
        public bool Delivered { get; set; } = false;
        public bool Finished { get; set; } = false;
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public int Shipping { get; set; }
        public int Total { get; set; }

        public ApplicationUser User { get; set; }


        public int BasketId { get; set; }
        public Basket Basket { get; set; }
    }
}
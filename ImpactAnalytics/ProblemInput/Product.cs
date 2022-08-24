using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalytics.ProblemInput
{
    public class Product
    {
        public int Id { get; set; }

        public int Inventory { get; set; }

        public double Cost { get; set; }
        private int weekCount { get; set; }
        public Product(int id, double inventory, double cost, ProdGroup prodGroup, int weekCount)
        {
            Id = id;
            Inventory = (int)inventory;
            Cost = cost;
            ProdGroup = prodGroup;
            DemandsByWeekPrice = new List<Dictionary<double, PriceDemand>>(weekCount);
            for(int i = 0; i < weekCount; i++)
            {
                DemandsByWeekPrice.Add(new Dictionary<double, PriceDemand>());
            }
            this.weekCount = weekCount;
        }

        public ProdGroup ProdGroup { get; set; }

        /// <summary>
        /// Index of the list represents week number of the Quater
        /// Key of the dictionary represents percentage Discount
        /// </summary>
        public List<Dictionary<double, PriceDemand>> DemandsByWeekPrice { get; set; }

        public override string ToString()
        {
            return Id.ToString();
        }
    }

    public class PriceDemand
    {
        public double SellingPrice { get; }
        public int Demand { get; }

        public PriceDemand(double sellingPrice, double demand)
        {
            SellingPrice = sellingPrice;
            Demand = (int)demand;
        }
    }
}

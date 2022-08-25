using ImpactAnalytics.ProblemInput;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalytics.Simulation
{
    public class Simulator
    {
        public ProdGroup ProdGroup { get; }

        private double[] discounts;

        public double TotalProfit { get; private set; }
        public double TotalInventoryLeft { get; private set; }

        public Simulator(ProdGroup prodGroup, double[] discount)
        {
            ProdGroup = prodGroup;
            this.discounts = discount;
        }

        public void Simulate()
        {
            ConcurrentBag<Tuple<int, double>> concurrentBag = new ConcurrentBag<Tuple<int, double>>();
            Parallel.ForEach(ProdGroup.Products, prod => 
            {
                var ret = Simulate(prod);
                concurrentBag.Add(ret);
            });
            foreach(var ret in concurrentBag)
            {
                TotalInventoryLeft += ret.Item1;
                TotalProfit += ret.Item2;
            }
        }

        private Tuple<int,double> Simulate(Product prod)
        {
            var inventoryLeft = prod.Inventory;
            var profit = 0.0;
            var week = 0;
            foreach(var discount in discounts)
            {
                var demand = prod.DemandsByWeekPrice[week][discount].Demand;
                var sellingPrice = prod.DemandsByWeekPrice[week][discount].SellingPrice;

                var quantitySold = Math.Min(inventoryLeft, demand);
                inventoryLeft -= quantitySold;
                profit += quantitySold * (sellingPrice - prod.Cost);
                if (inventoryLeft == 0)
                    break;
                week++;
            }
            return Tuple.Create(inventoryLeft, profit);
        }

        public string GetOutputAsString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Product Id,Week,Quantity Sold,Selling Price,Revenue,Cost,Demand");
            foreach(var prod in ProdGroup.Products)
            {
                var inventoryLeft = prod.Inventory;
                var profit = 0.0;
                var week = 0;
                foreach (var discount in discounts)
                {
                    var demand = prod.DemandsByWeekPrice[week][discount].Demand;
                    var sellingPrice = prod.DemandsByWeekPrice[week][discount].SellingPrice;

                    var quantitySold = Math.Min(inventoryLeft, demand);
                    inventoryLeft -= quantitySold;
                    profit += quantitySold * (sellingPrice - prod.Cost);
                    week++;
                    sb.AppendLine($"{prod.Id},{week},{quantitySold},{sellingPrice},{quantitySold * sellingPrice},{prod.Cost*quantitySold},{demand}");
                }
            }
            return sb.ToString();
        }
    }
}

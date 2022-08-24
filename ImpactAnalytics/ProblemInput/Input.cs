using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalytics.ProblemInput
{
    public class Input
    {
        private List<RawInput> RawData;
        public int WeekCount { get;private set; }

        private List<int> weekIds;
        private Dictionary<int,int> weeksById;

        public List<Product> Products { get; private set; }
        public List<ProdGroup> ProdGroups { get; private set; }
        public void ReadInput()
        {
            ReadRawInput();
            PopulateWeeks();
            PopulateProductsAndGroups();
            WeekCount = 13;
        }

        private void PopulateWeeks()
        {
            weekIds = new List<int>();
            weeksById = new Dictionary<int,int>();
            var minWeek = RawData.Min(r => r.WeekId);
            var maxWeek = RawData.Max(r => r.WeekId);
            WeekCount = maxWeek - minWeek + 1;
            var weekNumber = 0;
            for (var i = minWeek; i <= maxWeek; i++)
            {
                weekIds.Add(i);
                weeksById[i] = weekNumber;
                weekNumber++;
            }
        }

        private void PopulateProductsAndGroups()
        {
            var groupIds = RawData.Select(r => r.GroupId).Distinct().ToList();
            Products = new List<Product>();
            ProdGroups = new List<ProdGroup>();
            var groupsByProdGroup = RawData.GroupBy(r =>  r.GroupId );
            foreach(var groupByProdGroup in groupsByProdGroup)
            {
                var prodGroup = new ProdGroup(groupByProdGroup.Key);
                ProdGroups.Add(prodGroup);
                var groupsByProduct = groupByProdGroup.GroupBy(r => r.ProdId);
                foreach(var groupByProduct in groupsByProduct)
                {
                    var first = groupByProduct.First();
                    var product = new Product(groupByProduct.Key, first.Inventory, first.CostPrice, prodGroup, WeekCount);
                    Products.Add(product);
                    if (prodGroup.Products.Count < 100)
                        prodGroup.Products.Add(product);

                    foreach(var rawInput in groupByProduct)
                    {
                        var priceDemand = new PriceDemand(rawInput.SellingPrice, rawInput.Demand);
                        var weekNumber = GetWeekNumber(rawInput.WeekId);
                        product.DemandsByWeekPrice[weekNumber][rawInput.Discount] = priceDemand;
                        prodGroup.AllowedDiscounts.Add(rawInput.Discount);
                    }
                }
                prodGroup.PopulateAllowedDiscountsByWeek(WeekCount);
            }
        }

        private void ReadRawInput()
        {
            StreamReader inputStreamReader = new StreamReader("Data\\AssignmentData.csv");
            var headerRead = false;
            int prodIdIndex = 0;
            int discountIndex = 1;
            int weekIndex = 2;
            int groupIndex = 3;
            int demandIndex = 4;
            int sellingPriceIndex = 5;
            int inventoryIndex = 6;
            int costPriceIndex = 7;
            RawData = new List<RawInput>();
            while (!inputStreamReader.EndOfStream)
            {
                var line = inputStreamReader.ReadLine();
                var cells = line.Split(",").ToList();
                if (!headerRead)
                {
                    prodIdIndex = cells.IndexOf("product_id");
                    discountIndex = cells.IndexOf("price");
                    weekIndex = cells.IndexOf("week");
                    groupIndex = cells.IndexOf("group");
                    demandIndex = cells.IndexOf("demand");
                    sellingPriceIndex = cells.IndexOf("selling_price");
                    inventoryIndex = cells.IndexOf("total_inventory");
                    costPriceIndex = cells.IndexOf("cost_price");
                    headerRead = true;
                    continue;
                }
                var ProdId = int.Parse(cells[prodIdIndex]);
                var Discount = double.Parse(cells[discountIndex]);
                var WeekId = int.Parse(cells[weekIndex]);
                var GroupId = cells[groupIndex];
                var Demand = double.Parse(cells[demandIndex]);
                var SellingPrice = double.Parse(cells[sellingPriceIndex]);
                var Inventory = double.Parse(cells[inventoryIndex]);
                var costPrice = double.Parse(cells[costPriceIndex]);
                var input = new RawInput(ProdId,Discount,WeekId,GroupId,Demand,SellingPrice,Inventory,costPrice);
                RawData.Add(input);
            }
        }

        public int GetWeekId(int weekNumber)
        {
            return weekIds[weekNumber];
        }
        public int GetWeekNumber(int weekId)
        {
            return weeksById[weekId];
        }
    }

    public class RawInput
    {
        public int ProdId { get; }
        public double Discount { get;}
        public int WeekId { get;}
        public string GroupId { get;}
        public double Demand { get; }
        public double SellingPrice { get; }
        public double Inventory { get; }
        public double CostPrice { get; }

        public RawInput(int prodId, double discount, int weekId, string groupId, double demand, double sellingPrice, double inventory, double costPrice)
        {
            ProdId = prodId;
            Discount = discount;
            WeekId = weekId;
            GroupId = groupId;
            Demand = demand;
            SellingPrice = sellingPrice;
            Inventory = inventory;
            CostPrice = costPrice;
        }
    }
}

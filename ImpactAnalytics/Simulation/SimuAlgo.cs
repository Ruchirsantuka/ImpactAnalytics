using ImpactAnalytics.ProblemInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalytics.Simulation
{
    public class SimuAlgo
    {
        Input Input { get; }

        private int weekCount;

        Random random;
        public SimuAlgo(Input input)
        {
            Input = input;
            this.weekCount = input.WeekCount;
            random = new Random(0);
        }

        public void Run()
        {
            var group = Input.ProdGroups.First();
            double[] bestDiscount = new double[weekCount];
            var bestObj = 0.0;
            var totalInitialInventory = group.Products.Sum(p => p.Inventory);
            Stopwatch st = new Stopwatch();
            st.Start();
            for (int i = 0; i < Math.Min(10000, Math.Pow(weekCount, 3)); i++)
            {
                var discounts = GetRandomDiscountSequence(group);
                var simulator = new Simulator(group, discounts);
                simulator.Simulate();
                if (simulator.TotalInventoryLeft > 0.4 * totalInitialInventory)
                    continue;
                if (simulator.TotalProfit > bestObj)
                {
                    bestObj = simulator.TotalProfit;
                    bestDiscount = discounts;
                    Console.WriteLine($"Found better Solution. Itertaion: {i} \t\t Profit: {bestObj}");
                }
            }
            Console.WriteLine($"Took {st.ElapsedMilliseconds/100} sec");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best Solution found");
            for(int week = 0; week < weekCount; week++)
            {
                sb.AppendLine($"Week {week}: Discount {bestDiscount[week]}");
            }
            Console.WriteLine(sb.ToString());
        }

        private double[] GetRandomDiscountSequence(ProdGroup group)
        {
            var discounts = new double[weekCount];
            var week = 0;
            var allowedDiscounts = group.AllowedDiscountsByWeek[week].OrderBy(d => d).ToList();
            var discountIndex = random.Next(allowedDiscounts.Count);
            discounts[week] = allowedDiscounts[discountIndex];

            var maxDaviationOnOneSide = (int)(20 / 5);
            var maxAllowedDaviation = maxDaviationOnOneSide * 2 + 1;

            for (week = 1; week < weekCount; week++)
            {
                allowedDiscounts = group.AllowedDiscountsByWeek[week].OrderBy(d => d).ToList();
                var deviationIndex = random.Next(maxAllowedDaviation) - maxDaviationOnOneSide;
                discountIndex = Math.Min(allowedDiscounts.Count - 1, Math.Max(discountIndex + deviationIndex, 0));
                discounts[week] = allowedDiscounts[discountIndex];
            }
            return discounts;
        }
    }
}

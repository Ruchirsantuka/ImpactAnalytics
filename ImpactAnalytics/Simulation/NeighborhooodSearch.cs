using ImpactAnalytics.ProblemInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalytics.Simulation
{
    public class NeighborhooodSearch
    {
        Input Input { get; }

        private int weekCount;

        Random random;
        List<double>[] allowedDiscountsByWeek;


        public NeighborhooodSearch(Input input)
        {
            Input = input;
            this.weekCount = input.WeekCount;
            random = new Random(0);
        }

        public void Run()
        {
            var group = Input.ProdGroups.First();
            allowedDiscountsByWeek = new List<double>[weekCount];
            for(var week =0; week<weekCount;week++)
            {
                allowedDiscountsByWeek[week] = group.AllowedDiscountsByWeek[week].OrderBy(d => d).ToList();
            }
            double[] bestDiscount = new double[weekCount];
            var bestObj = 0.0;
            var totalInitialInventory = group.Products.Sum(p => p.Inventory);
            Stopwatch st = new Stopwatch();
            st.Start();
            int[] discIndexSequence = new int[weekCount];
            int[] bestIndexSequence = new int[weekCount];
            Console.WriteLine("Evaluating 1000 random solutions");
            for (int i = 0; i < 1000; i++)
            {
                var discounts = GetRandomDiscountSequence(out discIndexSequence);

                if (UpdateBestSol(ref bestDiscount, ref bestObj, i, discounts, group, totalInitialInventory))
                    bestIndexSequence = discIndexSequence;
            }

            Console.WriteLine("Evaluating 10000 neighbor solutions");
            for(int i = 0; i < 10000; i++)
            {
                var discounts = GetNeighborSol(bestIndexSequence, out var newDisckIndexSequence);
                if (newDisckIndexSequence.SequenceEqual(bestIndexSequence))
                    continue;
                if (UpdateBestSol(ref bestDiscount, ref bestObj, i, discounts, group, totalInitialInventory))
                    bestIndexSequence = newDisckIndexSequence;
            }
            var simulator = new Simulator(group, bestDiscount);
            File.WriteAllText("Output.csv", simulator.GetOutputAsString());
            Console.WriteLine($"Solved in {st.ElapsedMilliseconds/1000} sec");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best Solution found");
            for(int week = 0; week < weekCount; week++)
            {
                sb.AppendLine($"Week {week}: Discount {bestDiscount[week]}");
            }
            Console.WriteLine(sb.ToString());
        }

        private static bool UpdateBestSol(ref double[] bestDiscount, ref double bestObj, int i, double[] discounts, ProdGroup group, double totalInitialInventory)
        {
            var simulator = new Simulator(group, discounts);
            simulator.Simulate();
            if (simulator.TotalInventoryLeft > 0.4 * totalInitialInventory)
                return false;
            if (simulator.TotalProfit > bestObj)
            {
                bestObj = simulator.TotalProfit;
                bestDiscount = discounts;
                Console.WriteLine($"Found better Solution. Itertaion: {i} \t\t Profit: {bestObj}");
                return true;
            }
            return false;
        }

        private double[] GetRandomDiscountSequence(out int[] discIndexSequence)
        {
            var discounts = new double[weekCount];
            discIndexSequence = new int[weekCount];
            var week = 0;
            var allowedDiscounts = allowedDiscountsByWeek[week].OrderBy(d => d).ToList();
            var discountIndex = random.Next(allowedDiscounts.Count);
            discounts[week] = allowedDiscounts[discountIndex];
            discIndexSequence[week] = discountIndex;

            var maxDaviationOnOneSide = (int)(20 / 5);
            var maxAllowedDaviation = maxDaviationOnOneSide * 2 + 1;

            for (week = 1; week < weekCount; week++)
            {
                allowedDiscounts = allowedDiscountsByWeek[week].OrderBy(d => d).ToList();
                var deviationIndex = random.Next(maxAllowedDaviation) - maxDaviationOnOneSide;
                discountIndex = Math.Min(allowedDiscounts.Count - 1, Math.Max(discountIndex + deviationIndex, 0));
                discounts[week] = allowedDiscounts[discountIndex];
                discIndexSequence[week] = discountIndex;
            }
            return discounts;
        }

        private double[] GetNeighborSol(int[] currentDiscIndexSequence, out int[] newDiscIndexSequence)
        {
            var param = 0.1;
            var maxDaviationOnOneSide = (int)(20 / 5);
            newDiscIndexSequence = new int[weekCount];
            var newSequence = new double[weekCount];
            for (var week = 0; week < weekCount; week++)
            {
                var randNum = random.NextDouble();
                var change = 0;
                if (randNum < param)
                    change = -1;
                else if (randNum >= 1 - param)
                    change = 1;
                var discIndex = currentDiscIndexSequence[week] + change;
                discIndex = Math.Max(0, discIndex);
                discIndex = Math.Min(allowedDiscountsByWeek[week].Count - 1, discIndex);
                if(week > 0)
                {
                    var discPrevWeek = newDiscIndexSequence[week - 1];
                    discIndex = Math.Min(discPrevWeek + maxDaviationOnOneSide, Math.Max(discPrevWeek - maxDaviationOnOneSide, discIndex));
                }
                newDiscIndexSequence[week] = discIndex;
                var disc = allowedDiscountsByWeek[week][discIndex];
                newSequence[week] = disc;
            }
            return newSequence;
        }
    }
}

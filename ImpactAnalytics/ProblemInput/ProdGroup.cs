using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalytics.ProblemInput
{
    public class ProdGroup
    {

        public string Name;

        public List<Product> Products;

        public HashSet<double> AllowedDiscounts;
        public HashSet<double>[] AllowedDiscountsByWeek;

        public ProdGroup(string name)
        {
            Name = name;
            Products = new List<Product>();
            AllowedDiscounts = new HashSet<double>();
        }

        public void PopulateAllowedDiscountsByWeek(int weekCount)
        {
            AllowedDiscountsByWeek = new HashSet<double>[weekCount];
            for(int week = 0; week < weekCount; week++)
            {
                var allowedDiscounts = new HashSet<double>();
                AllowedDiscountsByWeek[week] = allowedDiscounts;
                foreach(var discount in AllowedDiscounts)
                {
                    if(Products.All(p => p.DemandsByWeekPrice[week].ContainsKey(discount)))
                    {
                        allowedDiscounts.Add(discount);
                    }
                }
            }
        }
    }
}

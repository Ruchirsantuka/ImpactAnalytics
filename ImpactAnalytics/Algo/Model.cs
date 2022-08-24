using Google.OrTools.LinearSolver;
using ImpactAnalytics.ProblemInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalytics.Algo
{
    public class Model
    {
        Input ProblemInput;

        Solver Solver;


        public Model(Input problemInput)
        {
            ProblemInput = problemInput;
            Solver = Solver.CreateSolver("SCIP");
            Solver.EnableOutput();
        }

        public void BuildModel()
        {
            PopulateVariables();
            PopulateConstraints();
            PopulateObjective();
        }

        public void Solve()
        {
            Solver.Solve();
        }

        #region Variable
        //Variables
        Dictionary<Product, Variable[]> InventoryVariables;
        Dictionary<Product, Variable[]> QuantitySoldVariables;
        Dictionary<Product, Variable[]> SellingPriceVariables;
        Dictionary<Product, Variable[]> RevenueVariables;
        Dictionary<ProdGroup, Dictionary<double, Variable>[]> DiscountVariables;
        Dictionary<Product, Dictionary<double, Variable>[]> QuantitySoldAtDiscountVariables;

        /// <summary>
        /// When inventory goes out of stock
        /// </summary>
        Dictionary<Product, Variable[]> OOS_Variables;
        /// <summary>
        /// If inventory exists
        /// </summary>
        Dictionary<Product, Variable[]> ES_Variables;
        private void PopulateVariables()
        {
            InventoryVariables = new Dictionary<Product, Variable[]>();
            QuantitySoldVariables = new Dictionary<Product, Variable[]>();
            SellingPriceVariables = new Dictionary<Product, Variable[]>();
            RevenueVariables = new Dictionary<Product, Variable[]>();
            DiscountVariables = new Dictionary<ProdGroup, Dictionary<double, Variable>[]>();
            QuantitySoldAtDiscountVariables = new Dictionary<Product, Dictionary<double, Variable>[]>();
            OOS_Variables = new Dictionary<Product, Variable[]>();
            ES_Variables = new Dictionary<Product, Variable[]>();


            foreach (var group in ProblemInput.ProdGroups)
            {
                DiscountVariables[group] = new Dictionary<double, Variable>[ProblemInput.WeekCount];
                for (var week = 0; week < ProblemInput.WeekCount; week++)
                {
                    DiscountVariables[group][week] = new Dictionary<double, Variable>();
                    foreach (var discount in group.AllowedDiscountsByWeek[week])
                    {
                        DiscountVariables[group][week][discount] = Solver.MakeBoolVar($"DiscSelected_{group.Name}_{week}_{discount}");

                    }
                }

                foreach (var prod in group.Products)
                {
                    InventoryVariables[prod] = new Variable[ProblemInput.WeekCount];
                    QuantitySoldVariables[prod] = new Variable[ProblemInput.WeekCount];
                    SellingPriceVariables[prod] = new Variable[ProblemInput.WeekCount];
                    RevenueVariables[prod] = new Variable[ProblemInput.WeekCount];
                    QuantitySoldAtDiscountVariables[prod] = new Dictionary<double, Variable>[ProblemInput.WeekCount];

                    OOS_Variables[prod] = new Variable[ProblemInput.WeekCount];
                    ES_Variables[prod] = new Variable[ProblemInput.WeekCount];


                    for (var week = 0; week < ProblemInput.WeekCount; week++)
                    {
                        InventoryVariables[prod][week] = Solver.MakeIntVar(0, prod.Inventory, $"Inventory_{prod.Id}_{week}");
                        QuantitySoldVariables[prod][week] = Solver.MakeIntVar(0, prod.DemandsByWeekPrice[week].Values.Max(p => p.Demand), $"QuantitySold_{prod.Id}_{week}");
                        SellingPriceVariables[prod][week] = Solver.MakeNumVar(0, prod.DemandsByWeekPrice[week].Values.Max(p => p.SellingPrice), $"SellingPrice_{prod.Id}_{week}");
                        RevenueVariables[prod][week] = Solver.MakeNumVar(0, prod.DemandsByWeekPrice[week].Values.Max(p => p.Demand * p.SellingPrice), $"Revenue_{prod.Id}_{week}");

                        QuantitySoldAtDiscountVariables[prod][week] = new Dictionary<double, Variable>();

                        OOS_Variables[prod][week] = Solver.MakeBoolVar($"OutStock_{prod.Id}_{week}");
                        ES_Variables[prod][week] = Solver.MakeBoolVar($"Inventory_Exists_{prod.Id}_{week}");

                        foreach (var discount in group.AllowedDiscountsByWeek[week])
                        {
                            QuantitySoldAtDiscountVariables[prod][week][discount] = Solver.MakeNumVar(0, prod.DemandsByWeekPrice[week][discount].Demand, $"QuanitiySoldAtDisc_{prod.Id}_{week}_{discount}");
                        }
                    }
                }
            }
        }
        #endregion Variable

        private void PopulateObjective()
        {
            var objective = Solver.Objective();

            foreach (var group in ProblemInput.ProdGroups)
            {
                foreach (var prod in group.Products)
                {
                    for (int week = 0; week < ProblemInput.WeekCount; week++)
                    {
                        objective.SetCoefficient(RevenueVariables[prod][week], 1);
                        objective.SetCoefficient(QuantitySoldVariables[prod][week], -prod.Cost);
                    }
                }
            }
            objective.SetOptimizationDirection(true);
        }

        private void PopulateConstraints()
        {
            PopulateFlowBalanceConstraint();
            PopulateSingleDiscount();
            //PopulateSellingPriceDetermination();
            PopulateRevenueDetermination();
            PopulateMinimumSell();
            PopulateCaterDemand();
            PopulateDiscRestrictionConstraint();
        }

        private void PopulateDiscRestrictionConstraint()
        {
            foreach (var group in ProblemInput.ProdGroups)
            {
                for (int week = 1; week < ProblemInput.WeekCount; week++)
                {
                    var ctr = Solver.MakeConstraint(-20, 20, $"Disc_Restriction");
                    foreach (var discount in group.AllowedDiscountsByWeek[week])
                    {
                        ctr.SetCoefficient(DiscountVariables[group][week - 1][discount], discount);
                        ctr.SetCoefficient(DiscountVariables[group][week][discount], -discount);
                    }
                }
            }
        }

        Dictionary<Product, Constraint[]> FlowBalance;
        private void PopulateFlowBalanceConstraint()
        {
            FlowBalance = new Dictionary<Product, Constraint[]>();
            foreach (var group in ProblemInput.ProdGroups)
            {
                foreach (var prod in group.Products)
                {
                    FlowBalance[prod] = new Constraint[ProblemInput.WeekCount];

                    for(int week = 0; week < ProblemInput.WeekCount; week++)
                    {
                        var ctr = Solver.MakeConstraint(0,0,$"FlowBal_{prod.Id}_{week}");
                        FlowBalance[prod][week] = ctr;
                        if (week >= 1)
                        {
                            ctr.SetCoefficient(InventoryVariables[prod][week - 1], 1);
                        }
                        else
                        {
                            ctr.SetBounds(-prod.Inventory, -prod.Inventory);
                        }

                        ctr.SetCoefficient(InventoryVariables[prod][week], -1);
                        ctr.SetCoefficient(QuantitySoldVariables[prod][week], -1);
                    }
                }
            }
        }

        Dictionary<Product, Constraint[]> SellingPriceDetermination;

        private void PopulateSellingPriceDetermination()
        {
            SellingPriceDetermination = new Dictionary<Product, Constraint[]>();
            foreach (var group in ProblemInput.ProdGroups)
            {
                foreach (var prod in group.Products)
                {
                    SellingPriceDetermination[prod] = new Constraint[ProblemInput.WeekCount];

                    for (int week = 0; week < ProblemInput.WeekCount; week++)
                    {
                        var ctr = Solver.MakeConstraint(0, 0, $"SellPrice_Determination{prod.Id}_{week}");
                        SellingPriceDetermination[prod][week] = ctr;

                        ctr.SetCoefficient(SellingPriceVariables[prod][week], 1);
                        foreach(var discount in group.AllowedDiscountsByWeek[week])
                        {
                            ctr.SetCoefficient(DiscountVariables[group][week][discount], -prod.DemandsByWeekPrice[week][discount].SellingPrice);
                        }
                    }
                }
            }
        }
        Dictionary<ProdGroup, Constraint[]> SingleDiscount;
        private void PopulateSingleDiscount()
        {
            SingleDiscount = new Dictionary<ProdGroup, Constraint[]>();
            foreach (var group in ProblemInput.ProdGroups)
            {
                SingleDiscount[group] = new Constraint[ProblemInput.WeekCount];
                for (int week = 0; week < ProblemInput.WeekCount; week++)
                {
                    var ctr = Solver.MakeConstraint(1,1,$"SingleDiscount_{group.Name}_{week}");
                    SingleDiscount[group][week] = ctr;
                    foreach(var discount in group.AllowedDiscountsByWeek[week])
                    {
                        ctr.SetCoefficient(DiscountVariables[group][week][discount], 1);
                    }
                }
            }
        }

        Dictionary<Product, Constraint[]> RevenueDetermination;

        private void PopulateRevenueDetermination()
        {
            RevenueDetermination = new Dictionary<Product, Constraint[]>();
            foreach (var group in ProblemInput.ProdGroups)
            {
                foreach (var prod in group.Products)
                {
                    RevenueDetermination[prod] = new Constraint[ProblemInput.WeekCount];

                    for (int week = 0; week < ProblemInput.WeekCount; week++)
                    {
                        var revenueDterminationctr = Solver.MakeConstraint(0, 0, $"Revenue_Determination_{prod.Id}_{week}");
                        RevenueDetermination[prod][week] = revenueDterminationctr;
                        revenueDterminationctr.SetCoefficient(RevenueVariables[prod][week], -1);

                        var bigM = prod.DemandsByWeekPrice[week].Values.Max(p => p.Demand);


                        var quantSoldBalance = Solver.MakeConstraint(0, 0, $"QuantSoldBalance_{prod.Id}_{week}");
                        quantSoldBalance.SetCoefficient(QuantitySoldVariables[prod][week], -1);
                        foreach (var discount in group.AllowedDiscountsByWeek[week])
                        {
                            //var quatAtDiscCtr = Solver.MakeConstraint($"QuantityAtDisc_{prod.Id}_{week}_{discount}");
                            //quatAtDiscCtr.SetCoefficient(QuantitySoldVariables[prod][week], -1);
                            //quatAtDiscCtr.SetLb(-bigM);
                            //quatAtDiscCtr.SetCoefficient(QuantitySoldAtDiscountVariables[prod][week][discount], 1);
                            //quatAtDiscCtr.SetCoefficient(DiscountVariables[group][week][discount], -bigM);

                            revenueDterminationctr.SetCoefficient(QuantitySoldAtDiscountVariables[prod][week][discount], prod.DemandsByWeekPrice[week][discount].SellingPrice);
                            quantSoldBalance.SetCoefficient(QuantitySoldAtDiscountVariables[prod][week][discount], 1);
                        }
                    }
                }
            }
        }

        private void PopulateMinimumSell()
        {
            var ctr = Solver.MakeConstraint($"MinimumSell");
            var ub = 0.0;
            foreach (var group in ProblemInput.ProdGroups)
            {
                foreach (var prod in group.Products)
                {
                    ctr.SetCoefficient(InventoryVariables[prod][ProblemInput.WeekCount - 1], 1);
                    ub += 0.4 * prod.Inventory;
                }
            }
            ctr.SetUb(ub);
        }

        private void PopulateCaterDemand()
        {
            foreach (var group in ProblemInput.ProdGroups)
            {
                foreach (var prod in group.Products)
                {
                    ES_Variables[prod][0].SetLb(Math.Min(prod.Inventory, 1));
                    
                    var oosOnceCtr = Solver.MakeConstraint(0, 1, $"OOS_Once_{prod}");

                    for (int week = 0; week < ProblemInput.WeekCount - 1; week++)
                    {
                        Constraint inventoryExistsStateCtr = Solver.MakeConstraint(0, 0, $"inventoryExistState_{prod}_{week}");
                        inventoryExistsStateCtr.SetCoefficient(ES_Variables[prod][week], 1);
                        inventoryExistsStateCtr.SetCoefficient(ES_Variables[prod][week + 1], -1);
                        inventoryExistsStateCtr.SetCoefficient(OOS_Variables[prod][week], -1);
                    }

                    for (int week = 0; week < ProblemInput.WeekCount; week++)
                    {
                        //var es_CapByInventoryCtr = Solver.MakeConstraint($"Es_CapByInventory_{prod.Id}_{week}");
                        //es_CapByInventoryCtr.SetUb(0);
                        //es_CapByInventoryCtr.SetCoefficient(ES_Variables[prod][week], 1);
                        //es_CapByInventoryCtr.SetCoefficient(InventoryVariables[prod][week], -1);

                        oosOnceCtr.SetCoefficient(OOS_Variables[prod][week], 1);

                        var bigM = prod.DemandsByWeekPrice[week].Values.Max(p => p.Demand);


                        foreach(var discount in group.AllowedDiscountsByWeek[week])
                        {
                            var demand = prod.DemandsByWeekPrice[week][discount].Demand;
                            var sellCapCtr = Solver.MakeConstraint($"SellCapCtr_{prod.Id}_{week}_{discount}");
                            sellCapCtr.SetUb(0);
                            sellCapCtr.SetCoefficient(QuantitySoldAtDiscountVariables[prod][week][discount], 1);
                            sellCapCtr.SetCoefficient(DiscountVariables[group][week][discount], -demand);

                            var mustSatisfyDemand = Solver.MakeConstraint($"MustSatisfyDem_{prod.Id}_{week}_{discount}");
                            mustSatisfyDemand.SetLb(-demand);
                            mustSatisfyDemand.SetCoefficient(QuantitySoldAtDiscountVariables[prod][week][discount], 1);
                            mustSatisfyDemand.SetCoefficient(ES_Variables[prod][week], -demand);
                            mustSatisfyDemand.SetCoefficient(OOS_Variables[prod][week], demand);
                            mustSatisfyDemand.SetCoefficient(DiscountVariables[group][week][discount], -demand);
                        }

                        var cantSatisfyDemand = Solver.MakeConstraint($"CantSatisfyDemand_{prod.Id}_{week}");
                        cantSatisfyDemand.SetUb(0);
                        cantSatisfyDemand.SetCoefficient(QuantitySoldVariables[prod][week], 1);
                        cantSatisfyDemand.SetCoefficient(ES_Variables[prod][week], -bigM);

                    }
                }
            }
        }

        public void PrintLp()
        {
            var lpFileContent = Solver.ExportModelAsLpFormat(false);
            File.WriteAllText("LpFile.lp", lpFileContent);
        }

        public void PrintSol()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Product Id,Week,Quantity Sold,Selling Price,Revenue,Cost,Demand");
            foreach (var group in ProblemInput.ProdGroups)
            {
                for (var week = 0; week < ProblemInput.WeekCount; week++)
                {
                    var discount = 0.0;
                    foreach (var d in group.AllowedDiscountsByWeek[week])
                    {
                        discount += DiscountVariables[group][week][d].SolutionValue() * d;
                    }
                    foreach (var prod in group.Products)
                    {
                        var quantitySold = QuantitySoldVariables[prod][week].SolutionValue();
                        var revenue = RevenueVariables[prod][week].SolutionValue();
                        var sellingPrice = prod.DemandsByWeekPrice[week][discount].SellingPrice;
                        var demand = prod.DemandsByWeekPrice[week][discount].Demand;
                        sb.AppendLine($"{prod.Id},{ProblemInput.GetWeekId(week)},{quantitySold},{sellingPrice},{revenue},{quantitySold*prod.Cost},{demand}");
                    }
                }
            }
            File.WriteAllText("QuantitySold.csv", sb.ToString());
        }
    }

}

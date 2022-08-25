// See https://aka.ms/new-console-template for more information
using ImpactAnalytics.Algo;
using ImpactAnalytics.ProblemInput;
using ImpactAnalytics.Simulation;


Console.WriteLine("Enter max number of products to be considered");
var maxProductsToBeConsidered = int.Parse(Console.ReadLine());
Input input = new Input();
input.ReadInput(maxProductsToBeConsidered);

Console.WriteLine("Choose Algo");
Console.WriteLine("1. MILP");
Console.WriteLine("2. Neighborhood Search");
var readLine = Console.ReadLine();
while(readLine != "1" && readLine != "2")
{
    Console.WriteLine("Enter 1 or 2");
    readLine = Console.ReadLine();
}

if (readLine == "1")
{
    Model model = new Model(input);
    model.BuildModel();
    model.Solve();
    model.PrintLp();
    model.PrintSol();
}

else if (readLine == "2")
{
    var simuAlgo = new NeighborhooodSearch(input);
    simuAlgo.Run();
}
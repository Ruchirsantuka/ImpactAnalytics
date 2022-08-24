// See https://aka.ms/new-console-template for more information
using ImpactAnalytics.Algo;
using ImpactAnalytics.ProblemInput;

Input input = new Input();
input.ReadInput();

Model model = new Model(input);
model.BuildModel();
model.Solve();
model.PrintLp();
model.PrintSol();
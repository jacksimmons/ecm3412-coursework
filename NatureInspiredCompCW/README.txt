----- Steps necessary to run the ACO algorithm:
1. Select parameters for the program; number of experiment steps, number of algorithm executions, algorithm type, etc.
    These parameters can be found (and changed) in the Program class, as attributes.
2. Create all sub-experiments you want in Experiment.RunExperiment.
    Note: Running the Number of Ants sub-experiment can take a while if the upper bound is of magnitude 10^3 or higher.
3. Run Program.cs, which requires a C# compiler. This can be obtained in one of these ways:
	3a. Install C# tools in Visual Studio and open the solution (.sln file).
		Then run the open program (F5).
	3b. Install the .NET CLI tool (https://github.com/dotnet/sdk/)
		Then open the working directory in a terminal, and run the commands:
			dotnet build --output {output_dir}
			dotnet ./{output_dir}/NatureInspiredCompCW.dll
        Description of the dotnet command: https://learn.microsoft.com/en-gb/dotnet/core/tools


----- To run an experiment
Await RunExperiment in Program.Main.
Alter the RunExperiment method in Experiment.cs to change which parameters to experiment.

    (In Main())
    await RunExperiment();


----- To run ACO with no experimentation
Await RunAlgorithm or RunAlgorithms in Program.Main:

    (In Main())
    await RunAlgorithm(params...);
    await RunAlgorithms(params...)


----- What this program outputs:
The program outputs the global lowest cost path found every time all ants finish.
It also outputs the total time taken for the program to execute.

Note: If you set numAlgorithms > 1, this output will be hard to read, as multiple algorithms
will be printing to the console simultaneously.
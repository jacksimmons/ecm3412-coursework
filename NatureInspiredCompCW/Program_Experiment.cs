using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ECM3412;


public enum ACONumberParameter
{
    NumAnts,
    NumIterations,
    PheromoneEvaporationRate,
    PheromoneImportance,
    StagnationIterations
}


public enum ACOVariation
{
    Standard,
    Elitist,
    MMAS,
}


public enum ACOHeuristic
{
    InverseDistance, // 1/d
    QOverDistance, // Q/d
    QSquaredOverDistance, // (Q^2)/d
}


partial class Program
{
    /// <summary>
    /// Experiment "suite" - run all experiments in here.
    /// </summary>
    public static async Task RunExperiment(float[,] distMatrix)
    {
        ACOSettings settings = defaultSettings;
        settings.suppressPrints = true;

        // vvvvv Experiment calls go here vvvvv

        // Example experiment. Runs over the number of iterations in the specified bounds.
        await RunSubExperiment(settings, ACONumberParameter.NumIterations, 1, 1001, distMatrix);
    }


    /// <summary>
    /// A generic method for grid search which tunes one parameter.
    /// </summary>
    /// <param name="settings">Default settings to operate on.</param>
    /// <param name="param">The parameter to tune.</param>
    /// <param name="LB">The lower bound of the parameter.</param>
    /// <param name="UB">The upper bound of the parameter.</param>
    /// <param name="distMatrix">The distance matrix.</param>
    public static async Task RunSubExperiment(ACOSettings settings, ACONumberParameter param, float LB, float UB, float[,] distMatrix)
    {
        // Facilitates generic parameter tuning - we use a float value so any necessary casting occurs here.
        void AssignSettingsParam(float value)
        {
            switch (param)
            {
                case ACONumberParameter.NumAnts:
                    settings.numAnts = (int)value;
                    break;
                case ACONumberParameter.NumIterations:
                    settings.requiredIterations = (int)value;
                    break;
                case ACONumberParameter.PheromoneEvaporationRate:
                    settings.pheroEvapRate = value;
                    break;
                case ACONumberParameter.PheromoneImportance:
                    settings.pheroImportance = value;
                    break;
                case ACONumberParameter.StagnationIterations:
                    settings.stagnantCountMax = (int)value;
                    break;
            }
        }

        Console.WriteLine($"Running experiment over {param}, LB: {LB}, UB: {UB}, Steps: {numExperimentSteps}\n");
        float step = (UB - LB) / numExperimentSteps;

        // Overwrite default settings. Structs are passed by value, so this does not affect the default settings.
        AssignSettingsParam(LB);

        float value = LB;

        // For loop is inclusive on the number of steps (N), because
        // Iteration 0 corresponds to the LB (LB + 0)
        // Iteration N corresponds to the UB (LB + (UB - LB) / N * N = UB)
        for (int i = 0; i <= numExperimentSteps; i++)
        {
            Console.WriteLine($"With parameter {param}: {value} (Floored if param is int)");
            await RunAlgorithms(settings, distMatrix);

            value += step;
            AssignSettingsParam(value);
        }
    }


    public static async Task RunVariationExperiment(ACOSettings settings, float[,] distMatrix)
    {
        //Console.WriteLine($"Running experiment over Algorithm Variation\n");
        //Console.WriteLine($"Variation: Standard");
        //settings.variation = ACOVariation.Standard;
        //await RunAlgorithms(settings, distMatrix);

        //Console.WriteLine($"Variation: Elitist");
        //settings.variation = ACOVariation.Elitist;
        //await RunAlgorithms(settings, distMatrix);

        Console.WriteLine($"Variation: MMAS");
        settings.variation = ACOVariation.MMAS;
        await RunPheromoneRangeExperiment(settings, 10e-6f, 100, distMatrix);
    }


    public static async Task RunHeuristicExperiment(ACOSettings settings, float[,] distMatrix)
    {
        Console.WriteLine($"Running experiment over Heuristic Function\n");
        Console.WriteLine($"Heuristic: 1/d");
        settings.heuristic = ACOHeuristic.InverseDistance;
        await RunAlgorithms(settings, distMatrix);

        Console.WriteLine($"Heuristic: Q/d");
        settings.heuristic = ACOHeuristic.QOverDistance;
        await RunSubExperiment(settings, ACONumberParameter.PheromoneImportance, 1, 10, distMatrix);

        //Console.WriteLine($"Heuristic: (Q^2)/d");
        //settings.heuristic = ACOHeuristic.QSquaredOverDistance;
        //await RunSubExperiment(settings, ACONumberParameter.PheromoneImportance, 1, 20, distMatrix);
    }


    public static async Task RunPheromoneRangeExperiment(ACOSettings settings, float lb, float range, float[,] distMatrix)
    {
        Console.WriteLine($"Running experiment over Pheromone Range, LB: {lb}, Range: {range}, Steps: {numExperimentSteps}\n"
            + "Note: This experiment will do nothing if the selected algorithm is not MMAS.");
        float step = (range - lb) / numExperimentSteps;

        // Overwrite default settings. Structs are passed by value, so this does not affect the default settings.
        settings.suppressPrints = true;
        settings.pheroMin = settings.pheroMax = lb;
        for (int i = 0; i <= numExperimentSteps; i++)
        {
            Console.WriteLine($"Pheromone Range: LB: {settings.pheroMin}, UB: {settings.pheroMax}");
            await RunAlgorithms(settings, distMatrix);

            settings.pheroMax += step;
        }
    }
}

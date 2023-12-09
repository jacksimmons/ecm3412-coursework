using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ECM3412;


internal enum ACOParameter
{
    NumAnts,
    NumIterations,
    PheromoneEvaporationRate,
}


partial class Program
{
    /// <summary>
    /// Runs an experiment across all parameters - a grid search for tuning each parameter within a range.
    /// Choose which experiments, and how many experiments, in here.
    /// </summary>
    public static async Task RunExperiment(float[,] distMatrix)
    {
        //await RunSubExperiment(m_defaultSettings, ACOParameter.NumIterations, 1, 1000);
        await RunVariationExperiment(settings, distMatrix);
    }


    /// <summary>
    /// A generic method for grid search which tunes one parameter.
    /// </summary>
    /// <param name="settings">Default settings to operate on.</param>
    /// <param name="param">The parameter to tune.</param>
    /// <param name="LB">The lower bound of the parameter.</param>
    /// <param name="UB">The upper bound of the parameter.</param>
    /// <param name="distMatrix">The distance matrix.</param>
    public static async Task RunSubExperiment(ACOSettings settings, ACOParameter param, float LB, float UB, float[,] distMatrix)
    {
        // Facilitates generic parameter tuning - we use a float value so any necessary casting occurs here.
        void AssignSettingsParam(float value)
        {
            switch (param)
            {
                case ACOParameter.NumAnts:
                    settings.numAnts = (int)value;
                    break;
                case ACOParameter.NumIterations:
                    settings.requiredIterations = (int)value;
                    break;
                case ACOParameter.PheromoneEvaporationRate:
                    settings.pheroEvapRate = value;
                    break;
            }
        }

        Console.WriteLine($"Running experiment over {param}, LB: {LB}, UB: {UB}, Steps: {numExperimentSteps}\n");
        float step = (UB - LB) / numExperimentSteps;

        // Overwrite default settings. Structs are passed by value, so this does not affect the default settings.
        AssignSettingsParam(LB);
        settings.suppressPrints = true;

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
        Console.WriteLine($"Running experiment over Algorithm Variation\n");
        Console.WriteLine($"Variation: Standard");
        settings.variation = ACOVariation.Standard;
        await RunAlgorithms(settings, distMatrix);

        Console.WriteLine($"Variation: Elitist");
        settings.variation = ACOVariation.Elitist;
        await RunAlgorithms(settings, distMatrix);

        Console.WriteLine($"Variation: MMAS");
        settings.variation = ACOVariation.MMAS;
        await RunAlgorithms(settings, distMatrix);
    }
}

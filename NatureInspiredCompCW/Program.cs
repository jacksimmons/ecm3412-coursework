using System.Xml.Serialization;

namespace ECM3412;


partial class Program
{
    // ----- Parameters
    // The name (+ extension) of the file (TSPLIB, .xml) to run the algorithm on.
    private const string filename = "Burma.xml";

    // Number of steps for each experiment. The higher this is, the longer execution will take,
    // but the higher the accuracy of the optimal value of each parameter.
    private const int numExperimentSteps = 10;

    // Number of parallel runs of the algorithm.
    private const int numAlgorithms = 16;

    // Settings for the algorithm. Note that altering these will affect experiments.
    private static readonly ACOSettings settings = new()
    {
        variation = ACOVariation.Standard,

        startVertex = 0,

        numAnts = 100,
        requiredIterations = 100,

        pheroRandAdd = 0f,
        pheroRandMult = 1f,

        probPheroExponent = 1f,
        probDesirabilityExponent = 1f,

        pheroEvapRate = 0.1f,
        pheroAddRate = 0.5f,

        pheroMin = 0.1f,
        pheroMax = 1.0f,
        maxStagnantIterations = 10000,

        suppressPrints = true,
    };


    public static async Task Main()
    {
        // Log the starting time of the program to calculate execution time
        long startTime = DateTime.Now.Ticks;

        // Get distance matrix.
        float[,] distMatrix = TSPLIBToDistanceMatrix();

        // Run a grid search for each parameter in turn
        await RunExperiment(distMatrix);

        RunGreedySearch(distMatrix);

        // Calculate and print the execution time.
        long elapsedTime = DateTime.Now.Ticks - startTime;
        TimeSpan timeSpan = new(elapsedTime);
        Console.WriteLine($"Execution took: {timeSpan.TotalMilliseconds} ms");
    }


    /// <summary>
    /// Extract a distance matrix from a TSPLIB (.xml) file.
    /// </summary>
    /// <returns>The extracted distance matrix.</returns>
    /// <exception cref="FileLoadException">If a file could not be deserialized.</exception>
    public static float[,] TSPLIBToDistanceMatrix()
    {
        // Reference: Microsoft https://learn.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer.deserialize?view=net-8.0#system-xml-serialization-xmlserializer-deserialize(system-io-stream)
        // Deserialize the data stored in XML to C# objects.
        // -- Create a serializer to convert XML to TSPInstance.
        XmlSerializer serializer = new XmlSerializer(typeof(TSPInstance));
        
        // -- Create a File Stream for the XML file, to read it.
        using FileStream fs = new(filename, FileMode.Open, FileAccess.Read);
        
        // -- Deserialize the file. Return type can be null; if it is, the deserialization failed.
        object deserialized = serializer.Deserialize(fs) ?? throw new FileLoadException("File failed to deserialize.");
        
        // -- Explicit cast object->TSPInstance
        TSPInstance instance = (TSPInstance)deserialized;

        // -- Return distance matrix.
        return instance.GetDistanceMatrix();
    }


    public static void RunGreedySearch(float [,] distMatrix)
    {
        AntColony aco = new(settings, distMatrix);
        GreedySearch greedy = new(distMatrix);
        List<int> greedyPath = greedy.Run();
        Console.WriteLine($"Greedy: {aco.CalculateCost(greedyPath)}");
    }


    /// <summary>
    /// Runs {numAlgorithms} iterations of the algorithm with given settings, then calculates performance
    /// based on average and global best.
    /// </summary>
    /// <param name="settings">The settings to pass to the algorithm.</param>
    public static async Task RunAlgorithms(ACOSettings settings, float[,] distMatrix)
    {
        // https://stackoverflow.com/questions/34375696/executing-tasks-in-parallel
        // Run the algorithm the number of times specified.
        // -- This is done by awaiting {numAlgorithms} algorithm tasks.
        Task<float>[] algorithms = new Task<float>[numAlgorithms];
        for (int i = 0; i < numAlgorithms; i++)
        {
            AntColony aco;
            switch (settings.variation)
            {
                case ACOVariation.Standard:
                    aco = new AntColony(settings, distMatrix);
                    break;
                case ACOVariation.Elitist:
                    aco = new ElitistAntColony(settings, distMatrix);
                    break;
                case ACOVariation.MMAS:
                    aco = new MMASAntColony(settings, distMatrix);
                    break;
                default:
                    Console.WriteLine("Unsupported ACO variation.");
                    continue;
            }

            algorithms[i] = RunAlgorithm(aco);
        }

        // Get return value from each of the tasks. Calculate and print the average lowest cost.
        float[] everyAlgBest = await Task.WhenAll(algorithms);

        float avgAlgBest = everyAlgBest.Sum() / numAlgorithms;
        float allAlgBest = everyAlgBest.Min();

        Console.WriteLine($"Algorithm performance over {numAlgorithms} runs:\nGlobal Best: {allAlgBest}\tAverage Best: {avgAlgBest}\n");
    }


    /// <summary>
    /// Runs an algorithm asynchronously.
    /// </summary>
    /// <param name="aco">The Ant Colony algorithm to run, which must already be constructed.</param>
    /// <returns>The return value of the algorithm's Start function. (Its best path)</returns>
    public static async Task<float> RunAlgorithm(AntColony aco)
    {
        return await Task.Run(aco.Start);
    }
}
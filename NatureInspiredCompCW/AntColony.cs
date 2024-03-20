namespace ECM3412;


/// The ant colony optimisation algorithm implementation.
/// I used psuedocode from lectures and wikipedia:
/// https://en.wikipedia.org/wiki/Ant_colony_optimization_algorithms


/// <summary>
/// A container for all of the ACO settings.
/// Simplifies the constructor definition for <c>AntColony</c>.
/// </summary>
public struct ACOSettings
{
    public ACOVariation variation;

    public int startVertex;
    public int numAnts;
    public int requiredIterations;

    // Constraints which affect exponents in the probability calculation
    // for an ant to traverse an edge.
    public float probPheroExponent; // Alpha
    public float probDesirabilityExponent; // Beta

    public ACOHeuristic heuristic;
    public float pheroImportance; // Q parameter if the heuristic used is Q/d.

    // The scale factor p, each pheromone is multiplied by (1-p) during evaporation.
    // Warning: Setting this too high causes a caught error - the pheromone gets so small
    // that it ends up becoming 0.
    public float pheroEvapRate;

    // Define the range in which pheromone is Clamped.
    public float pheroMin;
    public float pheroMax;
    
    // Maximum number of fitness evaluations without a new global best before "stagnation" has occurred.
    // (Only in MMAS) When stagnation occurs, all edges are initialised with pheroMax pheromone.
    public int stagnantCountMax;

    // Whether to suppress prints to the console for this colony.
    public bool suppressPrints;
}


/// <summary>
/// Implements the Ant-Colony Optimisation algorithm.
/// 
/// Implements the data as follows:
/// 
/// Vertex    - An integer, also an index for matrices.
/// Edge      - Adjacency/distance matrix, D; e = D[v1,v2]. If e == -1, there is no
///             adjacency. Otherwise, e == "cost of edge".
/// Pheromone - A Pheromone matrix, P; p = P[v1,v2]. p is equal to the
///             amount of pheromone on the edge v1, v2.
/// </summary>
public class AntColony
{
    // --- Readonly Variables ---
    // (These cannot be assigned to after constructor, but lists and matrices can still have their contents edited)
    public readonly Random rand;

    private readonly int m_startVertex;
    protected readonly int m_numAnts;
    private readonly float m_pheroEvapRate; // (e) Rate of pheromone evaporation.
    private readonly float m_pheroImportance; // (Q) Pheromones are multiplied by Q/cost during pheromone update.
    private readonly int m_requiredIterations;
    private readonly Func<float, float> m_heuristic;

    // Data matrices - these are separate as cost/pheromone are nearly always accessed separately
    // These matrices can be traversed both ways, and due to random pheromone both ways may have different costs.
    public readonly int numVertices; // Length for matrix dimensions
    public readonly float[,] distMatrix; // The distance matrix (D) - not visible to ants
    public readonly float[,] pheroMatrix; // Matrix storing pheromone intensities on each edge.

    public readonly float probPheroExponent; // Alpha in the next-edge-probability equation
    public readonly float probDesirabilityExponent; // Beta in the next-edge-probability equation

    protected readonly Ant[] m_ants;
    protected readonly bool m_suppressPrints;

    // --- Normal Variables ---
    // These variables need to be changed over the course of the program, or are changed in a subclass.
    protected List<int> m_currentFittest;
    private float m_currentFittestFitness;

    protected float m_pheroMin;
    protected float m_pheroMax;


    // ====== Helper Functions ======

    /// <summary>
    /// Abstracts running code for every index in a generic matrix.
    /// </summary>
    /// <param name="action">The generic action to apply for each index.</param>
    public void ForEachMatrixIndex(Action<int, int> action)
    {
        for (int i = 0; i < numVertices; i++)
        {
            for (int j = 0; j < numVertices; j++)
            {
                action(i, j);
            }
        }
    }


    /// <summary>
    /// Prints every value in a matrix.
    /// </summary>
    /// <param name="matrix">A 2D float array; i.e. a matrix to print.</param>
    public void PrintAllEntries(float[,] matrix)
    {
        ForEachMatrixIndex((int i, int j) => Console.WriteLine($"[{i},{j}]: {matrix[i, j]}"));
    }


    /// <summary>
    /// Calculates the cost (fitness) of a solution.
    /// </summary>
    /// <param name="path">A path of vertex indices.</param>
    /// <returns>The total (summed) cost of the path.</returns>
    public float CalculateCost(List<int> path)
    {
        float totalCost = 0;
        for (int i = 1; i < path.Count; i++)
        {
            totalCost += distMatrix[path[i-1], path[i]];
        }
        return totalCost;
    }


    /// <summary>
    /// Calculates and returns the average path cost from every ant.
    /// </summary>
    private float CalculateAverageCost()
    {
        float avg = 0;
        for (int i = 0; i < m_numAnts; i++)
        {
            avg += CalculateCost(m_ants[i].GetPath());
        }
        avg /= m_numAnts;
        return avg;
    }


    public AntColony(ACOSettings settings, float[,] distMat)
    {
        rand = new();

        m_startVertex = settings.startVertex;
        m_numAnts = settings.numAnts;
        m_pheroEvapRate = settings.pheroEvapRate;
        m_pheroImportance = settings.pheroImportance;

        distMatrix = distMat;
        numVertices = distMatrix.GetLength(0);
        pheroMatrix = new float[numVertices, numVertices];

        probPheroExponent = settings.probPheroExponent;
        probDesirabilityExponent = settings.probDesirabilityExponent;

        m_ants = new Ant[m_numAnts];
        m_currentFittest = new();
        m_currentFittestFitness = float.PositiveInfinity;

        m_requiredIterations = settings.requiredIterations;

        m_pheroMin = 10e-6f;
        m_pheroMax = 10e6f;

        m_suppressPrints = settings.suppressPrints;

        switch (settings.heuristic)
        {
            case ACOHeuristic.InverseDistance:
                m_heuristic = (float dist) => 1 / dist;
                break;
            case ACOHeuristic.QOverDistance:
                m_heuristic = (float dist) => settings.pheroImportance / dist;
                break;
            case ACOHeuristic.QSquaredOverDistance:
                m_heuristic = (float dist) => MathF.Pow(settings.pheroImportance, 2) / dist;
                break;
        }
    }


    // ====== Algorithm Procedures ======


    /// <summary>
    /// The entry point for the algorithm.
    /// </summary>
    /// <returns>The average path cost for the final pass.</returns>
    public float Start()
    {
        InitialisePheromone();

        // Construct all ants
        for (int i = 0; i < m_numAnts; i++)
        {
            m_ants[i] = new(this, i, m_startVertex, m_heuristic);
        }

        return Run();
    }


    /// <summary>
    /// Sets the initial value for each pheromone matrix entry.
    /// </summary>
    protected virtual void InitialisePheromone()
    {
        // Set each pheromone intensity to a random value
        SetAllPheromone(rand.NextSingle);
    }


    /// <summary>
    /// Sets the pheromone amount for each edge based on a function.
    /// </summary>
    protected void SetAllPheromone(Func<float> func)
    {
        ForEachMatrixIndex((int i, int j) => {
            if (distMatrix[i, j] == -1) return;
            pheroMatrix[i, j] = func();
        });
    }


    /// <summary>
    /// Runs the algorithm. Iterates over every ant for each pass.
    /// </summary>
    /// <returns>The average cost for the final pass.</returns>
    private float Run()
    {
        // Iteration number. Incremented after every ant path completion.
        for (int i = 0; i < m_requiredIterations; i++)
        {
            // Ant traversal - generate a path for each ant in the population.
            for (int a = 0; a < m_numAnts; a++)
            {
                // Get the ant to find a new path - this function is recursive and will not terminate
                // until it has found a new path.
                m_ants[a].Traverse();

                // Determine whether the path is the best one so far.
                // Update the current best path, if so.
                List<int> foundPath = m_ants[a].GetPath();
                float foundPathCost = CalculateCost(foundPath);
                CheckIfFittest(foundPath, foundPathCost);
            }

            // Update pheromone
            UpdatePheromone();

            if (m_currentFittest.Count != 0 && !m_suppressPrints)
                Console.WriteLine($"Best fitness of iteration {i}: {m_currentFittestFitness}");

            // Copy the current fittest list before resetting
            CopyFittest();

            // Reset paths for the next iteration
            for (int a = 0; a < m_numAnts; a++)
            {
                m_ants[a].ResetPath();
            }
        }

        // Return the fittest path and its cost
        if (!m_suppressPrints)
        {
            Console.WriteLine($"Fittest path found:");
            for (int i = 0; i < m_currentFittest.Count; i++)
            {
                Console.WriteLine($"\tVertex {m_currentFittest[i]}");
            }
        }
        return m_currentFittestFitness;
    }


    /// <summary>
    /// Checks if the provided path is fitter than the current best path, based on the cost parameter.
    /// If it is, update the current best path and current best cost.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="cost">The cost of the path to compare.</param>
    /// <returns><c>true</c> if it is the new best path, <c>false</c> otherwise.</returns>
    protected virtual bool CheckIfFittest(List<int> path, float cost)
    {
        if (cost < m_currentFittestFitness)
        {
            // For performance reasons, set the current fittest to a
            // reference to the fittest path. Need to copy the path later
            // as it will get reset.
            m_currentFittest = path;
            m_currentFittestFitness = cost;
            return true;
        }
        return false;
    }


    /// <summary>
    /// This method simply copies the current fittest path into a new list, because currently it is just
    /// a reference to an ant's path. And so it would get reset along with the ant's path.
    /// Creating a new() object whenever the current fittest path changes would be wasteful too, j=
    /// </summary>
    protected virtual void CopyFittest()
    {
        m_currentFittest = new(m_currentFittest);
    }


    /// <summary>
    /// Handles each ant's pheromone additions, and the evaporation of pheromone
    /// at the end of each pass.
    /// 
    /// Follows the equation:  pheromone = pheromone * (1-p) + sum_k(ant_k_pheromone)
    /// Evaporate pheromone:   pheromone = pheromone * (1-p)
    /// Add pheromone loop:    pheromone = sum_k(ant_k_pheromone)
    /// </summary>
    protected virtual void UpdatePheromone()
    {
        EvaporatePheromone();
        foreach (Ant ant in m_ants)
        {
            AddPheromone(ant.GetPath());
        }
    }


    /// <summary>
    /// Handles adding pheromone for a single ant's path.
    /// Adds the same amount to each edge; this amount is inversely proportional
    /// to the cost of the whole path.
    /// </summary>
    /// <param name="path">The path of the ant to add pheromone for.</param>
    protected void AddPheromone(List<int> path)
    {
        float cost = CalculateCost(path);
        if (cost <= 0) // No pheromone to add
            return;

        for (int i = 1; i < path.Count; i++)
        {
            pheroMatrix[path[i - 1], path[i]] += m_pheroImportance / cost;
        }
    }


    /// <summary>
    /// Reduces the amount of pheromone on each edge, by a multiplicative scale factor
    /// of (1-"evaporation rate")
    /// </summary>
    protected void EvaporatePheromone()
    {
        // Evaporates every value in the pheromone matrix. Also ensures each pheromone > 0.
        // Pheromone needs to be more than 0, or the probability of selection will be 0.
        ForEachMatrixIndex((int i, int j) =>
        {
            pheroMatrix[i, j] = float.Clamp(pheroMatrix[i, j] * (1 - m_pheroEvapRate), m_pheroMin, m_pheroMax);
        });
    }
}


/// <summary>
/// Implements an elitist adaptation of ACO.
/// 
/// In addition to each ant adding pheromone each iteration,
/// the global best path continuously adds pheromone to itself.
/// </summary>
public class ElitistAntColony : AntColony
{
    public ElitistAntColony(ACOSettings settings, float[,] distMatrix) : base(settings, distMatrix) { }


    protected override void UpdatePheromone()
    {
        // Update pheromone normally, then add pheromone for the global best path only.
        base.UpdatePheromone();
        if (m_currentFittest.Count > 0)
        {
            AddPheromone(m_currentFittest);
        }
    }
}


public class MMASAntColony : AntColony
{
    // Number of iterations required for "stagnation" to be determined to have occurred.
    private readonly int m_maxStagnantIterations;
    private int m_stagnantCount;

    private List<int> m_iterationBest;
    private float m_iterationBestCost;


    public MMASAntColony(ACOSettings settings, float[,] distMatrix) : base(settings, distMatrix)
    {
        m_iterationBest = new List<int>();
        m_iterationBestCost = float.PositiveInfinity;

        m_pheroMin = settings.pheroMin;
        m_pheroMax = settings.pheroMax;

        m_stagnantCount = 0;
        m_maxStagnantIterations = settings.stagnantCountMax;
    }


    protected override void InitialisePheromone()
    {
        // Set all pheromones to the maximum value
        SetAllPheromone(() => m_pheroMax);
    }


    protected override bool CheckIfFittest(List<int> path, float cost)
    {
        // Calculate whether path is iteration best
        if (cost < m_iterationBestCost)
        {
            m_iterationBest = path;
            m_iterationBestCost = cost;
        }

        bool isFittest = base.CheckIfFittest(path, cost);

        // If there has been no fittest for a while, the search space is stagnant,
        // reinitialise pheromone values to their maximum value.
        if (isFittest)
            m_stagnantCount = 0;
        else
        {
            m_stagnantCount++;
            if (m_stagnantCount >= m_maxStagnantIterations)
            {
                if (!m_suppressPrints) Console.WriteLine("Stagnant!");

                InitialisePheromone();
                m_stagnantCount = 0;
            }
        }

        return isFittest;
    }


    protected override void CopyFittest()
    {
        base.CopyFittest();

        // Additionally, copy the iteration best
        m_iterationBest = new(m_iterationBest);
    }


    protected override void UpdatePheromone()
    {
        // Only add pheromone for the global best path and iteration best path
        if (m_currentFittest.Count > 0)
            AddPheromone(m_currentFittest);
        if (m_iterationBest.Count > 0)
            AddPheromone(m_iterationBest);
    }
}
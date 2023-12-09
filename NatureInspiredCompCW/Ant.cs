namespace ECM3412;


/// <summary>
/// Class containing all of the logic for each ant.
/// </summary>
public class Ant
{
    private readonly int m_id; // Used for debugging purposes
    private readonly int m_startVertex; // The first and last vertex the ant goes to.

    private AntColony m_colony; // Reference to the colony which instantiated this ant.
    private List<int> m_currentPath; // (C) The current path of the ant.
    // Resets when the ant finishes its traversal.

    private int m_recursionDepth = 0; // Ensures ants behave properly, and helps locate bugs.


    //public Tuple<int, int> GetLastTraversedEdge()
    //{
    //    if (m_currentPath.Count == 1) // Return a loop path (so cost will be -1)
    //        return new(m_currentPath[0], m_currentPath[0]);
    //    return new(m_currentPath[^2], m_currentPath[^1]);
    //}


    public List<int> GetPath()
    {
        return m_currentPath;
    }


    public Ant(AntColony colony, int id, int startVertex)
    {
        m_id = id;
        m_colony = colony;
        m_startVertex = startVertex;
        m_currentPath = new() { m_startVertex };
    }


    /// <summary>
    /// A recursive function in which the ant finds a tour around all of the cities,
    /// based on pheromone and individual hop distance.
    /// </summary>
    /// <exception cref="StackOverflowException">Thrown when the ant surpasses the maximum recursion depth.</exception>
    public void Traverse()
    {
        //                                 (tau[i,j]^(alpha) * eta[i,j]^(beta))
        // From lectures: probability =    -----------------------------------
        //                              (sum_h(tau[i,h]^(alpha) * eta[i,h]^(beta))
        // Where sum_h sums over all non-visited vertices, h.
        // In the below comments, any reference to i or j corresponds to:
        // i = currentVertex
        // j = j in for loop below
        // alpha = "probability pheromone exponent"
        // beta = "probability desirability exponent"

        int currentVertex = m_currentPath[^1]; // i (Last entry in list)

        // pheroArr[j] = tau[i,j]^alpha (To save multiple exponentation operations)
        float[] pheroArr = new float[m_colony.numVertices];
        // desirabilityArr[j] = eta[i,j]^beta (To save multiple exponentation operations)
        float[] desirabilityArr = new float[m_colony.numVertices];
        // The denominator of the probability equation above.
        float sum = 0;

        // Fill out the pheromone and desirability arrays
        for (int j = 0; j < m_colony.numVertices; j++)
        {
            // Get the pheromone and distance for edge [i,j]
            float phero = m_colony.pheroMatrix[currentVertex, j]; // tau[i,j]
            float dist = m_colony.distMatrix[currentVertex, j]; // D[i,j]

            // If: Travelling an edge which goes nowhere
            // Or: Visiting a previously visited vertex
            if (currentVertex == j || m_currentPath.Contains(j))
            {
                // Set both values to 0, so there is no chance of selection p[i,j] = 0 / denom = 0
                pheroArr[j] = 0;
                desirabilityArr[j] = 0;
                continue;
            }

            // Fill in values for tau and eta, and add to the sum.
            pheroArr[j] = MathF.Pow(phero, m_colony.probPheroExponent);
            desirabilityArr[j] = MathF.Pow(1 / dist, m_colony.probDesirabilityExponent);
            sum += pheroArr[j] * desirabilityArr[j];
        }

        if (sum == 0 && (m_currentPath.Count < m_colony.numVertices))
        {
            throw new Exception("No valid vertices to go to next, likely due to multiple pheromone trails of 0 intensity." +
                "The assigned pheromone evaporation rate is too high.");
        }

        // Calculate probability for all edges, store in an array
        float[] nextVertexProb = new float[m_colony.numVertices];
        for (int i = 0; i < m_colony.numVertices; i++)
        {
            nextVertexProb[i] = pheroArr[i] * desirabilityArr[i] / sum;
        }


        // Randomly select one of the edges based on their probability.
        // -- Random value in the range [0, 1)
        float randomValue = m_colony.rand.NextSingle();

        // -- Add the probabilities to a sum until that sum is greater than the random value.
        // -- The vertex whose probability was just added is then selected.
        // -- i.e. Assigned to selectedVertex.
        int selectedVertex = -1;
        float probSum = 0;
        for (int i = 0; i < m_colony.numVertices; i++)
        {
            probSum += nextVertexProb[i];

            // If the sum of probabilities so far >= the random value selected
            if (probSum >= randomValue)
            {
                selectedVertex = i;
                break;
            }
        }

        // -- If no vertex was selected, then all vertices had a prob. of 0.
        // -- Assuming that the initial distance matrix is properly initialised,
        // -- this tour has traversed every vertex exactly once.
        if (selectedVertex == -1)
        {
            // Sometimes, due to floating point rounding, probability sum never surpasses
            // the random value. This check guarantees that the last valid vertex in the
            // probability list is picked, if the probability sum is greater than 0 (i.e.
            // there are still vertices left to traverse, but they were erroneously not picked)

            // E.g. A case that occurred before adding this condition:
            // "random value" == 0.99999994
            // "probability sum" == 0.9999999 (After summing all probabilities)
            // Therefore no vertex was picked, and so the program assumed the ant had visited
            // every city once.
            if (probSum > 0)
            {
                for (int v = m_colony.numVertices - 1; v >= 0; v--)
                {
                    if (nextVertexProb[v] > 0)
                    {
                        selectedVertex = v;

                        // Break the loop and exit this if-statement
                        goto add_vertex;
                    }
                }
            }


            // --- Finish the tour with the start vertex - the tour has to travel back to the start.
            m_currentPath.Add(m_startVertex);

            // --- Display the path and cost for debugging purposes
            //Console.WriteLine($"Ant {m_id} finished its path:");
            //for (int i = 0; i < m_currentPath.Count; i++)
            //{
            //    Console.WriteLine($"\tVertex {m_currentPath[i]}");
            //}
            //Console.WriteLine($"Ant {m_id} path fitness: {m_colony.CalculateCost(m_currentPath)}");

            return;
        }

        // -- Otherwise, there are vertices still left to add to the path.
        // --- Add the selected vertex to the list.
        add_vertex:
        m_currentPath.Add(selectedVertex);

        // --- An ant should never go further in recursion depth than the number of vertices.
        if (m_recursionDepth > m_colony.numVertices)
        {
            throw new StackOverflowException("This ant has reached recursion depth.");
        }

        // --- The path was not completed this call; recurse.
        m_recursionDepth++;
        Traverse();
        m_recursionDepth--;
    }


    /// <summary>
    /// Clears the path of an ant, then adds the starting vertex back.
    /// </summary>
    public void ResetPath()
    {
        m_currentPath.Clear();
        m_currentPath.Add(m_startVertex);
    }
}
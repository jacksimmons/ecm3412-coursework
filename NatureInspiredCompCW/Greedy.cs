namespace ECM3412;


/// <summary>
/// A simple greedy search I used as a basis for the performance of the Ant Colony optimisation.
/// </summary>
class GreedySearch
{
    private float[,] distMatrix;
    private List<int> path;
    private int length;


    public GreedySearch(float[,] distMatrix)
    {
        this.distMatrix = distMatrix;
        length = distMatrix.GetLength(0);
        path = new List<int>();
    }


    public List<int> Run()
    {
        path.Add(0);
        while (path.Count < length)
        {
            int cheapestNext = -1;
            float cheapest = float.PositiveInfinity;
            for (int i = 0; i < length; i++)
            {
                float cost = distMatrix[path[^1], i];
                if (cost < cheapest && cost >= 0 && !path.Contains(i))
                {
                    cheapestNext = i;
                    cheapest = distMatrix[path[^1], i];
                }
            }

            if (cheapestNext != -1)
            {
                path.Add(cheapestNext);
            }
        }

        path.Add(0);
        return path;
    }
}

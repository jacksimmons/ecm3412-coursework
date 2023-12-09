using System.Xml.Serialization;

namespace ECM3412;

/// This file is for deserializing XML into C# classes.
/// These classes are then further decomposed - all of the data can be simplified into a distance matrix.
/// TSPInstance.GetDistanceMatrix completes this decomposition.

// https://learn.microsoft.com/en-us/dotnet/standard/serialization/controlling-xml-serialization-using-attributes
// https://stackoverflow.com/questions/14783406/deserializing-nested-xml-into-c-sharp-objects
/// <summary>
/// Class for deserialized (from XML) TSP problems of the format used in this coursework.
/// </summary>
[XmlRoot("travellingSalesmanProblemInstance")]
public class TSPInstance
{
    [XmlElement("name")]
    public string m_name = "";
    [XmlElement("source")]
    public string m_source = "";
    [XmlElement("description")]
    public string m_description = "";
    [XmlElement("doublePrecision")]
    public int m_doublePrecision = 0;
    [XmlElement("ignoredDigits")]
    public int m_ignoredDigits = 0;
    [XmlArray("graph"), XmlArrayItem("vertex")]
    public Vertex[] m_graph = Array.Empty<Vertex>();


    public void Print()
    {
        Console.WriteLine($"Using TSP problem instance {m_name} from {m_source}: {m_description}.");
        for (int v = 0; v < m_graph.Length; v++)
        {
            Console.WriteLine($"Vertex {v}:");
            m_graph[v].Print();
        }
    }


    /// <summary>
    /// Extracts and returns a distance matrix from the vertex edges.
    /// </summary>
    public float[,] GetDistanceMatrix()
    {
        int numVertices = m_graph.Length;
        float[,] distMatrix = new float[numVertices, numVertices];

        // Set default value to -1
        for (int i = 0; i < numVertices; i++)
        {
            for (int j = 0; j < numVertices; j++)
            {
                distMatrix[i, j] = -1;
            }
        }

        // Fill in all applicable edge indices
        for (int v = 0; v < m_graph.Length; v++)
        {
            for (int e = 0; e < m_graph[v].edges.Length; e++)
            {
                Edge edge = m_graph[v].edges[e];
                distMatrix[v, edge.otherVertex] = edge.cost;
            }
        }

        return distMatrix;
    }
}


[XmlRoot("vertex")]
public class Vertex
{
    [XmlElement("edge")]
    public Edge[] edges = Array.Empty<Edge>();


    public void Print()
    {
        for (int e = 0; e < edges.Length; e++)
        {
            Console.WriteLine($"\tEdge {e}:");
            edges[e].Print();
        }
    }
}


[XmlRoot("edge")]
public class Edge
{
    [XmlAttribute("cost")]
    public float cost = -1;
    [XmlText()]
    public int otherVertex = 0;


    public void Print()
    {
        Console.WriteLine($"\t\tCost: {cost}, Other: {otherVertex}");
    }
}

using UnityEngine;
using System.Collections.Generic;

public class Grid : MonoBehaviour
{
    public int numCellsX = 50;         // Number of cells in the x-direction
    public int numCellsZ = 50;         // Number of cells in the z-direction
    public int numCellsY = 10;         // Number of cells in the y-direction (vertical)
    [Range(0.1f, 1f)]
    public float cellSize = 0.2f;      // Size of each cell in world units
    public float cellSpacing = 0.2f;   // Spacing between cells in world units
    public LayerMask obstacleMask;     // Mask to identify obstacles

    [SerializeField] bool _drawNodes;
    [SerializeField] bool _editorNodes;

    Node[,,] nodes;
    Vector3 gridOrigin; // Fixed grid origin point

    void Start()
    {
        CreateGrid();
    }

    void CreateGrid()
    {
        nodes = new Node[numCellsX, numCellsZ, numCellsY];
        gridOrigin = transform.position; // Store the grid origin point

        for (int x = 0; x < numCellsX; x++)
        {
            for (int z = 0; z < numCellsZ; z++)
            {
                for (int y = 0; y < numCellsY; y++)
                {
                    // Calculate the world position of each node relative to the grid origin
                    Vector3 worldPoint = gridOrigin + Vector3.right * (x * cellSpacing) + Vector3.forward * (z * cellSpacing) + Vector3.up * (y * cellSpacing);

                    // Check if the node is walkable
                    bool walkable = !Physics.CheckSphere(worldPoint, cellSpacing / 2, obstacleMask);

                    // Create and assign the node
                    nodes[x, z, y] = new Node(walkable, worldPoint, x, z, y);
                }
            }
        }
    }

    public Node GetNodeFromWorldPoint(Vector3 worldPosition)
    {
        // Calculate the grid cell indices based on the fixed grid origin
        float percentX = (worldPosition.x - gridOrigin.x) / (numCellsX * cellSpacing);
        float percentZ = (worldPosition.z - gridOrigin.z) / (numCellsZ * cellSpacing);
        float percentY = (worldPosition.y - gridOrigin.y) / (numCellsY * cellSpacing);
        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);
        percentY = Mathf.Clamp01(percentY);
        int x = Mathf.RoundToInt((numCellsX - 1) * percentX);
        int z = Mathf.RoundToInt((numCellsZ - 1) * percentZ);
        int y = Mathf.RoundToInt((numCellsY - 1) * percentY);
        return nodes[x, z, y];
    }

    public Node[] GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && z == 0 && y == 0) continue;

                    int checkX = node.gridX + x;
                    int checkZ = node.gridZ + z;
                    int checkY = node.gridY + y;

                    if (checkX >= 0 && checkX < numCellsX && checkZ >= 0 && checkZ < numCellsZ && checkY >= 0 && checkY < numCellsY)
                    {
                        neighbours.Add(nodes[checkX, checkZ, checkY]);
                    }
                }
            }
        }
        return neighbours.ToArray();
    }

    void OnDrawGizmos()
    {
        if (_drawNodes)
        {
            if (_editorNodes)
            {
                CreateGrid();
            }

            if (nodes != null)
            {
                foreach (Node node in nodes)
                {
                    Gizmos.color = node.walkable ? Color.white : Color.red;
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (cellSize - .1f));
                }
            }
        }
    }
}
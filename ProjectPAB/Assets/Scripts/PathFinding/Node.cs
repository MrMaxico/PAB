using UnityEngine;

public class Node
{
    public bool walkable; // True if the node is walkable
    public Vector3 worldPosition; // World position of the node
    public int gridX, gridZ, gridY; // Node's position in the grid array
    public float gCost, hCost; // Costs for pathfinding
    public Node parent; // Parent node for path reconstruction

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridZ, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridY = gridY;
    }

    public float fCost { get { return gCost + hCost; } } // Total cost for pathfinding
}
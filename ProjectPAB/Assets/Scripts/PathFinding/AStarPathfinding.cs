using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinding : MonoBehaviour
{
    public Grid grid;

    public List<Node> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        Node startNode = grid.GetNodeFromWorldPoint(startWorldPosition);
        Node endNode = grid.GetNodeFromWorldPoint(endWorldPosition);

        if (startNode == null || endNode == null || !startNode.walkable || !endNode.walkable)
        {
            Debug.LogError("Start or End Node is null or not walkable.");
            return null;
        }

        List<Node> openSet = new List<Node> { startNode };
        HashSet<Node> closedSet = new HashSet<Node>();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, endNode);

        while (openSet.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openSet);

            if (currentNode == endNode)
            {
                Debug.Log("Path found. Reconstructing...");

                return ReconstructPath(cameFrom, endNode);
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (Node neighbour in grid.GetNeighbours(currentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                float tentativeGCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (tentativeGCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = tentativeGCost;
                    neighbour.hCost = GetDistance(neighbour, endNode);
                    cameFrom[neighbour] = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }

        Debug.Log("Pathfinding failed: No path found.");
        return null;
    }

    Node GetLowestFCostNode(List<Node> nodes)
    {
        Node lowest = nodes[0];
        foreach (Node node in nodes)
        {
            if (node.fCost < lowest.fCost || node.fCost == lowest.fCost && node.hCost < lowest.hCost)
            {
                lowest = node;
            }
        }
        return lowest;
    }

    float GetDistance(Node nodeA, Node nodeB)
    {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        int distZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);

        // Use the Euclidean distance for 3D space
        return Mathf.Sqrt(distX * distX + distY * distY + distZ * distZ);
    }

    List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node currentNode)
    {
        List<Node> path = new List<Node> { currentNode };

        while (cameFrom.ContainsKey(currentNode))
        {
            currentNode = cameFrom[currentNode];
            path.Add(currentNode);
        }

        path.Reverse();

        Debug.Log("Reconstructed Path Length: " + path.Count);
        return path;
    }
}
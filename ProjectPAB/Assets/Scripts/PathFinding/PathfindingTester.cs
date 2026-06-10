using UnityEngine;

public class PathfindingTester : MonoBehaviour
{
    public AStarPathfinding pathfinding;
    public Transform startPoint, endPoint;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("DrawPath");
            Vector3 startWorldPosition = startPoint.position;
            Vector3 endWorldPosition = endPoint.position;
            var path = pathfinding.FindPath(startWorldPosition, endWorldPosition);

            if (path != null && path.Count > 1)
            {
                Debug.Log("Has path with length: " + path.Count);
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector3 currentPos = path[i].worldPosition;
                    Vector3 nextPos = path[i + 1].worldPosition;
                    Debug.DrawLine(currentPos, nextPos, Color.red, Mathf.Infinity);
                }
            }
            else
            {
                Debug.Log("No path found or path is too short.");
            }
        }
    }
}
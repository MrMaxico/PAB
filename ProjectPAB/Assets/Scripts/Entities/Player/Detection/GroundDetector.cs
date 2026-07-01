using UnityEngine;

namespace Entities.Player.Detection
{
    public class GroundDetector : DownwardDetector
    {
        public LayerMask GroundLayer => _detectionLayer;
    }
}
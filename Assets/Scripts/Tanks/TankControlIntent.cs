using UnityEngine;

namespace NetworkTankArena.Tanks
{
    /// <summary>Device-independent requests for one gameplay simulation step.</summary>
    public readonly struct TankControlIntent
    {
        public float Throttle { get; }
        public float Turn { get; }
        public Vector3 AimWorldPoint { get; }
        public bool HasAimPoint { get; }

        public TankControlIntent(float throttle, float turn, Vector3 aimWorldPoint, bool hasAimPoint)
        {
            Throttle = throttle;
            Turn = turn;
            AimWorldPoint = aimWorldPoint;
            HasAimPoint = hasAimPoint;
        }
    }
}

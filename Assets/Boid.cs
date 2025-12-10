using Managers;
using UnityEngine;
using Random = UnityEngine.Random;

public class Boid : MonoBehaviour {
    private static SimulationSettings Settings => SimulationManager.Instance.settings;
    [HideInInspector]
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;

    public bool showVelocity;
    public bool showAcceleration;
    public bool showFov;

    public bool showFib;
    // [HideInInspector]
    // public Vector3 alignmentVector = Vector3.zero;
    // [HideInInspector]
    // public Vector3 cohesionVector = Vector3.zero;
    // [HideInInspector]
    // public Vector3 separationVector = Vector3.zero;

    public int sample = 50;

    private float _hitDistance;
    private const float CollisionRadius = 1f;

    private const float GoldenRatio = 1.61803398875f;
    
    private void Update() {
        position = transform.position;
        
        // acceleration = Vector3.zero;
        // acceleration += alignmentVector;
        // acceleration += cohesionVector;
        // acceleration += separationVector;

        if (IsOnCollision) {
            _hitDistance = Settings.avoidanceRadius;
            var avoidance = AvoidObstacle();
            acceleration += avoidance * (Settings.avoidanceWeight * (Settings.avoidanceRadius / (_hitDistance * _hitDistance)));
        }

        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, Settings.maxSpeed);
        transform.position += velocity * Time.deltaTime;
        
        transform.forward = velocity.normalized;
    }

    private bool IsOnCollision
        => Physics.SphereCast(position, CollisionRadius, transform.forward, out _, Settings.avoidanceRadius);


    private Vector3 AvoidObstacle() {
        var forward = transform.forward;
        var pointsCount = sample;
        for (var i = 0; i < pointsCount; i++) {
            var point = FibSpherePoint(i, GoldenRatio, pointsCount);
            var dir = transform.TransformDirection(point);
            var ray = new Ray(position, dir);
            if (!Physics.SphereCast(ray, CollisionRadius, out var min, Settings.avoidanceRadius)) {
                return dir;
            }
            if (min.distance < _hitDistance) {
                _hitDistance = min.distance;
            }
        }
        return forward;
    }

    private static Vector3 FibSpherePoint(int pointIndex, float turnFraction, float pointsCount) {
        var phi = 2 * Mathf.PI * (pointIndex / turnFraction % 1);
        var theta = Mathf.Acos(1 - 2 * pointIndex / pointsCount);
            
        var x = Mathf.Cos(phi) * Mathf.Sin(theta);
        var y = Mathf.Sin(phi) * Mathf.Sin(theta);
        var z = Mathf.Cos(theta);
        return new Vector3(x, y, z);
    }


    private void OnDrawGizmosSelected() {
        if (showAcceleration) {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, acceleration);
        }

        if (showVelocity) {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, velocity);
        }

        if (showFib) {
            Gizmos.color = Color.blanchedAlmond;
            for (var i = 0; i < sample; i++) {
                var point = FibSpherePoint(i, GoldenRatio, sample);
                Gizmos.DrawSphere(position + point * 2f, .05f);
            }
            
        }

        if (showFov) {
            Gizmos.color = Color.yellow;
            GizmoHelpers.DrawFov(transform.position, Settings.neighbourDistance, 15, Settings.fieldOfView, transform.up, transform.forward);
            GizmoHelpers.DrawFov(transform.position, Settings.neighbourDistance, 15, Settings.fieldOfView, transform.right, transform.forward);
        }
    }
}

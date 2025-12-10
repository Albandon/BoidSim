using System.Linq;
using Managers;
using Strategies.Interfaces;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Strategies {
    public class CPUFlockingStrategy : IFlockingStrategy {
        private Boid[] _boids;
        private readonly Boid _boidPrefab = Settings.boidPrefab;
        private static SimulationSettings Settings => SimulationManager.Instance.settings;

        public void Initialize() {
            var settings = Settings;
            for (var i = 0; i < settings.boidCount; i++) {
                var pos = Random.insideUnitSphere * settings.spawnRadius;
                var transform = Object.FindFirstObjectByType<BoidManager>().transform;
                var boid = Object.Instantiate(_boidPrefab, pos, Quaternion.identity,transform);
                boid.velocity = Random.insideUnitSphere.normalized * settings.initialSpeed;
                boid.position = pos;
            }
            _boids = Object.FindObjectsByType<Boid>(FindObjectsSortMode.None);
        }
    
        public void Update(float deltaTime) {
            foreach (var boid in _boids) {
                var align = Vector3.zero;
                var cohere = Vector3.zero;
                var separation = Vector3.zero;
                var neighbourCount = 0;
            
                foreach (var other in _boids.Where(other => IsNeighbour(boid, other))) {
                    #if UNITY_EDITOR
                    if (Selection.activeGameObject == boid.gameObject) 
                    {
                        Debug.DrawLine(boid.position, other.position, Color.green);
                    }
                    #endif
                    align += other.velocity;
                    cohere += other.position;
                    
                    var diff = boid.position - other.position;
                    var sqrDistance = diff.sqrMagnitude;
                
                    // separation += diff * (invDist * (Settings.separationRadius / sqrDistance));
                    separation += diff * (Settings.separationRadius / sqrDistance);
                    neighbourCount++;
                }

                if (neighbourCount > 0) {
                    align /= neighbourCount;
                    align = align.normalized * Settings.maxSpeed;
                    align -= boid.velocity;
                
                    cohere /= neighbourCount;
                    cohere -= boid.position;
                    cohere = cohere.normalized * Settings.maxSpeed;
                    cohere -= boid.velocity;
                
                    separation /= neighbourCount;
                }

                var accel = Vector3.zero;
                accel += align * Settings.alignmentWeight +
                         cohere * Settings.cohesionWeight +
                         separation * Settings.separationWeight;

                boid.acceleration = accel;
            }
        }

        public void Dispose() { }

        private static bool IsNeighbour(Boid boid, Boid other) {
            if (other == boid) return false;
        
            var diff = other.position - boid.position;
            var sqrDist = diff.sqrMagnitude;
            if ( sqrDist > Settings.neighbourDistance * Settings.neighbourDistance) return false;
        
            var dot = Vector3.Dot(boid.transform.forward, diff);
            var cosAngle = dot / Mathf.Sqrt(sqrDist);
            
            return cosAngle >= Mathf.Cos(Settings.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
    }
}
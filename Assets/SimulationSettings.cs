using System;
using DTO;
using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "Scriptable Objects/Settings")]
public class SimulationSettings : ScriptableObject {
    
    public event Action<DynamicParameters> OnDynamicParamsChanged;
    
    public int boidCount = 100;
    public float spawnRadius = 10f;
    public int  gridResolution = 54;
    [Header("Boid Settings")]
    public float initialSpeed = 2f;
    public float separationRadius = 1f;
    public float maxSpeed = 5f;
    public float neighbourDistance = 3f;
    public float avoidanceRadius = 4f;
        
    [Range(0f,360f)]
    public float fieldOfView = 120f;

    [Header("Rule Weights")]
    [Range(0f, 1f)]
    public float separationWeight = 1.0f;
    [Range(0f, 1f)]
    public float alignmentWeight = 1.0f;
    [Range(0f, 1f)]
    public float cohesionWeight = 1.0f;
    [Range(0f, 1f)]
    public float avoidanceWeight = 1.0f;
    
    [Header("GPU Shaders")]
    public ComputeShader sortShader;
    public ComputeShader cellRangeShader;
    public ComputeShader flockingShader;
    public ComputeShader updateShader;

    [Header("Instancing")]
    public Mesh mesh;
    public Material material;
    public Boid boidPrefab;

    private void OnValidate() {
        OnDynamicParamsChanged?.Invoke(new DynamicParameters {
            AlignmentWeight = alignmentWeight,
            CohesionWeight = cohesionWeight,
            SeparationWeight = separationWeight,
            SeparationRadius = separationRadius,
            AvoidanceRadius = avoidanceRadius,
            AvoidanceWeight = avoidanceWeight,
        });
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class InstancingTest : MonoBehaviour {
    public Mesh mesh;
    public Material material;
    
    public Vector3 position;
    public Vector3 velocity;
    public int count => 1;
    
    ComputeBuffer positionBuffer;
    ComputeBuffer velocityBuffer;
    ComputeBuffer argsBuffer;
    
    void Start()
    {
        positionBuffer = new ComputeBuffer(count, sizeof(float) * 3);
        velocityBuffer = new ComputeBuffer(count, sizeof(float) * 3);
        var args = new uint[5];
        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)count;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;
        
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    private void Update() {
        var pos = new [] { position };
        var vel = new [] { velocity };
        positionBuffer.SetData(pos);
        velocityBuffer.SetData(vel);
        
        material.SetBuffer("positions", positionBuffer);
        material.SetBuffer("velocities", velocityBuffer);
        
        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            material,
            new Bounds(Vector3.zero, Vector3.one * 1000f),
            argsBuffer
        );
    }

    private void OnDestroy() {
        positionBuffer?.Release();
        velocityBuffer?.Release();
        argsBuffer?.Release();
    }
}

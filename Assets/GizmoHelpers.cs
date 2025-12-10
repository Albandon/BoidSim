using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class GizmoHelpers {
    public static void DrawFov(Vector3 center, float radius, int segments, float fov, Vector3 axis, Vector3 forward) {
        
        var angleStep = fov / segments;
        var halfFov = fov * 0.5f;
        var points = new List<Vector3>();

        for (var i = 0; i <= segments; i++) {
            var point = Quaternion.AngleAxis(- halfFov + i * angleStep, axis) * forward;
            points.Add(center + point * radius);

        }
        points.Add(center);
        Gizmos.DrawLineStrip(points.ToArray(), true);
    }
}
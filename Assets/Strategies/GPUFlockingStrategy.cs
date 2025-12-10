using System.Linq;
using DTO;
using Managers;
using Strategies.Interfaces;
using UnityEngine;

namespace Strategies {
    public class GPUFlockingStrategy : IFlockingStrategy {
        #region CachedPropertyIDs

        private static readonly int BoidCount = Shader.PropertyToID("boid_count");
        private static readonly int TotalCells = Shader.PropertyToID("total_cells");
        private static readonly int CellIds = Shader.PropertyToID("cell_ids");
        private static readonly int CellStarts = Shader.PropertyToID("cell_starts");
        private static readonly int CellEnds = Shader.PropertyToID("cell_ends");
        private static readonly int NeighbourDistance = Shader.PropertyToID("neighbour_distance");
        private static readonly int GridResolution = Shader.PropertyToID("grid_resolution");
        private static readonly int CellSize = Shader.PropertyToID("cell_size");
        private static readonly int MinBound = Shader.PropertyToID("min_bound");
        private static readonly int MaxBound = Shader.PropertyToID("max_bound");
        private static readonly int SeparationRadius = Shader.PropertyToID("separation_radius");
        private static readonly int AvoidanceRadius = Shader.PropertyToID("avoidance_radius");
        private static readonly int Positions = Shader.PropertyToID("positions");
        private static readonly int Velocities = Shader.PropertyToID("velocities");
        private static readonly int Accelerations = Shader.PropertyToID("accelerations");
        private static readonly int BoidIds = Shader.PropertyToID("boid_ids");
        private static readonly int MaxSpeed = Shader.PropertyToID("max_speed");
        private static readonly int CosHalfFOV = Shader.PropertyToID("cos_half_fov");
        private static readonly int SeparationWeight = Shader.PropertyToID("separation_weight");
        private static readonly int CohesionWeight = Shader.PropertyToID("cohesion_weight");
        private static readonly int AlignmentWeight = Shader.PropertyToID("alignment_weight");
        private static readonly int AvoidanceWeight = Shader.PropertyToID("avoidance_weight");
        private static readonly int DeltaTime = Shader.PropertyToID("delta_time");

        #endregion

        #region Data

        private static SimulationSettings Settings => SimulationManager.Instance.settings;
        private int _count;
        private int _totalCells;
        private ComputeShader _sortShader;
        private ComputeShader _cellRangeShader;
        private ComputeShader _flockingShader;
        private ComputeShader _updateShader;
        private ComputeShader _reorderShader;
        private BitonicSorter _sorter;

        private ComputeBuffer _positionBuffer,
            _velocityBuffer,
            _accelerationBuffer,
            _cellIdBuffer,
            _boidIdBuffer,
            _cellStartBuffer,
            _cellEndBuffer,
            _argsBuffer;

        private int _updateRangesKernel,
            _resetRangesKernel,
            _flockKernel,
            _updateKernel;
        #endregion

        public void Initialize() {
            Settings.OnDynamicParamsChanged += HandleDynamicDataChange;
            
            _sortShader = Settings.sortShader;
            _cellRangeShader = Settings.cellRangeShader;
            _flockingShader = Settings.flockingShader;
            _updateShader = Settings.updateShader;
            _count = Settings.boidCount;
            _sorter = new BitonicSorter(_sortShader);

            var cellDimension = Settings.gridResolution / (Settings.neighbourDistance * 2);
            _totalCells = (int)(cellDimension * cellDimension * cellDimension);
            AssignKernels();
            PrepareBuffers();
            AssignStaticData();
            AssignDynamicData();
        }


        public void Update(float deltaTime) {
            //pozycja + prędkość + id komórek
            _updateShader.SetFloat(DeltaTime, deltaTime);
            _updateShader.Dispatch(_updateKernel, Mathf.CeilToInt(_count / 256f), 1, 1);

            
            //sortowanie na podstawie id komórek
            _sorter.Dispatch(_cellIdBuffer, _boidIdBuffer, _count);

            //reset i aktualizacja zakresów
            _cellRangeShader.Dispatch(_resetRangesKernel, Mathf.CeilToInt(_totalCells / 256f), 1, 1);
            _cellRangeShader.Dispatch(_updateRangesKernel, Mathf.CeilToInt(_count / 256f), 1, 1);
            

            //przejście algorytmu głównego
            _flockingShader.Dispatch(_flockKernel, Mathf.CeilToInt(_count / 256f), 1, 1);
            
            Graphics.DrawMeshInstancedIndirect(
                Settings.mesh,
                0,
                Settings.material,
                new Bounds(Vector3.zero, Vector3.one * 1000f),
                _argsBuffer
            );
        }

        public void Dispose() {
            Settings.OnDynamicParamsChanged -= HandleDynamicDataChange;
            
            _accelerationBuffer?.Release();
            _velocityBuffer?.Release();
            _positionBuffer?.Release();
            _boidIdBuffer?.Release();
            _cellIdBuffer?.Release();
            _cellStartBuffer?.Release();
            _cellEndBuffer?.Release();
            _argsBuffer?.Release();
        }

        private void PrepareBuffers() {
            CreateBuffers();
            ResetCellBuffers();
            PrepareData();
        }

        private void PrepareData() {
            var boidCount = Settings.boidCount;
            var acceleration = new Vector3[boidCount];
            var positions = new Vector3[boidCount];
            var velocity = new Vector3[boidCount];
            var boidIds = new int[boidCount];

            for (var i = 0; i < boidCount; i++) {
                boidIds[i] = i;
                positions[i] = Random.insideUnitSphere * Settings.spawnRadius;
                velocity[i] = Random.insideUnitSphere.normalized * Settings.initialSpeed;
                acceleration[i] = Vector3.zero;
            }

            var args = new uint[5];
            args[0] = Settings.mesh.GetIndexCount(0);
            args[1] = (uint)_count;
            args[2] = Settings.mesh.GetIndexStart(0);
            args[3] = Settings.mesh.GetBaseVertex(0);
            args[4] = 0;

            _argsBuffer.SetData(args);
            _boidIdBuffer.SetData(boidIds);
            _positionBuffer.SetData(positions);
            _velocityBuffer.SetData(velocity);            
            _accelerationBuffer.SetData(acceleration);
        }

        private void ResetCellBuffers() {
            var resetData = Enumerable.Range(0, _totalCells)
                .Select(_ => -1)
                .ToArray();

            _cellStartBuffer.SetData(resetData);
            _cellEndBuffer.SetData(resetData);
        }

        private void CreateBuffers() {
            var argsLength = 5;
            _positionBuffer = new ComputeBuffer(_count, sizeof(float) * 3);
            _velocityBuffer = new ComputeBuffer(_count, sizeof(float) * 3);
            _accelerationBuffer = new ComputeBuffer(_count, sizeof(float) * 3);
            _cellIdBuffer = new ComputeBuffer(_count, sizeof(uint));
            _boidIdBuffer = new ComputeBuffer(_count, sizeof(uint));
            _cellStartBuffer = new ComputeBuffer(_totalCells, sizeof(int));
            _cellEndBuffer = new ComputeBuffer(_totalCells, sizeof(int));
            _argsBuffer = new ComputeBuffer(1, argsLength * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        private void AssignKernels() {
            _updateRangesKernel = _cellRangeShader.FindKernel("update_ranges");
            _resetRangesKernel = _cellRangeShader.FindKernel("reset_ranges");
            _flockKernel = _flockingShader.FindKernel("flock");
            _updateKernel = _updateShader.FindKernel("update");
        }

        private void AssignStaticData() {
            _cellRangeShader.SetInt(BoidCount, _count);
            _cellRangeShader.SetInt(TotalCells, _totalCells);
            
            _cellRangeShader.SetBuffer(_resetRangesKernel, CellIds, _cellIdBuffer);
            _cellRangeShader.SetBuffer(_resetRangesKernel, CellStarts, _cellStartBuffer);
            _cellRangeShader.SetBuffer(_resetRangesKernel, CellEnds, _cellEndBuffer);
            _cellRangeShader.SetBuffer(_updateRangesKernel, CellIds, _cellIdBuffer);
            _cellRangeShader.SetBuffer(_updateRangesKernel, CellStarts, _cellStartBuffer);
            _cellRangeShader.SetBuffer(_updateRangesKernel, CellEnds, _cellEndBuffer);
            
            _flockingShader.SetInt(BoidCount, _count);
            _flockingShader.SetFloat(NeighbourDistance, Settings.neighbourDistance);
            _flockingShader.SetFloat(GridResolution, Settings.gridResolution);
            _flockingShader.SetFloat(CellSize, Settings.neighbourDistance * 2);
            _flockingShader.SetFloat(MinBound, -Settings.gridResolution * 0.5f);
            _flockingShader.SetFloat(MaxBound, Settings.gridResolution * 0.5f);
            _flockingShader.SetFloat(SeparationRadius, Settings.separationRadius);
            _flockingShader.SetFloat(AvoidanceRadius, Settings.avoidanceRadius);

            _flockingShader.SetBuffer(_flockKernel, Positions, _positionBuffer);
            _flockingShader.SetBuffer(_flockKernel, Velocities, _velocityBuffer);
            _flockingShader.SetBuffer(_flockKernel, Accelerations, _accelerationBuffer);
            _flockingShader.SetBuffer(_flockKernel, BoidIds, _boidIdBuffer);
            _flockingShader.SetBuffer(_flockKernel, CellStarts, _cellStartBuffer);
            _flockingShader.SetBuffer(_flockKernel, CellEnds, _cellEndBuffer);

            _updateShader.SetInt(BoidCount, _count);
            _updateShader.SetFloat(GridResolution, Settings.gridResolution);
            _updateShader.SetFloat(CellSize, Settings.neighbourDistance * 2);
            _updateShader.SetFloat(MinBound, -Settings.gridResolution * 0.5f);
            _updateShader.SetFloat(MaxBound, Settings.gridResolution * 0.5f);

            _updateShader.SetBuffer(_updateKernel, Positions, _positionBuffer);
            _updateShader.SetBuffer(_updateKernel, Velocities, _velocityBuffer);
            _updateShader.SetBuffer(_updateKernel, Accelerations, _accelerationBuffer);
            _updateShader.SetBuffer(_updateKernel, CellIds, _cellIdBuffer);
            _updateShader.SetBuffer(_updateKernel, BoidIds, _boidIdBuffer);

            Settings.material.SetBuffer(Positions, _positionBuffer);
            Settings.material.SetBuffer(Velocities, _velocityBuffer);
        }

        private void AssignDynamicData() {
            _flockingShader.SetFloat(MaxSpeed, Settings.maxSpeed);
            _flockingShader.SetFloat(CosHalfFOV, Mathf.Cos(Settings.fieldOfView * .5f * Mathf.Deg2Rad));
            _flockingShader.SetFloat(SeparationWeight, Settings.separationWeight);
            _flockingShader.SetFloat(CohesionWeight, Settings.cohesionWeight);
            _flockingShader.SetFloat(AlignmentWeight, Settings.alignmentWeight);
            _flockingShader.SetFloat(AvoidanceWeight, Settings.avoidanceWeight);

            _updateShader.SetInt(BoidCount, _count);
            _updateShader.SetFloat(MaxSpeed, Settings.maxSpeed);
        }

        private void HandleDynamicDataChange(DynamicParameters data) {
            _flockingShader.SetFloat(SeparationWeight, data.SeparationWeight);
            _flockingShader.SetFloat(AlignmentWeight, data.AlignmentWeight);
            _flockingShader.SetFloat(CohesionWeight, data.CohesionWeight);
            _flockingShader.SetFloat(AvoidanceWeight, data.AvoidanceWeight);
            _flockingShader.SetFloat(SeparationRadius, data.SeparationRadius);
            _flockingShader.SetFloat(AvoidanceRadius, data.AvoidanceRadius);
        }
    }
}
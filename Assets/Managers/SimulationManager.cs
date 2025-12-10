using System.Collections;
using System.IO;
using Strategies;
using Strategies.Interfaces;
using UnityEngine;

namespace Managers {
    public enum SimulationMode
    {
        CPU,
        GPU
    }

    public class SimulationManager : MonoBehaviour
    {
        private ISimulationStrategy _strategy;

        [Header("Simulation Mode")]
        public SimulationMode currentMode = SimulationMode.CPU;

        [Header("Settings")]
        public SimulationSettings settings;
        public Transform enviroment;
        public bool showGrid = false;

        public static SimulationManager Instance { get; private set; }

        private SimulationMode _currentMode;
        public SimulationMode CurrentMode
        {
            get => _currentMode;
            set {
                if (_currentMode == value) return;
                
                _currentMode = value;
                ChangeMode(_currentMode);
            }
        }
        private SimulationMode _lastModeEditorCheck;


        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            float scale = settings.gridResolution / 54f;
            enviroment.localScale = new Vector3(scale, scale, scale);
        }


        private void Start()
        {
            _currentMode = (SimulationMode)(-1);
            CurrentMode = currentMode;

            _lastModeEditorCheck = currentMode;
        }


        private void Update()
        {
            if (!Application.isPlaying) return;

            if (currentMode == _lastModeEditorCheck) return;
            _lastModeEditorCheck = currentMode;
            CurrentMode = currentMode;
        }


        private void ChangeMode(SimulationMode mode)
        {
            _strategy?.Dispose();

            _strategy = mode switch
            {
                SimulationMode.CPU => new CPUSimulationStrategy(),
                SimulationMode.GPU => new GPUSimulationStrategy(),
                _ => null
            };

            _strategy?.Start();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGrid) return;

            Gizmos.color = Color.red;

            float cellSize = settings.neighbourDistance * 2f;
            float cellsPerAxis = settings.gridResolution / cellSize;

            float minBound = -settings.gridResolution * .5f;
            float centerMin = minBound + settings.neighbourDistance;

            Vector3 minVector = new(centerMin, centerMin, centerMin);

            for (var i = 0; i < cellsPerAxis; i++)
                for (var j = 0; j < cellsPerAxis; j++)
                    for (var k = 0; k < cellsPerAxis; k++)
                        Gizmos.DrawWireCube(minVector + new Vector3(i, j, k) * cellSize,
                            new Vector3(cellSize, cellSize, cellSize));
        }
    }
}

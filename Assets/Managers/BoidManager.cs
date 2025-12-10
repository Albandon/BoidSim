using Strategies;
using Strategies.Interfaces;
using UnityEngine;

namespace Managers
{
    public class BoidManager : MonoBehaviour {
        public SimulationMode simulationMode;
        private IFlockingStrategy _strategy;

        private void Start() {
            InitStrategy(simulationMode);
        }

        private void Update() {
            _strategy.Update(Time.deltaTime);
        }

        private void InitStrategy (SimulationMode mode) {
            
        
            _strategy = mode switch {
                SimulationMode.CPU => new CPUFlockingStrategy(),
                SimulationMode.GPU => new GPUFlockingStrategy(),
                _ => null
            };
            _strategy?.Initialize();
        }

        private void OnDestroy() {
            _strategy?.Dispose();
        }


        private void OnDrawGizmosSelected() {
            if (SimulationManager.Instance == null) { return; }
            var settings = SimulationManager.Instance.settings;
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, settings.spawnRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, settings.neighbourDistance);
        }
    }
}
using Strategies.Interfaces;
using UnityEngine.SceneManagement;

namespace Strategies {
    public class GPUSimulationStrategy : ISimulationStrategy {
        public void Start() {
            SceneManager.LoadSceneAsync("Scenes/GPU", LoadSceneMode.Additive);
        }

        public void Dispose() {
            SceneManager.UnloadSceneAsync("Scenes/GPU");
        }
    }
}
using Strategies.Interfaces;
using UnityEngine.SceneManagement;

namespace Strategies {
    public class CPUSimulationStrategy : ISimulationStrategy {
        public void Start() {
            SceneManager.LoadSceneAsync("Scenes/CPU", LoadSceneMode.Additive);
        }

        public void Dispose() {
            SceneManager.UnloadSceneAsync("Scenes/CPU");
        }
    }
}
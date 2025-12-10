namespace Strategies.Interfaces {
    public interface IFlockingStrategy {
        void Initialize();
        void Update( float deltaTime);
        void Dispose();
    }
}
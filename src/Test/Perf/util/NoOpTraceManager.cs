namespace Roslyn.Test.Performance.Utilities
{
    internal class NoOpTraceManager : ITraceManager
    {
        public NoOpTraceManager()
        {
        }

        public bool HasWarmUpIteration
        {
            get
            {
                return false;
            }
        }

        public void Initialize()
        {
        }

        public void Cleanup()
        {
        }

        public void EndEvent()
        {
        }

        public void EndScenario()
        {
        }

        public void EndScenarios()
        {
        }

        public void ResetScenarioGenerator()
        {
        }

        public void Setup()
        {
        }

        public void Start()
        {
        }

        public void StartEvent()
        {
        }

        public void StartScenarios()
        {
        }

        public void StartScenario(string scenarioName, string processName)
        {
        }

        public void Stop()
        {
        }

        public void WriteScenarios(string[] scenarios)
        {
        }

        public void WriteScenariosFileToDisk()
        {
        }
    }
}

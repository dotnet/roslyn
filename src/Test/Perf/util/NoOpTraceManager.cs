using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    public class NoOpTraceManager : ITraceManager
    {
        private readonly int _iterations;
        public NoOpTraceManager(int iterations)
        {
            _iterations = iterations;
        }

        public int Iterations
        {
            get
            {
                return _iterations;
            }
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

        public void StartScenario(string scenarioName, string processName)
        {
        }

        public void Stop()
        {
        }

        public void WriteScenariosFileToDisk()
        {
        }
    }
}

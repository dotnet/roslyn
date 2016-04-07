using System;
using System.Diagnostics;
using System.IO;
namespace Roslyn.Test.Performance.Utilities
{
    public interface ITraceManager
    {
        int Iterations { get; }

        void Cleanup();
        void EndEvent();
        void EndScenario();
        void EndScenarios();
        void ResetScenarioGenerator();
        void Setup();
        void Start();
        void StartEvent();
        void StartScenario(string scenarioName, string processName);
        void Stop();
        void WriteScenariosFileToDisk();
    }
}

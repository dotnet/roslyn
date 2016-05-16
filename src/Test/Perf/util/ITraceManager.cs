using System;
using System.Diagnostics;
using System.IO;
namespace Roslyn.Test.Performance.Utilities
{
    public interface ITraceManager
    {
        bool HasWarmUpIteration { get; }

        void Initialize();
        void Cleanup();
        void EndEvent();
        void EndScenario();
        void EndScenarios();
        void ResetScenarioGenerator();
        void Setup();
        void Start();
        void StartEvent();
        void StartScenario(string scenarioName, string processName);
        void StartScenarios();
        void Stop();
        void WriteScenarios(string[] scenarios);
        void WriteScenariosFileToDisk();
    }
}

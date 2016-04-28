using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Roslyn.Test.Performance.Utilities.DownloadUtilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Utilities
{
    public class TraceManager : ITraceManager
    {
        private readonly ScenarioGenerator _scenarioGenerator;
        private readonly int _iterations;
        private readonly string _cpcPath;

        private int _startEventAbsoluteInstance = 1;
        private int _stopEventAbsoluteInstance = 1;
        private readonly bool _verbose;
        private readonly ILogger _logger;

        public TraceManager(
            int iterations,
            string cpcPath,
            string scenarioPath,
            bool verbose,
            ILogger logger)
        {
            _iterations = iterations;
            _cpcPath = cpcPath;
            _scenarioGenerator = new ScenarioGenerator(scenarioPath);
            _verbose = verbose;
            _logger = logger;
        }

        public int Iterations
        {
            get
            {
                return _iterations;
            }
        }

        public void Setup()
        {
            ShellOutVital(_cpcPath, "/Setup /DisableArchive", _verbose, _logger);
        }

        public void Start()
        {
            ShellOutVital(_cpcPath, "/Start /DisableArchive", _verbose, _logger);
        }

        public void Stop()
        {
            var scenariosXmlPath = Path.Combine(GetCPCDirectoryPath(), "scenarios.xml");
            var consumptionTempResultsPath = Path.Combine(GetCPCDirectoryPath(), "ConsumptionTempResultsPath.xml");
            ShellOutVital(_cpcPath, $"/Stop /DisableArchive /ScenarioPath=\"{scenariosXmlPath}\" /ConsumptionTempResultsPath=\"{consumptionTempResultsPath}\"", _verbose, _logger);
        }

        public void Cleanup()
        {
            ShellOutVital(_cpcPath, "/Cleanup /DisableArchive", _verbose, _logger);
        }

        public void StartScenario(string scenarioName, string processName)
        {
            _scenarioGenerator.AddStartScenario(scenarioName, processName);
        }

        public void StartEvent()
        {
            _scenarioGenerator.AddStartEvent(_startEventAbsoluteInstance);
            _startEventAbsoluteInstance++;
        }

        public void EndEvent()
        {
            _scenarioGenerator.AddEndEvent();
            _stopEventAbsoluteInstance++;
        }

        public void EndScenario()
        {
            _scenarioGenerator.AddEndScenario();
        }

        public void EndScenarios()
        {
            _scenarioGenerator.AddScenariosFileEnd();
        }

        public void WriteScenariosFileToDisk()
        {
            _scenarioGenerator.WriteToDisk();
        }

        public void ResetScenarioGenerator()
        {
            _scenarioGenerator.Initialize();
            _startEventAbsoluteInstance = 1;
            _stopEventAbsoluteInstance = 1;
        }
    }
}

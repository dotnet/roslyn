#load "scenario_generator_util.csx"
#load "test_util.csx"

using System;
using System.Diagnostics;
using System.IO;

interface ITraceManager
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

class TraceManagerFactory
{
    public static ITraceManager GetTraceManager(int iterations = 1)
    {
        var directoryInfo = new RelativeDirectory();
        var cpcFullPath = Path.Combine(directoryInfo.CPCDirectoryPath, "CPC.exe");
        var scenarioPath = directoryInfo.CPCDirectoryPath;
        if (File.Exists(cpcFullPath))
        {
            return new TraceManager(iterations, cpcFullPath, scenarioPath);
        }
        else
        {
            return new NoOpTraceManager(iterations);
        }
    }
}

/// This is the Trace Manager that will be used when there is no CPC found in the machine
/// All operations are NoOp
class NoOpTraceManager : ITraceManager
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

class TraceManager: ITraceManager
{
    private readonly ScenarioGenerator _scenarioGenerator;
    private readonly int _iterations;
    private readonly string _cpcPath;
    
    private RelativeDirectory _directoryInfo = new RelativeDirectory();

    private int _startEventAbsoluteInstance = 1;
    private int _stopEventAbsoluteInstance = 1;

    public TraceManager(
        int iterations,
        string cpcPath,
        string scenarioPath): base()
    {
        _iterations = iterations;
        _cpcPath = cpcPath;
        _scenarioGenerator = new ScenarioGenerator(scenarioPath);
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
        ShellOutVital(_cpcPath, "/Setup /DisableArchive");
    }

    public void Start()
    {
        ShellOutVital(_cpcPath, "/Start /DisableArchive");
    }

    public void Stop()
    {
        var scenariosXmlPath = Path.Combine(_directoryInfo.CPCDirectoryPath, "scenarios.xml");
        var consumptionTempResultsPath = Path.Combine(_directoryInfo.CPCDirectoryPath, "ConsumptionTempResultsPath.xml");
        ShellOutVital(_cpcPath, $"/Stop /DisableArchive /ScenarioPath=\"{scenariosXmlPath}\" /ConsumptionTempResultsPath=\"{consumptionTempResultsPath}\"");
    }

    public void Cleanup()
    {
        ShellOutVital(_cpcPath, "/Cleanup /DisableArchive");
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

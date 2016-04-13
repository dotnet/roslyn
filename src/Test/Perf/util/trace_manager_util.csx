#load "scenario_generator_util.csx"
#load "test_util.csx"

using System;
using System.Diagnostics;
using System.IO;

interface ITraceManager
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
    void WriteScenariosFileToDisk();
}

class TraceManagerFactory
{
    public static ITraceManager GetTraceManager()
    {
        var directoryInfo = new RelativeDirectory();
        var cpcFullPath = Path.Combine(directoryInfo.CPCDirectoryPath, "CPC.exe");
        var scenarioPath = directoryInfo.CPCDirectoryPath;
        if (File.Exists(cpcFullPath))
        {
            return new TraceManager(cpcFullPath, scenarioPath);
        }
        else
        {
            return new NoOpTraceManager();
        }
    }
}

/// This is the Trace Manager that will be used when there is no CPC found in the machine
/// All operations are NoOp
class NoOpTraceManager : ITraceManager
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

    public void WriteScenariosFileToDisk()
    {
    }
}

class TraceManager: ITraceManager
{
    private readonly ScenarioGenerator _scenarioGenerator;
    private readonly string _cpcPath;
    
    private RelativeDirectory _directoryInfo = new RelativeDirectory();

    private int _startEventAbsoluteInstance = 1;
    private int _stopEventAbsoluteInstance = 1;

    public TraceManager(
        string cpcPath,
        string scenarioPath): base()
    {
        _cpcPath = cpcPath;
        _scenarioGenerator = new ScenarioGenerator(scenarioPath);
    }

    public bool HasWarmUpIteration
    {
        get
        {
            return true;
        }    
    }
    
    // Cleanup the results directory and files before every run
    public void Initialize()
    {
        var consumptionTempResultsPath = Path.Combine(_directoryInfo.CPCDirectoryPath, "ConsumptionTempResults.xml");
        if (File.Exists(consumptionTempResultsPath))
        {
            File.Delete(consumptionTempResultsPath);
        }
        
        if (Directory.Exists(_directoryInfo.CPCDirectoryPath))
        {
            var databackDirectories = Directory.GetDirectories(_directoryInfo.CPCDirectoryPath, "DataBackup*", SearchOption.AllDirectories);
            foreach (var databackDirectory in databackDirectories)
            {
                Directory.Delete(databackDirectory, true);
            }
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
        var consumptionTempResultsPath = Path.Combine(_directoryInfo.CPCDirectoryPath, "ConsumptionTempResults.xml");
        ShellOutVital(_cpcPath, $"/Stop /DisableArchive /ScenarioPath=\"{scenariosXmlPath}\" /ConsumptionTempResultsPath=\"{consumptionTempResultsPath}\"");
    }

    public void Cleanup()
    {
        ShellOutVital(_cpcPath, "/Cleanup /DisableArchive");
    }
    
    public void StartScenarios()
    {
        _scenarioGenerator.AddScenariosFileStart();
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

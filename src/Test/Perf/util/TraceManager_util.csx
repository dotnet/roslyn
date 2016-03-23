#load "ScenarioGenerator_util.csx"

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
    public static ITraceManager GetTraceManager(
        int iterations = 3,
        string cpcFolderPath = @"%SYSTEMDRIVE%\CPC",
        string scenarioPath = @"%SYSTEMDRIVE%\CPC")
    {
        var cpcFullPath = Path.Combine(Environment.ExpandEnvironmentVariables(cpcFolderPath), "CPC.exe");
        if (File.Exists(cpcFullPath))
        {
            var expandedScenarioPath = Environment.ExpandEnvironmentVariables(scenarioPath);
            return new TraceManager(iterations, cpcFullPath, expandedScenarioPath);
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

class TraceManager : ITraceManager
{
    private readonly ScenarioGenerator _scenarioGenerator;

    private string _cpcPath = "CPC.exe";
    private int _startEventAbsoluteInstance = 1;
    private int _stopEventAbsoluteInstance = 1;
    private readonly int _iterations;
    
    public TraceManager(
        int iterations,
        string cpcPath,
        string scenarioPath)
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
         var processResult = RunProcess(_cpcPath, "/Setup /DisableArchive");
        if (processResult.Failed)
        {
            throw new SystemException($@"The process ""CPC.exe /Setup /DisableArchive"" failed. {processResult.StdErr}");
        }
    }

    public void Start()
    {
        var processResult = RunProcess(_cpcPath, "/Start /DisableArchive");
        if (processResult.Failed)
        {
            throw new SystemException($@"The process ""CPC.exe /Start /DisableArchive"" failed. {processResult.StdErr}");
        }
    }

    public void Stop()
    {
        var processResult = RunProcess(_cpcPath, @"/Stop /DisableArchive /ScenarioPath=""%SYSTEMDRIVE%/CPC/scenarios.xml"" /ConsumptionTempResultsPath=""%SYSTEMDRIVE%/CPC/consumptionTempResults.xml""");
        if (processResult.Failed)
        {
            throw new SystemException($@"The process ""CPC.exe /Stop /DisableArchive"" failed. {processResult.StdErr}");
        }
    }

    public void Cleanup()
    {
        var processResult = RunProcess(_cpcPath, "/Cleanup /DisableArchive");
        if (processResult.Failed)
        {
            throw new SystemException($@"The process ""CPC.exe /Cleanup /DisableArchive"" failed. {processResult.StdErr}");
        }
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

    private ProcessResult RunProcess(string executablePath, string args)
    {
        var startInfo = new ProcessStartInfo(executablePath, args);
        startInfo.UseShellExecute = true;
        var process = new Process
        {
            StartInfo = startInfo,
        };

        process.Start();

        process.WaitForExit();

        return new ProcessResult
        {
            ExecutablePath = executablePath,
            Args = args,
            Code = process.ExitCode,
            StdOut = "",
            StdErr = "",
        };
    }
}
#load "ScenarioGenerator_util.csx"

using System;
using System.Diagnostics;
using System.IO;

public class TraceManager
{
    private readonly ScenarioGenerator _scenarioGenerator;

    private string _cpcFullPath = "CPC.exe";
    private int _startEventAbsoluteInstance = 1;
    private int _stopEventAbsoluteInstance = 1;
    
    public TraceManager(
        string cpcFolderPath = @"%SYSTEMDRIVE%\CPC",
        string scenarioPath = @"%SYSTEMDRIVE%\CPC")
    {
        _cpcFullPath = Path.Combine(Environment.ExpandEnvironmentVariables(cpcFolderPath), "CPC.exe");
        _scenarioGenerator = new ScenarioGenerator(Environment.ExpandEnvironmentVariables(scenarioPath));
    }
    
    public void Setup()
    {
         var processResult = RunProcess(_cpcFullPath, "/Setup /DisableArchive");
        if (processResult.Failed)
        {
            throw new SystemException($@"The process ""CPC.exe /Setup /DisableArchive"" failed. {processResult.StdErr}");
        }
    }

    public void Start()
    {
        var processResult = RunProcess(_cpcFullPath, "/Start /DisableArchive");
        if (processResult.Failed)
        {
            throw new SystemException($@"The process ""CPC.exe /Start /DisableArchive"" failed. {processResult.StdErr}");
        }
    }

    public void Stop()
    {
        var processResult = RunProcess(_cpcFullPath, @"/Stop /DisableArchive /ScenarioPath=""%SYSTEMDRIVE%/CPC/scenarios.xml"" /ConsumptionTempResultsPath=""%SYSTEMDRIVE%/CPC/consumptionTempResults.xml""");
        if (processResult.Failed)
        {
            throw new SystemException($@"The process ""CPC.exe /Stop /DisableArchive"" failed. {processResult.StdErr}");
        }
    }

    public void Cleanup()
    {
        var processResult = RunProcess(_cpcFullPath, "/Cleanup /DisableArchive");
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

    public static ProcessResult RunProcess(string _cpcFullPath, string args)
    {
        var startInfo = new ProcessStartInfo(_cpcFullPath, args);
        startInfo.UseShellExecute = true;
        var process = new Process
        {
            StartInfo = startInfo,
        };

        process.Start();

        process.WaitForExit();

        return new ProcessResult
        {
            ExecutablePath = _cpcFullPath,
            Args = args,
            Code = process.ExitCode,
            StdOut = "",
            StdErr = "",
        };
    }
}
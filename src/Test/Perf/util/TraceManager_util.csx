#load "ScenarioGenerator_util.csx"
#load "PerformanceEventSource_util.csx"

using System;
using System.Diagnostics;
using System.IO;

public class TraceManager
{
    private readonly ScenarioGenerator _scenarioGenerator;

    private string _cpcFullPath = "CPC.exe";
    public TraceManager(
        string cpcFolderPath = @"%SYSTEMDRIVE%\CPC",
        string scenarioPath = @"%SYSTEMDRIVE%\CPC")
    {
        _cpcFullPath = Path.Combine(Environment.ExpandEnvironmentVariables(cpcFolderPath), "CPC.exe");
        _scenarioGenerator = new ScenarioGenerator(Environment.ExpandEnvironmentVariables(scenarioPath));
    }

    public static void Run(string testExecutable, string argument)
    {
        var processResult = RunProcess(testExecutable, argument);
        if (processResult.Failed)
        {
            throw new SystemException($@"The process {testExecutable} failed. {processResult.StdErr}");
        }
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
        var processResult = RunProcess(_cpcFullPath, "/Stop /DisableArchive");
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
        _scenarioGenerator.AddStartEvent(PerformanceEventSource.Log.Guid.ToString(), 1);
        PerformanceEventSource.Log.EventStart();
    }

    public void EndEvent()
    {
        _scenarioGenerator.AddEndEvent(PerformanceEventSource.Log.Guid.ToString(), 2);
        PerformanceEventSource.Log.EventEnd();
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
    }
    
    public void PrintTest()
    {
        Console.WriteLine("Test Printing by TraceManager");
    }

    public static ProcessResult RunProcess(string _cpcFullPath, string args)
    {
        var startInfo = new ProcessStartInfo(_cpcFullPath, args);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.Start();

        var output = new StringWriter();
        var error = new StringWriter();

        process.OutputDataReceived += (s, e) => {
            if (!String.IsNullOrEmpty(e.Data))
            {
                output.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) => {
            if (!String.IsNullOrEmpty(e.Data))
            {
                error.WriteLine(e.Data);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return new ProcessResult
        {
            ExecutablePath = _cpcFullPath,
            Args = args,
            Code = process.ExitCode,
            StdOut = output.ToString(),
            StdErr = error.ToString(),
        };
    }
}
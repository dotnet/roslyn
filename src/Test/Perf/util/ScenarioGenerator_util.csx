using System.Collections.Generic;
using System.IO;

public class ScenarioGenerator
{
    private const string KernelProviderGuid = @"{9e814aad-3204-11d2-9a82-006008a86939}";
    private string _fullPath = "scenarios.xml";
    private List<string> _buffer;

    public ScenarioGenerator(string scenarioFolderPath = "")
    {
        if (!string.IsNullOrEmpty(scenarioFolderPath))
        {
            _fullPath = Path.Combine(scenarioFolderPath, _fullPath);
        }

        Initialize();
    }

    public void Initialize()
    {
        // Delete any existing file
        if (File.Exists(_fullPath))
        {
            File.Delete(_fullPath);
        }

        _buffer = new List<string>();
        AddScenariosFileStart();
    }

    public void AddScenariosFileStart()
    {
        Log(@"<?xml version=""1.0"" encoding=""utf-8"" ?>");
        Log(@"<scenarios>");
    }

    public void AddScenariosFileEnd()
    {
        Log(@"</scenarios>");
    }

    public void AddStartScenario(string scenarioName, string processName)
    {
        Log($@"<scenario name=""{scenarioName}"" process=""{processName}"">");
    }

    public void AddEndScenario()
    {
        Log(@"</scenario>");
    }

    public void AddStartEvent(string providerGuid, int eventId, int absoluteInstance)
    {
        //Log($@"<from providerGuid=""{providerGuid}"" eventId=""{eventId}"" eventName = ""PerformanceEventSource/Event/Start""/>");
        Log($@"<from providerGuid=""{KernelProviderGuid}"" absoluteInstance=""{absoluteInstance}"" process=""csc"" eventName = ""Process/Start""/>");
    }

    public void AddEndEvent(string providerGuid, int eventId, int absoluteInstance)
    {
        //Log($@"<to providerGuid=""{providerGuid}"" eventId=""{eventId}"" eventName = ""PerformanceEventSource/Eventend""/>");
        Log($@"<to providerGuid=""{KernelProviderGuid}"" absoluteInstance=""1"" process=""csc"" eventName=""Process/Stop""/>");
    }

    public void AddComment(string comment)
    {
        Log($@"<!-- {comment} -->");
    }

    public void WriteToDisk()
    {
        File.WriteAllLines(_fullPath, _buffer);
    }

    private void Log(string log)
    {
        _buffer.Add(log);
    }
}
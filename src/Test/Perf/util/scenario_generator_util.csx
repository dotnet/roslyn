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
        WriteToBuffer(@"<?xml version=""1.0"" encoding=""utf-8"" ?>");
        WriteToBuffer(@"<scenarios>");
    }

    public void AddScenariosFileEnd()
    {
        WriteToBuffer(@"</scenarios>");
    }

    public void AddStartScenario(string scenarioName, string processName)
    {
        WriteToBuffer($@"<scenario name=""{scenarioName}"" process=""{processName}"">");
    }

    public void AddEndScenario()
    {
        WriteToBuffer(@"</scenario>");
    }

    public void AddStartEvent(int absoluteInstance)
    {
        WriteToBuffer($@"<from providerGuid=""{KernelProviderGuid}"" absoluteInstance=""{absoluteInstance}"" process=""csc"" eventName = ""Process/Start""/>");
    }

    public void AddEndEvent()
    {
        WriteToBuffer($@"<to providerGuid=""{KernelProviderGuid}"" absoluteInstance=""1"" process=""csc"" eventName=""Process/Stop""/>");
    }

    public void AddComment(string comment)
    {
        WriteToBuffer($@"<!-- {comment} -->");
    }

    public void WriteToDisk()
    {
        File.WriteAllLines(_fullPath, _buffer);
    }

    private void WriteToBuffer(string content)
    {
        _buffer.Add(content);
    }
}
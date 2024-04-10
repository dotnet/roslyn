// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.IO;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Utilities
{
    public class ScenarioGenerator
    {
        private const string KernelProviderGuid = @"{9e814aad-3204-11d2-9a82-006008a86939}";
        private readonly string _fullPath;
        private readonly List<string> _buffer;

        public ScenarioGenerator()
        {
            _fullPath = Path.Combine(TestUtilities.GetCPCDirectoryPath(), "scenarios.xml");
            _buffer = [];
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

        public void AddLine(string line)
        {
            WriteToBuffer(line);
        }

        public void WriteToDisk()
        {
            if (File.Exists(_fullPath))
            {
                Log($"deleting {_fullPath}");
                File.Delete(_fullPath);
            }

            Log($"Writing scenarios.xml to {_fullPath}");
            File.WriteAllLines(_fullPath, _buffer);
        }

        private void WriteToBuffer(string content)
        {
            _buffer.Add(content);
        }
    }
}

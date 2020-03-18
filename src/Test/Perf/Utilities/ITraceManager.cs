﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    public interface ITraceManager
    {
        bool HasWarmUpIteration { get; }

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

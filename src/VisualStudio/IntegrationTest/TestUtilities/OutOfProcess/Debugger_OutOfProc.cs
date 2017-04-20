// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio debugger by remoting calls into Visual Studio.
    /// </summary>
    public partial class Debugger_OutOfProc : OutOfProcComponent
    {
        public Verifier Verify { get; }

        private readonly Debugger_InProc _debuggerInProc;
        private readonly VisualStudioInstance _instance;

        public Debugger_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _debuggerInProc = CreateInProcComponent<Debugger_InProc>(visualStudioInstance);
            Verify = new Verifier(this);
        }

        public void SetBreakPoint(string fileName, int lineNumber, int columnIndex) => _debuggerInProc.SetBreakPoint(fileName, lineNumber, columnIndex);

        public void StartDebugging(bool waitForBreakMode) => _debuggerInProc.StartDebugging(waitForBreakMode);

        public void StepOver(bool waitForBreakOrEnd) => _debuggerInProc.StepOver(waitForBreakOrEnd);

        public void Stop(bool waitForDesignMode) => _debuggerInProc.Stop(waitForDesignMode);

        public string EvaluateExpression(string expression) => _debuggerInProc.EvaluateExpression(expression);
    }
}
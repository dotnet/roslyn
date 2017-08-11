// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Debugger_InProc : InProcComponent
    {
        private readonly EnvDTE.Debugger _debugger;

        private Debugger_InProc()
        {
            _debugger = GetDTE().Debugger;
        }

        public static Debugger_InProc Create()
            => new Debugger_InProc();

        public void SetBreakPoint(string fileName, int lineNumber, int columnIndex)
        {
            // Need to increment the line number because editor line numbers starts from 0 but the debugger ones starts from 1.
            _debugger.Breakpoints.Add(File: fileName, Line: lineNumber + 1, Column: columnIndex);
        }

        public void Go(bool waitForBreakMode) => _debugger.Go(waitForBreakMode);

        public void StepOver(bool waitForBreakOrEnd) => _debugger.StepOver(waitForBreakOrEnd);

        public void Stop(bool waitForDesignMode) => _debugger.Stop(WaitForDesignMode: waitForDesignMode);

        public void SetNextStatement() => _debugger.SetNextStatement();

        public void ExecuteStatement(string statement) => _debugger.ExecuteStatement(statement);

        public Common.Expression GetExpression(string expressionText) => new Common.Expression(_debugger.GetExpression(expressionText));
    }
}

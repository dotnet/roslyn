// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public void Go(bool waitForBreakMode) => _debugger.Go(waitForBreakMode);

        public Common.Expression GetExpression(string expressionText) => new Common.Expression(_debugger.GetExpression(expressionText));
    }
}

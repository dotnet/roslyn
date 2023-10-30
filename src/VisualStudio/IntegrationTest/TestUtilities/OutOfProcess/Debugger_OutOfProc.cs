// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio debugger by remoting calls into Visual Studio.
    /// </summary>
    public partial class Debugger_OutOfProc : OutOfProcComponent
    {
        private readonly Debugger_InProc _debuggerInProc;

        public Debugger_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            _debuggerInProc = CreateInProcComponent<Debugger_InProc>(visualStudioInstance);
        }

        public void Go(bool waitForBreakMode) => _debuggerInProc.Go(waitForBreakMode);

        public void CheckExpression(string expressionText, string expectedType, string expectedValue)
        {
            var entry = _debuggerInProc.GetExpression(expressionText);
            Assert.Equal(expectedType, entry.Type);
            Assert.Equal(expectedValue, entry.Value);
        }
    }
}

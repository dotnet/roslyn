// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.Debugger
{
    public static partial class DebuggerExtensions
    {
        public static void SetBreakpoint(this AbstractIntegrationTest test, string text)
        {
            test.PlaceCaret(text);
            test.ExecuteCommand("Debug.ToggleBreakpoint");
        }

        public static void StartDebugging(this AbstractIntegrationTest test, bool waitForBreakMode = false)
        {
            test.VisualStudio.Instance.Debugger.StartDebugging(waitForBreakMode);
        }

        public static void ContinueExecution(this AbstractIntegrationTest test, bool waitForBreakMode = false)
        {
            test.VisualStudio.Instance.Debugger.Continue(waitForBreakMode);
        }
    }
}

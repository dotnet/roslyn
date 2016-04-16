// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using System;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    public class ScriptingTestHelpers
    {
        internal static void AssertCompilationError(Script script, params DiagnosticDescription[] expectedDiagnostics)
        {
            AssertCompilationError(() => script.RunAsync().Wait(), expectedDiagnostics);
        }

        internal static void AssertCompilationError(Task<ScriptState> state, string code, params DiagnosticDescription[] expectedDiagnostics)
        {
            AssertCompilationError(() => state.Result.ContinueWithAsync(code).Wait(), expectedDiagnostics);
        }

        internal static void AssertCompilationError<T>(Task<ScriptState<T>> state, string code, params DiagnosticDescription[] expectedDiagnostics)
        {
            AssertCompilationError(() => state.Result.ContinueWithAsync(code).Wait(), expectedDiagnostics);
        }

        internal static void AssertCompilationError(ScriptState state, string code, params DiagnosticDescription[] expectedDiagnostics)
        {
            AssertCompilationError(() => state.ContinueWithAsync(code).Wait(), expectedDiagnostics);
        }

        internal static void AssertCompilationError(Action action, params DiagnosticDescription[] expectedDiagnostics)
        {
            bool noException = false;
            try
            {
                action();
                noException = true;
            }
            catch (CompilationErrorException e)
            {
                e.Diagnostics.Verify(expectedDiagnostics);
                e.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && e.Message == d.ToString());
            }
            catch (Exception e)
            {
                Assert.False(true, "Unexpected exception: " + e);
            }

            Assert.False(noException);
        }
    }
}

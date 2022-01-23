// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        public static ScriptState<T> RunScriptWithOutput<T>(Script<T> script, string expectedOutput)
        {
            string output;
            string errorOutput;
            ScriptState<T> result = null;
            RuntimeEnvironmentFactory.CaptureOutput(() =>
            {
                var task = script.RunAsync();
                task.Wait();
                result = task.Result;
            }, expectedOutput.Length, out output, out errorOutput);
            Assert.Equal(expectedOutput, output.Trim());
            return result;
        }

        public static T EvaluateScriptWithOutput<T>(Script<T> script, string expectedOutput)
        {
            string output;
            string errorOutput;
            T result = default(T);
            RuntimeEnvironmentFactory.CaptureOutput(() =>
            {
                var task = script.EvaluateAsync();
                task.Wait();
                result = task.Result;
            }, expectedOutput.Length, out output, out errorOutput);
            Assert.Equal(expectedOutput, output.Trim());
            return result;
        }

        public static void ContinueRunScriptWithOutput<T>(Task<ScriptState<T>> scriptState, string code, string expectedOutput)
        {
            string output;
            string errorOutput;
            RuntimeEnvironmentFactory.CaptureOutput(() =>
            {
                scriptState.ContinueWith(code).Wait();
            }, expectedOutput.Length, out output, out errorOutput);
            Assert.Equal(expectedOutput, output.Trim());
        }

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

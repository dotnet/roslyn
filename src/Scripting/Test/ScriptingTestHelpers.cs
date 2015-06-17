// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    public class ScriptingTestHelpers
    {
        internal static void AssertCompilationError(ScriptEngine engine, string code, params DiagnosticDescription[] expectedDiagnostics)
        {
            AssertCompilationError(engine.CreateSession(), code, expectedDiagnostics);
        }

        internal static void AssertCompilationError(Session session, string code, params DiagnosticDescription[] expectedDiagnostics)
        {
            bool noException = false;
            try
            {
                session.Execute(code);
                noException = true;
            }
            catch (CompilationErrorException e)
            {
                e.Diagnostics.Verify(expectedDiagnostics);
                e.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && e.Message == d.ToString());
            }

            Assert.False(noException);
        }
    }
}

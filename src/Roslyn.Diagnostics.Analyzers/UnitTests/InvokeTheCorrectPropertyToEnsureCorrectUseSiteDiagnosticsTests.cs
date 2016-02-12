// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class InvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicInvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return null;
        }
    }
}
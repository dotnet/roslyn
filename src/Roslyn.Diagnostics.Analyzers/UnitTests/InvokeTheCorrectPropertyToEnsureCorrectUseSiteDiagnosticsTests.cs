// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.VisualBasic.Analyzers;
using Test.Utilities;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.BasicInvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class InvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsTests
    {
    }
}
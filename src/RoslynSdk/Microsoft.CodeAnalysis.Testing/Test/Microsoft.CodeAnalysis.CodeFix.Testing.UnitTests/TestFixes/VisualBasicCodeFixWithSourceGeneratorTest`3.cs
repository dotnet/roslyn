// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.TestFixes
{
    internal class VisualBasicCodeFixWithSourceGeneratorTest<TAnalyzer, TCodeFix, TSourceGenerator>
        : VisualBasicCodeFixTest<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
        where TSourceGenerator : ISourceGenerator, new()
    {
        protected override IEnumerable<Type> GetSourceGenerators()
        {
            yield return typeof(TSourceGenerator);
        }
    }
}

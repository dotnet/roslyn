// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing.XUnit
{
    [Obsolete(ObsoleteMessages.FrameworkPackages)]
    public static class AnalyzerVerifier
    {
        public static AnalyzerVerifier<TAnalyzer> Create<TAnalyzer>()
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            return new AnalyzerVerifier<TAnalyzer>();
        }
    }
}

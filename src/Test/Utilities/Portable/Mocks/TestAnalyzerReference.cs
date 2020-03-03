// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Test.Utilities
{
    internal class TestAnalyzerReference : AnalyzerReference
    {
        public override string FullPath => throw new NotImplementedException();
        public override object Id => throw new NotImplementedException();
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => throw new NotImplementedException();
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => throw new NotImplementedException();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public partial class DeterministicKeyBuilderTests<TCompilation, TCompilationOptions, TParseOptions>
    {
        private sealed class Analyzer : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

            public override void Initialize(AnalysisContext context) => throw new NotImplementedException();
        }

        private sealed class Analyzer2 : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

            public override void Initialize(AnalysisContext context) => throw new NotImplementedException();
        }

        private sealed class Generator : ISourceGenerator
        {
            public void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();

            public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
        }

        private sealed class Generator2 : ISourceGenerator
        {
            public void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();

            public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
        }
    }
}

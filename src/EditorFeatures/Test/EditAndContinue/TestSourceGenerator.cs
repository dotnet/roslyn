// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class TestSourceGenerator : DiagnosticAnalyzer, ISourceGenerator
    {
        public Action<GeneratorExecutionContext>? ExecuteImpl;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => throw new NotImplementedException();

        public void Execute(GeneratorExecutionContext context)
            => (ExecuteImpl ?? throw new NotImplementedException()).Invoke(context);

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}

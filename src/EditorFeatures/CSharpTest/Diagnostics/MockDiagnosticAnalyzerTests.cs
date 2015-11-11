// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.MockDiagnosticAnalyzer
{
    public partial class MockDiagnosticAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private class MockDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public const string Id = "MockDiagnostic";
            private DiagnosticDescriptor _descriptor = new DiagnosticDescriptor(Id, "MockDiagnostic", "MockDiagnostic", "InternalCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(_descriptor);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);
            }

            public void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
            {
                context.RegisterCompilationEndAction(AnalyzeCompilation);
            }

            public void AnalyzeCompilation(CompilationAnalysisContext context)
            {
            }
        }

        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(
                    new MockDiagnosticAnalyzer(),
                    null);
        }

        private async Task VerifyDiagnosticsAsync(
             string source,
             params DiagnosticDescription[] expectedDiagnostics)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(source))
            {
                var actualDiagnostics = await this.GetDiagnosticsAsync(workspace);
                actualDiagnostics.Verify(expectedDiagnostics);
            }
        }

        [WorkItem(906919)]
        [WpfFact]
        public async Task Bug906919()
        {
            string source = "[|class C { }|]";
            await VerifyDiagnosticsAsync(source);
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
            private readonly DiagnosticDescriptor _descriptor = new DiagnosticDescriptor(Id, "MockDiagnostic", "MockDiagnostic", "InternalCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

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

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new MockDiagnosticAnalyzer(), null);

        private async Task VerifyDiagnosticsAsync(
             string source,
             params DiagnosticDescription[] expectedDiagnostics)
        {
            using var workspace = TestWorkspace.CreateCSharp(source);
            var actualDiagnostics = await this.GetDiagnosticsAsync(workspace, new TestParameters());
            actualDiagnostics.Verify(expectedDiagnostics);
        }

        [WorkItem(906919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/906919")]
        [Fact]
        public async Task Bug906919()
        {
            var source = "[|class C { }|]";
            await VerifyDiagnosticsAsync(source);
        }
    }
}

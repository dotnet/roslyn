// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class SuppressMessageAttributeWorkspaceTests : SuppressMessageAttributeTests
    {
        protected override async Task VerifyAsync(string source, string language, DiagnosticAnalyzer[] analyzers, DiagnosticDescription[] expectedDiagnostics, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = true, string rootNamespace = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(source, language, rootNamespace))
            {
                var documentId = workspace.Documents[0].Id;
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var span = (await document.GetSyntaxRootAsync()).FullSpan;

                var actualDiagnostics = new List<Diagnostic>();
                foreach (var analyzer in analyzers)
                {
                    actualDiagnostics.AddRange(
                        await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(analyzer, document, span, onAnalyzerException, logAnalyzerExceptionAsDiagnostics));
                }

                actualDiagnostics.Verify(expectedDiagnostics);
            }
        }

        private static Task<TestWorkspace> CreateWorkspaceFromFileAsync(string source, string language, string rootNamespace)
        {
            if (language == LanguageNames.CSharp)
            {
                return CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(source);
            }
            else
            {
                return VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(
                    source,
                    compilationOptions: new VisualBasic.VisualBasicCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary, rootNamespace: rootNamespace));
            }
        }

        protected override bool ConsiderArgumentsForComparingDiagnostics
        {
            get
            {
                // Round tripping diagnostics from DiagnosticData causes the Arguments info stored within compiler DiagnosticWithInfo to be lost, so don't compare Arguments in IDE.
                // NOTE: We will still compare squiggled text for the diagnostics, which is also a sufficient test.
                return false;
            }
        }
    }
}

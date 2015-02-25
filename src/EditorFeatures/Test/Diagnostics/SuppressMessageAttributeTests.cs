// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        protected override void Verify(string source, string language, DiagnosticAnalyzer[] analyzers, DiagnosticDescription[] expectedDiagnostics, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException = null, string rootNamespace = null)
        {
            using (var workspace = CreateWorkspaceFromFile(source, language, rootNamespace))
            {
                var documentId = workspace.Documents[0].Id;
                var document = workspace.CurrentSolution.GetDocument(documentId);
                var span = document.GetSyntaxRootAsync().Result.FullSpan;

                var actualDiagnostics = new List<Diagnostic>();
                foreach (var analyzer in analyzers)
                {
                    actualDiagnostics.AddRange(DiagnosticProviderTestUtilities.GetAllDiagnostics(analyzer, document, span, donotCatchAnalyzerExceptions: continueOnAnalyzerException == null));
                }

                actualDiagnostics.Verify(expectedDiagnostics);
            }
        }

        private static TestWorkspace CreateWorkspaceFromFile(string source, string language, string rootNamespace)
        {
            if (language == LanguageNames.CSharp)
            {
                return CSharpWorkspaceFactory.CreateWorkspaceFromFile(source);
            }
            else
            {
                return VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(
                    source,
                    compilationOptions: new VisualBasic.VisualBasicCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary, rootNamespace: rootNamespace));
            }
        }

        protected override DiagnosticDescription WithArguments(DiagnosticDescription d, params string[] arguments)
        {
            // TODO: Round tripping between Diagnostic and DiagnosticData seems to cause us to lose the arguments info.
            //       For now return just the original diagnostic, we need to clean this up to handle this scenario better.
            return d;
        }
    }
}

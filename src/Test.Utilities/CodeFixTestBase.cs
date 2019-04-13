// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Test.Utilities
{
    public abstract class CodeFixTestBase : DiagnosticAnalyzerTestBase
    {
        protected abstract CodeFixProvider GetCSharpCodeFixProvider();

        protected abstract CodeFixProvider GetBasicCodeFixProvider();

        protected void VerifyCSharpFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFix(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), new[] { oldSource }, new[] { newSource }, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, allowUnsafeCode: false, validationMode: validationMode);
        }

        protected void VerifyCSharpFix(string[] oldSources, string[] newSources, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, TestValidationMode validationMode = DefaultTestValidationMode, ReferenceFlags referenceFlags = ReferenceFlags.None)
        {
            VerifyFix(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), oldSources, newSources, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, allowUnsafeCode: false, validationMode: validationMode, referenceFlags: referenceFlags);
        }

        protected void VerifyBasicFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFix(LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), GetBasicCodeFixProvider(), new[] { oldSource }, new[] { newSource }, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, allowUnsafeCode: false, validationMode: validationMode);
        }

        private void VerifyFix(
            string language,
            DiagnosticAnalyzer analyzerOpt,
            CodeFixProvider codeFixProvider,
            string[] oldSources,
            string[] newSources,
            int? codeFixIndex,
            bool allowNewCompilerDiagnostics,
            bool onlyFixFirstFixableDiagnostic,
            bool allowUnsafeCode,
            TestValidationMode validationMode,
            ReferenceFlags referenceFlags = ReferenceFlags.None)
        {
            var runner = new CodeFixRunner(analyzerOpt, codeFixProvider, validationMode);
            Assert.True(oldSources.Length == newSources.Length, "Length of expected and actual sources arrays must match.");
            Document[] documents = CreateDocuments(oldSources, language, referenceFlags, allowUnsafeCode: allowUnsafeCode);

            var project = documents.First().Project;
            Solution newSolution;
            if (onlyFixFirstFixableDiagnostic)
            {
                newSolution = runner.ApplySingleFix(project, ImmutableArray<TestAdditionalDocument>.Empty, codeFixIndex ?? 0);
            }
            else
            {
                newSolution = runner.ApplyFixesOneByOne(project.Solution, ImmutableArray<TestAdditionalDocument>.Empty, allowNewCompilerDiagnostics, codeFixIndex ?? 0);
            }

            VerifyDocuments(newSolution, documents, newSources);
        }

        private static void VerifyDocuments(Solution solution, Document[] documents, string[] newSources)
        {
            for (int i = 0; i < documents.Length; i++)
            {
                var expectedText = newSources[i].Trim();
                var document = solution.GetDocument(documents[i].Id);
                var actualText = GetActualTextForNewDocument(document, documents[i].Name).ToString().Trim();
                if (expectedText != actualText)
                {
                    Assert.False(true, $"Expected:\n{expectedText}\n\nActual:\n{actualText}\n");
                }
            }
        }

        private static SourceText GetActualTextForNewDocument(Document documentInNewWorkspace, string newSourceFileName)
        {
            TextDocument newDocument = documentInNewWorkspace;
            if (documentInNewWorkspace.Name != newSourceFileName)
            {
                newDocument = documentInNewWorkspace.Project.Documents.FirstOrDefault(d => d.Name == newSourceFileName) ??
                    documentInNewWorkspace.Project.AdditionalDocuments.FirstOrDefault(d => d.Name == newSourceFileName);

                if (newDocument == null)
                {
                    throw new Exception($"Unable to find document with name {newSourceFileName} in new workspace after applying fix.");
                }
            }

            return GetSourceText(newDocument);
        }

        private static SourceText GetSourceText(TextDocument textDocument)
        {
            if (((textDocument as Document)?.SupportsSyntaxTree).GetValueOrDefault())
            {
                var newSourceDocument = (Document)textDocument;
                newSourceDocument = Simplifier.ReduceAsync(newSourceDocument, Simplifier.Annotation).Result;
                SyntaxNode root = newSourceDocument.GetSyntaxRootAsync().Result;
                root = Formatter.Format(root, Formatter.Annotation, newSourceDocument.Project.Solution.Workspace);
                return root.GetText();
            }
            else
            {
                return textDocument.GetTextAsync(CancellationToken.None).Result;
            }
        }
    }
}

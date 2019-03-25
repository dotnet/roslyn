// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        protected virtual bool TestFixAllByDefault => true;

        protected void VerifyCSharpUnsafeCodeFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, FixAllScope? testFixAllScope = FixAllScope.Document)
        {
            VerifyFix(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), new[] { oldSource }, new[] { newSource }, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, testFixAllScope, allowUnsafeCode: true, validationMode: DefaultTestValidationMode);
        }

        protected void VerifyCSharpFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, FixAllScope? testFixAllScope = FixAllScope.Document, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFix(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), new[] { oldSource }, new[] { newSource }, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, testFixAllScope, allowUnsafeCode: false, validationMode: validationMode);
        }

        protected void VerifyCSharpFix(string[] oldSources, string[] newSources, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, FixAllScope? testFixAllScope = FixAllScope.Project, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFix(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), oldSources, newSources, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, testFixAllScope, allowUnsafeCode: false, validationMode: validationMode);
        }

        protected void VerifyCSharpFixAll(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFixAll(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), new[] { oldSource }, new[] { newSource }, FixAllScope.Document, codeFixIndex, allowNewCompilerDiagnostics, allowUnsafeCode: false, validationMode: validationMode);
        }

        protected void VerifyBasicFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, FixAllScope? testFixAllScope = FixAllScope.Document, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFix(LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), GetBasicCodeFixProvider(), new[] { oldSource }, new[] { newSource }, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, testFixAllScope, allowUnsafeCode: false, validationMode: validationMode);
        }

        protected void VerifyBasicFix(string[] oldSources, string[] newSources, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool onlyFixFirstFixableDiagnostic = false, FixAllScope? testFixAllScope = FixAllScope.Project, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFix(LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), GetBasicCodeFixProvider(), oldSources, newSources, codeFixIndex, allowNewCompilerDiagnostics, onlyFixFirstFixableDiagnostic, testFixAllScope, allowUnsafeCode: false, validationMode: validationMode);
        }

        protected void VerifyBasicFixAll(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, TestValidationMode validationMode = DefaultTestValidationMode)
        {
            VerifyFixAll(LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), GetBasicCodeFixProvider(), new[] { oldSource }, new[] { newSource }, FixAllScope.Document, codeFixIndex, allowNewCompilerDiagnostics, allowUnsafeCode: false, validationMode: validationMode);
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
            FixAllScope? testFixAllScope,
            bool allowUnsafeCode,
            TestValidationMode validationMode)
        {
            var runner = new CodeFixRunner(analyzerOpt, codeFixProvider, validationMode);
            Assert.True(oldSources.Length == newSources.Length, "Length of expected and actual sources arrays must match.");
            Document[] documents = CreateDocuments(oldSources, language, allowUnsafeCode: allowUnsafeCode);

            var project = documents.First().Project;
            Solution newSolution;
            if (onlyFixFirstFixableDiagnostic)
            {
                newSolution = runner.ApplySingleFix(project, ImmutableArray<TestAdditionalDocument>.Empty, codeFixIndex ?? 0);
                testFixAllScope = null;
            }
            else
            {
                newSolution = runner.ApplyFixesOneByOne(project.Solution, ImmutableArray<TestAdditionalDocument>.Empty, allowNewCompilerDiagnostics, codeFixIndex ?? 0);
            }

            VerifyDocuments(newSolution, documents, newSources);

            if (TestFixAllByDefault && testFixAllScope.HasValue)
            {
                VerifyFixAll(runner, documents, newSources, testFixAllScope.Value, codeFixIndex, allowNewCompilerDiagnostics);
            }
        }

        private void VerifyFixAll(
            string language,
            DiagnosticAnalyzer analyzerOpt,
            CodeFixProvider codeFixProvider,
            string[] oldSources,
            string[] newSources,
            FixAllScope fixAllScope,
            int? codeFixIndex,
            bool allowNewCompilerDiagnostics,
            bool allowUnsafeCode,
            TestValidationMode validationMode)
        {
            var runner = new CodeFixRunner(analyzerOpt, codeFixProvider, validationMode);
            Assert.True(oldSources.Length == newSources.Length, "Length of expected and actual sources arrays must match.");
            Document[] documents = CreateDocuments(oldSources, language, allowUnsafeCode: allowUnsafeCode);
            VerifyFixAll(runner, documents, newSources, fixAllScope, codeFixIndex, allowNewCompilerDiagnostics);
        }

        private static void VerifyFixAll(CodeFixRunner runner, Document[] documents, string[] newSources, FixAllScope fixAllScope, int? codeFixIndex, bool allowNewCompilerDiagnostics)
        {
            var solution = documents.First().Project.Solution;
            var newSolution = runner.ApplyFixAll(solution, fixAllScope, allowNewCompilerDiagnostics, codeFixIndex ?? 0);
            VerifyDocuments(newSolution, documents, newSources);
        }

        protected static void VerifyAdditionalFileFix(
            string language,
            DiagnosticAnalyzer analyzerOpt,
            CodeFixProvider codeFixProvider,
            string source,
            IEnumerable<TestAdditionalDocument> additionalFiles,
            TestAdditionalDocument newAdditionalFileToVerify,
            int? codeFixIndex = null,
            bool allowNewCompilerDiagnostics = false,
            bool onlyFixFirstFixableDiagnostic = false,
            TestValidationMode validationMode = DefaultTestValidationMode)
        {
            Document document = CreateDocument(source, language);
            if (additionalFiles != null)
            {
                var project = document.Project;
                foreach (var additionalFile in additionalFiles)
                {
                    project = project.AddAdditionalDocument(additionalFile.Name, additionalFile.GetText(), filePath: additionalFile.Path).Project;
                }

                document = project.GetDocument(document.Id);
            }

            var additionalFileText = newAdditionalFileToVerify.GetText().ToString();

            Solution newSolution;
            var runner = new CodeFixRunner(analyzerOpt, codeFixProvider, validationMode);
            if (onlyFixFirstFixableDiagnostic || codeFixIndex.HasValue)
            {
                newSolution = runner.ApplySingleFix(document.Project, additionalFiles, codeFixIndex ?? 0);
            }
            else
            {
                newSolution = runner.ApplyFixesOneByOne(document.Project.Solution, additionalFiles, allowNewCompilerDiagnostics, codeFixIndex ?? 0);
            }

            Assert.Equal(additionalFileText, GetActualTextForNewDocument(newSolution.GetDocument(document.Id), newAdditionalFileToVerify.Name).ToString());
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

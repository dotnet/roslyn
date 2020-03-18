﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService
{
    [UseExportProvider]
    public class CodeRefactoringServiceTest
    {
        [Fact]
        public async Task TestExceptionInComputeRefactorings()
        {
            await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInCodeActions>();
        }

        [Fact]
        public async Task TestExceptionInComputeRefactoringsAsync()
        {
            await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInComputeRefactoringsAsync>();
        }

        [Fact]
        public async Task TestProjectRefactoringAsync()
        {
            var code = @"
    a
";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var refactoringService = workspace.GetService<ICodeRefactoringService>();

            var reference = new StubAnalyzerReference();
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            var document = project.Documents.Single();
            var refactorings = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);

            var stubRefactoringAction = refactorings.Single(refactoring => refactoring.CodeActions.FirstOrDefault().action?.Title == nameof(StubRefactoring));
            Assert.True(stubRefactoringAction is object);
        }

        private async Task VerifyRefactoringDisabledAsync<T>()
            where T : CodeRefactoringProvider
        {
            var exportProvider = ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(T))).CreateExportProvider();
            using var workspace = TestWorkspace.CreateCSharp(@"class Program {}", exportProvider: exportProvider);
            var refactoringService = workspace.GetService<ICodeRefactoringService>();
            var codeRefactoring = exportProvider.GetExportedValues<CodeRefactoringProvider>().OfType<T>().Single();

            var project = workspace.CurrentSolution.Projects.Single();
            var document = project.Documents.Single();
            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>() as EditorLayerExtensionManager.ExtensionManager;
            var result = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(codeRefactoring));
            Assert.False(extensionManager.IsIgnored(codeRefactoring));
        }

        internal class StubRefactoring : CodeRefactoringProvider
        {
            public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                context.RegisterRefactoring(CodeAction.Create(
                    nameof(StubRefactoring),
                    cancellationToken => Task.FromResult(context.Document),
                    equivalenceKey: nameof(StubRefactoring)));

                return Task.CompletedTask;
            }
        }

        private class StubAnalyzerReference : AnalyzerReference, ICodeRefactoringProviderFactory
        {
            public readonly CodeRefactoringProvider Refactoring;

            public StubAnalyzerReference()
            {
                Refactoring = new StubRefactoring();
            }

            public StubAnalyzerReference(CodeRefactoringProvider codeRefactoring)
            {
                Refactoring = codeRefactoring;
            }

            public override string Display => nameof(StubAnalyzerReference);

            public override string FullPath => string.Empty;

            public override object Id => nameof(StubAnalyzerReference);

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
                => ImmutableArray<DiagnosticAnalyzer>.Empty;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
                => ImmutableArray<DiagnosticAnalyzer>.Empty;

            public ImmutableArray<CodeRefactoringProvider> GetRefactorings()
                => ImmutableArray.Create(Refactoring);
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService
{
    [UseExportProvider]
    public class CodeRefactoringServiceTest
    {
        [Fact]
        public async Task TestExceptionInComputeRefactorings()
            => await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInCodeActions>();

        [Fact]
        public async Task TestExceptionInComputeRefactoringsAsync()
            => await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInComputeRefactoringsAsync>();

        [Fact]
        public async Task TestProjectRefactoringAsync()
        {
            var code = @"
    a
";

            using var workspace = TestWorkspace.CreateCSharp(code, composition: FeaturesTestCompositions.Features);
            var refactoringService = workspace.GetService<ICodeRefactoringService>();

            var reference = new StubAnalyzerReference();
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            var document = project.Documents.Single();
            var refactorings = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, isBlocking: false, CancellationToken.None);

            var stubRefactoringAction = refactorings.Single(refactoring => refactoring.CodeActions.FirstOrDefault().action?.Title == nameof(StubRefactoring));
            Assert.True(stubRefactoringAction is object);
        }

        [ExportCodeRefactoringProvider(InternalLanguageNames.TypeScript, Name = "TypeScript CodeRefactoring Provider"), Shared, PartNotDiscoverable]
        internal sealed class TypeScriptCodeRefactoringProvider : CodeRefactoringProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TypeScriptCodeRefactoringProvider()
            {
            }

            public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
#pragma warning disable CS0612 // Type or member is obsolete
                var isBlocking = ((ITypeScriptCodeRefactoringContext)context).IsBlocking;
#pragma warning restore

                context.RegisterRefactoring(CodeAction.Create($"Blocking={isBlocking}", _ => Task.FromResult<Document>(null)));

                return Task.CompletedTask;
            }
        }

        [Theory]
        [CombinatorialData]
        public async Task TestTypeScriptRefactorings(bool isBlocking)
        {
            var composition = FeaturesTestCompositions.Features.AddParts(typeof(TypeScriptCodeRefactoringProvider));

            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""TypeScript"">
        <Document FilePath=""Test"">abc</Document>
    </Project>
</Workspace>", composition: composition);

            var refactoringService = workspace.GetService<ICodeRefactoringService>();

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var optionsProvider = workspace.GlobalOptions.GetCodeActionOptionsProvider();
            var refactorings = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), optionsProvider, isBlocking, CancellationToken.None);
            Assert.Equal($"Blocking={isBlocking}", refactorings.Single().CodeActions.Single().action.Title);
        }

        private static async Task VerifyRefactoringDisabledAsync<T>()
            where T : CodeRefactoringProvider
        {
            using var workspace = TestWorkspace.CreateCSharp(@"class Program {}",
                composition: EditorTestCompositions.EditorFeatures.AddParts(typeof(T)));

            var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();

            var errorReported = false;
            errorReportingService.OnError = message => errorReported = true;

            var refactoringService = workspace.GetService<ICodeRefactoringService>();
            var codeRefactoring = workspace.ExportProvider.GetExportedValues<CodeRefactoringProvider>().OfType<T>().Single();

            var project = workspace.CurrentSolution.Projects.Single();
            var document = project.Documents.Single();
            var extensionManager = (EditorLayerExtensionManager.ExtensionManager)document.Project.Solution.Services.GetRequiredService<IExtensionManager>();
            var result = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, isBlocking: false, CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(codeRefactoring));
            Assert.False(extensionManager.IsIgnored(codeRefactoring));

            Assert.True(errorReported);
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
                => Refactoring = new StubRefactoring();

            public StubAnalyzerReference(CodeRefactoringProvider codeRefactoring)
                => Refactoring = codeRefactoring;

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

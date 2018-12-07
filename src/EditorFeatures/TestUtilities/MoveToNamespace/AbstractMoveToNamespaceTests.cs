// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddImports;
using Microsoft.CodeAnalysis.CSharp.ChangeNamespace;
using Microsoft.CodeAnalysis.CSharp.MoveToNamespace;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Composition;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.AbstractCodeActionOrUserDiagnosticTest;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    [UseExportProvider]
    public abstract class AbstractMoveToNamespaceTests
    {
        private static readonly IExportProviderFactory CSharpExportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic
                    .WithPart(typeof(TestMoveToNamespaceOptionsService))
                    .WithPart(typeof(CSharpMoveToNamespaceService))
                    .WithPart(typeof(CSharpChangeNamespaceService))
                    .WithPart(typeof(Experiments.DefaultExperimentationService))
                    .WithPart(typeof(CSharpAddImportsService))
                    .WithPart(typeof(CSharpRemoveUnnecessaryImportsService)));

        public static Task TestMoveToNamespaceCommandCSharpAsync(
            string markup,
            bool expectedSuccess,
            string expectedNamespace = null,
            string expectedMarkup = null)
        {
            return TestMoveToNamespaceCommandAsync(
                markup,
                expectedSuccess,
                LanguageNames.CSharp,
                expectedNamespace: expectedNamespace,
                expectedMarkup: expectedMarkup);
        }

        public static Task TestMoveToNamespaceCommandVisualBasicAsync(
            string markup,
            bool expectedSuccess,
            string expectedNamespace = null,
            string expectedMarkup = null)
        {
            return TestMoveToNamespaceCommandAsync(
                markup,
                expectedSuccess,
                LanguageNames.VisualBasic,
                expectedNamespace: expectedNamespace,
                expectedMarkup: expectedMarkup);
        }


        public static async Task TestMoveToNamespaceCommandAsync(
            string markup,
            bool expectedSuccess,
            string languageName,
            string expectedNamespace = null,
            string expectedMarkup = null,
            CompilationOptions compilationOptions = null)
        {
            expectedNamespace = expectedNamespace ?? string.Empty;
            var parameters = new TestParameters();
            using (var workspace = CreateWorkspace(markup, languageName, compilationOptions, parameters))
            {
                var testDocument = workspace.Documents.Single(d => d.CursorPosition.HasValue);
                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

                var result = await MoveViaCommandAsync(testDocument, document, expectedNamespace);

                if (expectedSuccess)
                {
                    Assert.True(result.Succeeded);
                    Assert.NotNull(result.UpdatedSolution);
                    Assert.NotNull(result.UpdatedDocumentId);

                    if (expectedMarkup != null)
                    {
                        var updatedDocument = result.UpdatedSolution.GetDocument(result.UpdatedDocumentId);
                        var updatedText = await updatedDocument.GetTextAsync().ConfigureAwait(false);
                        Assert.Equal(expectedMarkup, updatedText.ToString());
                    }
                }
                else
                {
                    Assert.False(result.Succeeded);
                }
            }
        }

        private static TestWorkspace CreateWorkspace(
            string markup,
            string languageName,
            CompilationOptions compilationOptions,
            TestParameters parameters)
        {
            var exportProviderFactory = GetExportProviderFactory(languageName);
            var exportProvider = exportProviderFactory.CreateExportProvider();

            var workspace = languageName == LanguageNames.CSharp
                ? TestWorkspace.CreateCSharp(markup, exportProvider: exportProvider, compilationOptions: compilationOptions as CSharpCompilationOptions)
                : TestWorkspace.CreateVisualBasic(markup, exportProvider: exportProvider, compilationOptions: compilationOptions);

            workspace.ApplyOptions(parameters.options);

            return workspace;
        }

        private static IExportProviderFactory GetExportProviderFactory(string languageName)
        {
            return languageName == LanguageNames.CSharp
                ? CSharpExportProviderFactory
                : throw new InvalidOperationException("VB is not currently supported");
        }

        private static async Task<MoveToNamespaceResult> MoveViaCommandAsync(
            TestHostDocument testDocument,
            Document document,
            string newNamespace)
        {
            var cancellationToken = CancellationToken.None;
            var moveToNamespaceService = document.GetLanguageService<AbstractMoveToNamespaceService>();

            var analysisResult = await moveToNamespaceService.AnalyzeTypeAtPositionAsync(
                document,
                testDocument.CursorPosition.Value,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await moveToNamespaceService.MoveToNamespaceAsync(
                analysisResult,
                newNamespace,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}

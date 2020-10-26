// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentSymbols;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.DocumentSymbols
{
    public abstract class AbstractDocumentSymbolsServiceTests<TWorkspaceFixture> : TestBase
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        private static readonly TestComposition s_baseComposition = EditorTestCompositions.EditorFeatures.AddExcludedPartTypes(typeof(IDocumentSymbolsService));
        private readonly TestFixtureHelper<TWorkspaceFixture> _fixtureHelper = new();
        private ExportProvider? _lazyExportProvider;

        protected ExportProvider ExportProvider
            => _lazyExportProvider ??= GetComposition().ExportProviderFactory.CreateExportProvider();

        protected virtual TestComposition GetComposition()
            => s_baseComposition.AddParts(GetDocumentSymbolsServicePartType());

        private protected ReferenceCountedDisposable<TWorkspaceFixture> GetOrCreateWorkspaceFixture()
            => _fixtureHelper.GetOrCreateFixture();

        protected abstract Type GetDocumentSymbolsServicePartType();

        protected async Task AssertExpectedContent(string fileContent, string expectedHierarchicalLayout, string expectedNonHierarchicalLayout)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(ExportProvider);

            var document1 = workspaceFixture.Target.UpdateDocument(fileContent, SourceCodeKind.Regular);

            var documentSymbolsService = GetDocumentSymbolsService(document1);

            var actualHierarchicalResult = await documentSymbolsService.GetSymbolsInDocumentAsync(document1, DocumentSymbolsOptions.FullHierarchy, CancellationToken.None);
            var formattedActualHierarchicalResult = AbstractDocumentSymbolsServiceTests<TWorkspaceFixture>.FormatSymbolResult(actualHierarchicalResult);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedHierarchicalLayout, formattedActualHierarchicalResult);

            var actualNonHierarchicalResult = await documentSymbolsService.GetSymbolsInDocumentAsync(document1, DocumentSymbolsOptions.TypesAndMethodsOnly, CancellationToken.None);
            var formattedActualNonHierarchicalResult = AbstractDocumentSymbolsServiceTests<TWorkspaceFixture>.FormatSymbolResult(actualNonHierarchicalResult);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedNonHierarchicalLayout, formattedActualNonHierarchicalResult);
        }

        protected virtual IDocumentSymbolsService GetDocumentSymbolsService(Document document1)
        {
            return document1.Project.GetRequiredLanguageService<IDocumentSymbolsService>();
        }

        private static string FormatSymbolResult(ImmutableArray<DocumentSymbolInfo> results)
        {
            var formatted = new StringBuilder();

            AppendInfo(formatted, results, "");

            return formatted.ToString();

            static void AppendInfo(StringBuilder formatted, ImmutableArray<DocumentSymbolInfo> currentLevel, string leadingText)
            {
                foreach (var info in currentLevel)
                {
                    formatted.Append(leadingText);
                    formatted.AppendLine(info.Symbol.ToTestDisplayString());
                    if (!info.ChildrenSymbols.IsEmpty)
                    {
                        AppendInfo(formatted, info.ChildrenSymbols, leadingText + "  ");
                    }
                }
            }
        }
    }
}

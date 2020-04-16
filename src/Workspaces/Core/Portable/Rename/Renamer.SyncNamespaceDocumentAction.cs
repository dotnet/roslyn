// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Action that will sync the namespace of the document to match the folders property 
        /// of that document
        /// </summary>
        internal sealed class SyncNamespaceDocumentAction : RenameDocumentAction
        {
            private readonly AnalysisResult _analysis;

            private SyncNamespaceDocumentAction(AnalysisResult analysis, ImmutableArray<ErrorResource> errors)
                : base(errors)
            {
                _analysis = analysis;
            }

            public override string GetDescription(CultureInfo? culture)
                => WorkspacesResources.ResourceManager.GetString("Sync_namespace_to_folder_structure", culture ?? WorkspacesResources.Culture)!;

            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, OptionSet _, CancellationToken cancellationToken)
            {
                // If we are modifying the solution, we shouldn't have offered a change if the target namespace
                // could not be determined
                RoslynDebug.AssertNotNull(_analysis.TargetNamespace);

                var solution = document.Project.Solution;
                var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // Don't descend into anything other than top level declarations from the root.
                // ChangeNamespaceService only controls top level declarations right now
                var namespaceDeclarations = syntaxRoot.DescendantNodes(n => !syntaxFacts.IsDeclaration(n)).Where(n => syntaxFacts.IsNamespaceDeclaration(n));

                foreach (var namespaceDeclaration in namespaceDeclarations)
                {
                    solution = await changeNamespaceService.ChangeNamespaceAsync(
                        document,
                        namespaceDeclaration,
                        _analysis.TargetNamespace,
                        cancellationToken).ConfigureAwait(false);

                    document = solution.GetRequiredDocument(document.Id);
                }

                return solution;
            }

            public static Task<SyncNamespaceDocumentAction?> TryCreateAsync(Document document, IReadOnlyList<string> newFolders, CancellationToken _)
            {
                using var _1 = ArrayBuilder<ErrorResource>.GetInstance(out var errors);

                var analysisResult = Analyze(document, newFolders);

                if (analysisResult.HasValue)
                {
                    return Task.FromResult<SyncNamespaceDocumentAction?>(new SyncNamespaceDocumentAction(analysisResult.Value, errors.ToImmutable()));
                }

                return Task.FromResult<SyncNamespaceDocumentAction?>(null);
            }

            private static AnalysisResult? Analyze(Document document, IReadOnlyCollection<string> newFolders)
            {
                // https://github.com/dotnet/roslyn/issues/41841
                // VB implementation is incomplete for sync namespace
                if (document.Project.Language == LanguageNames.CSharp)
                {
                    var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var targetNamespace = changeNamespaceService.TryBuildNamespaceFromFolders(newFolders, syntaxFacts);

                    if (targetNamespace is null)
                    {
                        return null;
                    }

                    return new AnalysisResult(targetNamespace);
                }
                else
                {
                    return null;
                }
            }

            private readonly struct AnalysisResult
            {
                public string TargetNamespace { get; }

                public AnalysisResult(string targetNamespace)
                {
                    TargetNamespace = targetNamespace;
                }
            }
        }

    }
}

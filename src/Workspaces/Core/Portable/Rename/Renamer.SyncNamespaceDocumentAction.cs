// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Action that will sync the namespace of the document to match the folders property 
        /// of that document
        /// </summary>
        internal class SyncNamespaceDocumentAction : RenameDocumentAction
        {
            private readonly AnalysisResult _analysis;

            private SyncNamespaceDocumentAction(AnalysisResult analysis, OptionSet optionSet, ImmutableArray<ErrorResource> errors)
                : base(errors, optionSet)
            {
                _analysis = analysis;
            }

            public override string GetDescription(CultureInfo? culture)
             => WorkspacesResources.ResourceManager.GetString("Sync_namespace_to_folder_structure", culture ?? WorkspacesResources.Culture)!;

            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, CancellationToken cancellationToken)
            {
                var solution = document.Project.Solution;

                if (!_analysis.SupportsSyncNamespace)
                {
                    return solution;
                }

                var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();

                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var targetNamespace = BuildNamespaceFromFolders(document.Folders, syntaxFacts);

                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // Don't descend into anything other than top level declarations from the root.
                // ChangeNamespaceService only controls top level declarations right now
                var namespaceDeclarations = syntaxRoot.DescendantNodes(n => !syntaxFacts.IsDeclaration(n)).Where(n => syntaxFacts.IsNamespaceDeclaration(n));

                foreach (var namespaceDeclaration in namespaceDeclarations)
                {
                    solution = await changeNamespaceService.ChangeNamespaceAsync(
                        document,
                        namespaceDeclaration,
                        targetNamespace,
                        cancellationToken).ConfigureAwait(false);

                    document = solution.GetRequiredDocument(document.Id);
                }

                return solution;

                static string BuildNamespaceFromFolders(
                    IEnumerable<string> folders,
                    ISyntaxFacts syntaxFacts)
                {
                    var parts = folders.SelectMany(folder => folder.Split(new[] { '.' }));

                    // string.Empty is used to represent the global namespace
                    // when calling ChangeNamespaceAsync
                    return parts.All(syntaxFacts.IsValidIdentifier) ? string.Join(".", parts) : string.Empty;
                }
            }

            public static Task<SyncNamespaceDocumentAction> CreateAsync(Document document, IReadOnlyList<string> newFolders, OptionSet optionSet, CancellationToken _)
            {
                var errors = new ArrayBuilder<ErrorResource>();

                var analysisResult = Analyze(document);

                if (!analysisResult.SupportsSyncNamespace)
                {
                    errors.Add(new ErrorResource(nameof(WorkspacesResources.The_project_does_not_support_sync_namespace), new object[0]));
                }

                return Task.FromResult(new SyncNamespaceDocumentAction(analysisResult, optionSet, errors.ToImmutableAndFree()));
            }

            private static AnalysisResult Analyze(Document document)
            {
                return new AnalysisResult(
                    supportsSyncNamespace: document.Project.Language == LanguageNames.CSharp);
            }

            private readonly struct AnalysisResult
            {
                public bool SupportsSyncNamespace { get; }

                public AnalysisResult(bool supportsSyncNamespace)
                {
                    SupportsSyncNamespace = supportsSyncNamespace;
                }
            }
        }

    }
}

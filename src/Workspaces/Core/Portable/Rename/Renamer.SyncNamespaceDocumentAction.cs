// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Action that will sync the namespace of the document to match the folders property 
        /// of that document, similar to if a user performed the "Sync Namespace" code refactoring.
        /// 
        /// For example, if a document is moved from "Bat/Bar/Baz" folder structure to "Bat/Bar/Baz/Bat" and contains
        /// a namespace definition of Bat.Bar.Baz in the document, then it would update that definition to 
        /// Bat.Bar.Baz.Bat and update the solution to reflect these changes. Uses <see cref="IChangeNamespaceService"/>
        /// </summary>
        internal sealed class SyncNamespaceDocumentAction : RenameDocumentAction
        {
            private readonly AnalysisResult _analysis;
            private readonly CodeCleanupOptionsProvider _fallbackOptions;

            private SyncNamespaceDocumentAction(AnalysisResult analysis, CodeCleanupOptionsProvider fallbackOptions)
                : base(ImmutableArray<ErrorResource>.Empty)
            {
                _analysis = analysis;
                _fallbackOptions = fallbackOptions;
            }

            public override string GetDescription(CultureInfo? culture)
                => WorkspacesResources.ResourceManager.GetString("Sync_namespace_to_folder_structure", culture ?? WorkspacesResources.Culture)!;

            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, DocumentRenameOptions options, CancellationToken cancellationToken)
            {
                var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();
                var solution = await changeNamespaceService.TryChangeTopLevelNamespacesAsync(document, _analysis.TargetNamespace, _fallbackOptions, cancellationToken).ConfigureAwait(false);

                // If the solution fails to update fail silently. The user will see no large
                // negative impact from not doing this modification, and it's possible the document
                // was too malformed to update any namespaces.
                return solution ?? document.Project.Solution;
            }

            public static SyncNamespaceDocumentAction? TryCreate(Document document, IReadOnlyList<string> newFolders, CodeCleanupOptionsProvider fallbackOptions)
            {
                var analysisResult = Analyze(document, newFolders);

                if (analysisResult.HasValue)
                {
                    return new SyncNamespaceDocumentAction(analysisResult.Value, fallbackOptions);
                }

                return null;
            }

            private static AnalysisResult? Analyze(Document document, IReadOnlyCollection<string> newFolders)
            {
                // https://github.com/dotnet/roslyn/issues/41841
                // VB implementation is incomplete for sync namespace
                if (document.Project.Language == LanguageNames.CSharp)
                {
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var targetNamespace = PathMetadataUtilities.TryBuildNamespaceFromFolders(newFolders, syntaxFacts, document.Project.DefaultNamespace);

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Action that will sync the namespace of the document to match the folders property 
        /// of that document, similar to if a user performed the "Sync Namespace" code refactoring.
        /// 
        /// For example, if a document is moved from "Foo/Bar/Baz" folder structure to "Foo/Bar/Baz/Bat" and contains
        /// a namespace definition of Foo.Bar.Baz in the document, then it would update that definition to 
        /// Foo.Bar.Baz.Bat and update the solution to reflect these changes. Uses <see cref="IChangeNamespaceService"/>
        /// </summary>
        internal sealed class SyncNamespaceDocumentAction : RenameDocumentAction
        {
            private readonly AnalysisResult _analysis;

            private SyncNamespaceDocumentAction(AnalysisResult analysis)
                : base(ImmutableArray<ErrorResource>.Empty)
            {
                _analysis = analysis;
            }

            public override string GetDescription(CultureInfo? culture)
                => WorkspacesResources.ResourceManager.GetString("Sync_namespace_to_folder_structure", culture ?? WorkspacesResources.Culture)!;

            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, OptionSet _, CancellationToken cancellationToken)
            {
                var solution = document.Project.Solution;
                var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();
                return await changeNamespaceService.ChangeTopLevelNamespacesAsync(document, _analysis.TargetNamespace, cancellationToken).ConfigureAwait(false);
            }

            public static SyncNamespaceDocumentAction? TryCreate(Document document, IReadOnlyList<string> newFolders, CancellationToken _)
            {
                var analysisResult = Analyze(document, newFolders);

                if (analysisResult.HasValue)
                {
                    return new SyncNamespaceDocumentAction(analysisResult.Value);
                }

                return null;
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

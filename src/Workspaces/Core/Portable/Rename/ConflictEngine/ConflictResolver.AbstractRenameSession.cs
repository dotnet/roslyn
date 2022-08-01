// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal partial class ConflictResolver
    {
        private abstract class AbstractRenameSession
        {
            /// <summary>
            /// The method determines the set of documents that need to be processed for Rename and also determines
            ///  the possible set of names that need to be checked for conflicts.
            /// </summary>
            protected static async Task FindDocumentsAndPossibleNameConflictsAsync(ISymbol renameSymbol)
            {
                try
                {
                    var symbol = _renameLocationSet.Symbol;
                    var solution = _renameLocationSet.Solution;
                    var dependencyGraph = solution.GetProjectDependencyGraph();
                    _topologicallySortedProjects = dependencyGraph.GetTopologicallySortedProjects(_cancellationToken).ToList();

                    var allRenamedDocuments = _renameLocationSet.Locations.Select(loc => loc.Location.SourceTree!).Distinct().Select(solution.GetRequiredDocument);
                    _documentsIdsToBeCheckedForConflict.AddRange(allRenamedDocuments.Select(d => d.Id));

                    var documentsFromAffectedProjects = RenameUtilities.GetDocumentsAffectedByRename(symbol, solution, _renameLocationSet.Locations);
                    foreach (var language in documentsFromAffectedProjects.Select(d => d.Project.Language).Distinct())
                    {
                        solution.Workspace.Services.GetLanguageServices(language).GetService<IRenameRewriterLanguageService>()
                            ?.TryAddPossibleNameConflicts(symbol, _replacementText, _possibleNameConflicts);
                    }

                    await AddDocumentsWithPotentialConflictsAsync(documentsFromAffectedProjects).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task AddDocumentsWithPotentialConflictsAsync(IEnumerable<Document> documents)
            {
                try
                {
                    foreach (var document in documents)
                    {
                        if (_documentsIdsToBeCheckedForConflict.Contains(document.Id))
                            continue;

                        if (!document.SupportsSyntaxTree)
                            continue;

                        var info = await SyntaxTreeIndex.GetRequiredIndexAsync(document, _cancellationToken).ConfigureAwait(false);
                        if (info.ProbablyContainsEscapedIdentifier(_originalText))
                        {
                            _documentsIdsToBeCheckedForConflict.Add(document.Id);
                            continue;
                        }

                        if (info.ProbablyContainsIdentifier(_replacementText))
                        {
                            _documentsIdsToBeCheckedForConflict.Add(document.Id);
                            continue;
                        }

                        foreach (var replacementName in _possibleNameConflicts)
                        {
                            if (info.ProbablyContainsIdentifier(replacementName))
                            {
                                _documentsIdsToBeCheckedForConflict.Add(document.Id);
                                break;
                            }
                        }
                    }
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
            protected static async Task<(ImmutableHashSet<DocumentId> documentsIdsToBeCheckedForConflict, ImmutableArray<string> possibleNameConflicts)> FindDocumentsAndPossibleNameConflictsAsync(
                SymbolicRenameLocations renameLocations,
                string replacementText,
                string originalText,
                CancellationToken cancellationToken)
            {
                try
                {
                    var symbol = renameLocations.Symbol;
                    var solution = renameLocations.Solution;

                    var allRenamedDocuments = renameLocations.Locations.Select(loc => loc.Location.SourceTree!).Distinct().Select(solution.GetRequiredDocument);
                    using var _ = PooledHashSet<DocumentId>.GetInstance(out var documentsIdsToBeCheckedForConflictBuilder);
                    documentsIdsToBeCheckedForConflictBuilder.AddRange(allRenamedDocuments.Select(d => d.Id));
                    var documentsFromAffectedProjects = RenameUtilities.GetDocumentsAffectedByRename(
                        symbol,
                        solution,
                        renameLocations.Locations);

                    var possibleNameConflicts = new List<string>();
                    foreach (var language in documentsFromAffectedProjects.Select(d => d.Project.Language).Distinct())
                    {
                        solution.Workspace.Services.GetLanguageServices(language).GetService<IRenameRewriterLanguageService>()
                            ?.TryAddPossibleNameConflicts(symbol, replacementText, possibleNameConflicts);
                    }

                    await AddDocumentsWithPotentialConflictsAsync(
                        documentsFromAffectedProjects,
                        replacementText,
                        originalText,
                        documentsIdsToBeCheckedForConflictBuilder,
                        possibleNameConflicts,
                        cancellationToken).ConfigureAwait(false);

                    return (documentsIdsToBeCheckedForConflictBuilder.ToImmutableHashSet(), possibleNameConflicts.ToImmutableArray());
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private static async Task AddDocumentsWithPotentialConflictsAsync(
                IEnumerable<Document> documents,
                string replacementText,
                string originalText,
                PooledHashSet<DocumentId> documentsIdsToBeCheckedForConflictBuilder,
                List<string> possibleNameConflicts,
                CancellationToken cancellationToken)
            {
                try
                {
                    foreach (var document in documents)
                    {
                        if (documentsIdsToBeCheckedForConflictBuilder.Contains(document.Id))
                            continue;

                        if (!document.SupportsSyntaxTree)
                            continue;

                        var info = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                        if (info.ProbablyContainsEscapedIdentifier(originalText))
                        {
                            documentsIdsToBeCheckedForConflictBuilder.Add(document.Id);
                            continue;
                        }

                        if (info.ProbablyContainsIdentifier(replacementText))
                        {
                            documentsIdsToBeCheckedForConflictBuilder.Add(document.Id);
                            continue;
                        }

                        foreach (var replacementName in possibleNameConflicts)
                        {
                            if (info.ProbablyContainsIdentifier(replacementName))
                            {
                                documentsIdsToBeCheckedForConflictBuilder.Add(document.Id);
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

            protected static bool IsIdentifierValid_Worker(Solution solution, string replacementText, IEnumerable<ProjectId> projectIds)
            {
                foreach (var language in projectIds.Select(p => solution.GetRequiredProject(p).Language).Distinct())
                {
                    var languageServices = solution.Workspace.Services.GetLanguageServices(language);
                    var renameRewriterLanguageService = languageServices.GetRequiredService<IRenameRewriterLanguageService>();
                    var syntaxFactsLanguageService = languageServices.GetRequiredService<ISyntaxFactsService>();
                    if (!renameRewriterLanguageService.IsIdentifierValid(replacementText, syntaxFactsLanguageService))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}

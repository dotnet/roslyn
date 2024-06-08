// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal static class WorkspaceDiagnosticSourceHelpers
{
    public static IEnumerable<Project> GetProjectsInPriorityOrder(Solution solution, ImmutableArray<string> supportedLanguages)
    {
        return GetProjectsInPriorityOrderWorker(solution)
            .WhereNotNull()
            .Distinct()
            .Where(p => supportedLanguages.Contains(p.Language));

        static IEnumerable<Project?> GetProjectsInPriorityOrderWorker(Solution solution)
        {
            var documentTrackingService = solution.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all projects
            // (depending on current FSA settings).

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            yield return activeDocument?.Project;
            foreach (var doc in visibleDocuments)
                yield return doc.Project;

            foreach (var project in solution.Projects)
                yield return project;
        }
    }

    public static bool ShouldSkipDocument(RequestContext context, TextDocument document)
    {
        // Only consider closed documents here (and only open ones in the DocumentPullDiagnosticHandler).
        // Each handler treats those as separate worlds that they are responsible for.
        if (context.IsTracking(document.GetURI()))
        {
            context.TraceInformation($"Skipping tracked document: {document.GetURI()}");
            return true;
        }

        // Do not attempt to get workspace diagnostics for Razor files, Razor will directly ask us for document diagnostics
        // for any razor file they are interested in.
        return document.IsRazorDocument();
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class SolutionChangeSummary
    {
        public readonly Solution OldSolution;
        public readonly Solution NewSolution;

        public readonly int TotalFilesAffected;
        public readonly int TotalProjectsAffected;

        public SolutionChangeSummary(Solution oldSolution, Solution newSolution, SolutionChanges changes)
        {
            OldSolution = oldSolution;
            NewSolution = newSolution;

            foreach (var p in changes.GetProjectChanges())
            {
                TotalProjectsAffected += 1;

                TotalFilesAffected += p.GetAddedDocuments().Count() +
                                      p.GetChangedDocuments().Count() +
                                      p.GetRemovedDocuments().Count() +
                                      p.GetAddedAdditionalDocuments().Count() +
                                      p.GetChangedAdditionalDocuments().Count() +
                                      p.GetRemovedAdditionalDocuments().Count() +
                                      p.GetAddedAnalyzerConfigDocuments().Count() +
                                      p.GetChangedAnalyzerConfigDocuments().Count() +
                                      p.GetRemovedAnalyzerConfigDocuments().Count();

                if (p.GetAddedDocuments().Any() || p.GetRemovedDocuments().Any() ||
                    p.GetAddedAdditionalDocuments().Any() || p.GetRemovedAdditionalDocuments().Any() ||
                    p.GetAddedAnalyzerConfigDocuments().Any() || p.GetRemovedAnalyzerConfigDocuments().Any() ||
                    p.GetAddedMetadataReferences().Any() || p.GetRemovedMetadataReferences().Any() ||
                    p.GetAddedProjectReferences().Any() || p.GetRemovedProjectReferences().Any() ||
                    p.GetAddedAnalyzerReferences().Any() || p.GetRemovedAnalyzerReferences().Any())
                {
                    TotalFilesAffected += 1;  // The project file itself was affected too.
                }
            }

            var totalProjectsAddedOrRemoved = changes.GetAddedProjects().Count() + changes.GetRemovedProjects().Count();

            TotalFilesAffected += totalProjectsAddedOrRemoved;
            TotalProjectsAffected += totalProjectsAddedOrRemoved;
        }
    }
}

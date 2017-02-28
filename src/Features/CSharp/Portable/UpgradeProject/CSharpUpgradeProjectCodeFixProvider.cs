// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UpgradeProject;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UpgradeProject
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUpgradeProjectCodeFixProvider : AbstractUpgradeProjectCodeFixProvider
    {
        private const string CS8022 = nameof(CS8022); // error CS8022: Feature is not available in C# 1.  Please use language version X or greater.
        private const string CS8023 = nameof(CS8023); // error CS8023: Feature is not available in C# 2.  Please use language version X or greater.
        private const string CS8024 = nameof(CS8024); // error CS8024: Feature is not available in C# 3.  Please use language version X or greater.
        private const string CS8025 = nameof(CS8025); // error CS8025: Feature is not available in C# 4.  Please use language version X or greater.
        private const string CS8026 = nameof(CS8026); // error CS8026: Feature is not available in C# 5.  Please use language version X or greater.
        private const string CS8059 = nameof(CS8059); // error CS8059: Feature is not available in C# 6.  Please use language version X or greater.

        private static readonly ImmutableArray<string> s_diagnostics = 
            ImmutableArray.Create(CS8022, CS8023, CS8024, CS8025, CS8026, CS8059);
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = s_diagnostics;

        public override string UpgradeThisProjectResource => CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0;
        public override string UpgradeAllProjectsResource => CSharpFeaturesResources.Upgrade_all_projects_to_csharp_language_version_0;

        public override string SuggestedVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            return diagnostics.Select(d => SuggestedVersion(d.Id)).First().ToDisplayString();
        }

        private LanguageVersion SuggestedVersion(string id)
        {
            switch (id)
            {
                case CS8022:
                case CS8023:
                case CS8024:
                case CS8025:
                case CS8026:
                case CS8059:
                    return LanguageVersion.Default;
                default:
                    return LanguageVersion.Latest;
            }
        }

        public override Solution UpgradeProject(Solution solution, ProjectId projectId, string version)
        {
            var project = solution.GetProject(projectId);
            var parseOptions = (CSharpParseOptions)project.ParseOptions;

            return solution.WithProjectParseOptions(projectId,
                parseOptions.WithLanguageVersion(parseOptions.LanguageVersion.WithLanguageVersion(version)));
        }

        public override Solution UpgradeAllProjects(Solution solution, string version)
        {
            var currentSolution = solution;
            foreach (var project in solution.Projects)
            {
                if (project.Language == LanguageNames.CSharp)
                {
                    currentSolution = UpgradeProject(currentSolution, project.Id, version);
                }
            }

            return currentSolution;
        }

        public override bool CouldBeUpgradedToo(Project project)
        {
            return project.Language == LanguageNames.CSharp;
        }
    }
}

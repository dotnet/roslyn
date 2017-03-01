// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UpgradeProject;

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

        public override IEnumerable<string> SuggestedVersions(ImmutableArray<Diagnostic> diagnostics)
        {
            var required = RequiredVersion(diagnostics);

            var builder = ArrayBuilder<string>.GetInstance(1);

            var generic = required <= LanguageVersion.Default.MapSpecifiedToEffectiveVersion()
               ? LanguageVersion.Default // for all versions prior to current Default
               : LanguageVersion.Latest; // for more recent versions

            builder.Add(generic.ToDisplayString());

            // also suggest the specific required version
            builder.Add(required.ToDisplayString());

            return builder.ToImmutableAndFree();
        }

        private LanguageVersion RequiredVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            LanguageVersion max = 0;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Properties.TryGetValue(DiagnosticPropertyConstants.RequiredLanguageVersion, out string requiredVersion) &&
                    LanguageVersion.Default.TryParseDisplayString(requiredVersion, out var required))
                {
                    max = max > required ? max : required;
                }
            }

            return max;
        }

        public override Solution UpgradeProject(Solution solution, ProjectId projectId, string version)
        {
            var project = solution.GetProject(projectId);
            var parseOptions = (CSharpParseOptions)project.ParseOptions;
            LanguageVersion.Default.TryParseDisplayString(version, out var newVersion);

            return solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(newVersion));
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

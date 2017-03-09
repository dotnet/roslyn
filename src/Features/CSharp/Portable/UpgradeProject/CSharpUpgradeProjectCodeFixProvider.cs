// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
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

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS8022, CS8023, CS8024, CS8025, CS8026, CS8059);

        public override string UpgradeThisProjectResource => CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0;
        public override string UpgradeAllProjectsResource => CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0;

        public override ImmutableArray<string> SuggestedVersions(ImmutableArray<Diagnostic> diagnostics)
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

        private static LanguageVersion RequiredVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            LanguageVersion max = 0;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Properties.TryGetValue(DiagnosticPropertyConstants.RequiredLanguageVersion, out string requiredVersion) &&
                    CSharpParseOptions.TryParseLanguageVersion(requiredVersion, out var required))
                {
                    max = max > required ? max : required;
                }
            }

            return max;
        }

        public override Solution UpgradeProject(Project project, string version)
        {
            var parseOptions = (CSharpParseOptions)project.ParseOptions;
            Contract.ThrowIfFalse(CSharpParseOptions.TryParseLanguageVersion(version, out var newVersion));

            if (parseOptions.LanguageVersion.ToDisplayString() != version &&
                newVersion.MapSpecifiedToEffectiveVersion() >= parseOptions.LanguageVersion)
            {
                return project.Solution.WithProjectParseOptions(project.Id, parseOptions.WithLanguageVersion(newVersion));
            }
            else
            {
                // when fixing all projects in a solution, don't downgrade those with newer language versions
                return project.Solution;
            }
        }
    }
}

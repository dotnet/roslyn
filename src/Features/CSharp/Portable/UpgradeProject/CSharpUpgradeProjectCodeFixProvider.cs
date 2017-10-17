// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.UpgradeProject;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UpgradeProject
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUpgradeProjectCodeFixProvider : AbstractUpgradeProjectCodeFixProvider
    {
<<<<<<< HEAD
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            new[]
            {
                "CS8022", // error CS8022: Feature is not available in C# 1. Please use language version X or greater.
                "CS8023", // error CS8023: Feature is not available in C# 2. Please use language version X or greater.
                "CS8024", // error CS8024: Feature is not available in C# 3. Please use language version X or greater.
                "CS8025", // error CS8025: Feature is not available in C# 4. Please use language version X or greater.
                "CS8026", // error CS8026: Feature is not available in C# 5. Please use language version X or greater.
                "CS8059", // error CS8059: Feature is not available in C# 6. Please use language version X or greater.
                "CS8107", // error CS8059: Feature is not available in C# 7.0. Please use language version X or greater.
                "CS8302", // error CS8302: Feature is not available in C# 7.1. Please use language version X or greater.
                "CS8306", // error CS8306: ... Please use language version 7.1 or greater to access a un-named element by its inferred name.
                "CS8314", // error CS9003: An expression of type '{0}' cannot be handled by a pattern of type '{1}' in C# {2}. Please use language version {3} or greater.
                "CS8320", // error CS8320: Feature is not available in C# 7.2. Please use language version X or greater.
                "CS8350", // error CS8350: Feature is not available in C# 8.0. Please use language version X or greater.
            });
=======
        private const string CS8022 = nameof(CS8022); // error CS8022: Feature is not available in C# 1. Please use language version X or greater.
        private const string CS8023 = nameof(CS8023); // error CS8023: Feature is not available in C# 2. Please use language version X or greater.
        private const string CS8024 = nameof(CS8024); // error CS8024: Feature is not available in C# 3. Please use language version X or greater.
        private const string CS8025 = nameof(CS8025); // error CS8025: Feature is not available in C# 4. Please use language version X or greater.
        private const string CS8026 = nameof(CS8026); // error CS8026: Feature is not available in C# 5. Please use language version X or greater.
        private const string CS8059 = nameof(CS8059); // error CS8059: Feature is not available in C# 6. Please use language version X or greater.
        private const string CS8107 = nameof(CS8107); // error CS8059: Feature is not available in C# 7.0. Please use language version X or greater.
        private const string CS8302 = nameof(CS8302); // error CS8302: Feature is not available in C# 7.1. Please use language version X or greater.
        private const string CS8306 = nameof(CS8306); // error CS8306: ... Please use language version 7.1 or greater to access a un-named element by its inferred name.
        private const string CS8314 = nameof(CS8314); // error CS9003: An expression of type '{0}' cannot be handled by a pattern of type '{1}' in C# {2}. Please use language version {3} or greater.
        private const string CS8320 = nameof(CS8320); // error CS8320: Feature is not available in C# 7.2. Please use language version X or greater.
        private const string CS1738 = nameof(CS1738); // error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS8022, CS8023, CS8024, CS8025, CS8026, CS8059, CS8107, CS8302, CS8306, CS8314, CS8320, CS1738);
>>>>>>> upstream/master

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
            builder.Add(required.ToDisplayString()); // also suggest the specific required version

            return builder.ToImmutableAndFree();
        }

        private static LanguageVersion RequiredVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            LanguageVersion max = 0;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Properties.TryGetValue(DiagnosticPropertyConstants.RequiredLanguageVersion, out string requiredVersion) &&
                    requiredVersion.TryParse(out var required))
                {
                    max = max > required ? max : required;
                }
            }

            return max;
        }

        public override Solution UpgradeProject(Project project, string newVersion)
        {
            var parseOptions = (CSharpParseOptions)project.ParseOptions;
            if (IsUpgrade(parseOptions, newVersion))
            {
                Contract.ThrowIfFalse(newVersion.TryParse(out var parsedNewVersion));
                return project.Solution.WithProjectParseOptions(project.Id, parseOptions.WithLanguageVersion(parsedNewVersion));
            }
            else
            {
                // when fixing all projects in a solution, don't downgrade those with newer language versions
                return project.Solution;
            }
        }

        public override bool IsUpgrade(ParseOptions projectOptions, string newVersion)
        {
            var parseOptions = (CSharpParseOptions)projectOptions;
            Contract.ThrowIfFalse(newVersion.TryParse(out var parsedNewVersion));

            // treat equivalent versions (one generic and one specific) to be a valid upgrade
            return parsedNewVersion.MapSpecifiedToEffectiveVersion() >= parseOptions.LanguageVersion &&
                parseOptions.SpecifiedLanguageVersion.ToDisplayString() != newVersion;
        }
    }
}

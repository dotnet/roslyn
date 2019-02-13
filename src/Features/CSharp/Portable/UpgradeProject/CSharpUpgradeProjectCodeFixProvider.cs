// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UpgradeProject;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UpgradeProject
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUpgradeProjectCodeFixProvider : AbstractUpgradeProjectCodeFixProvider
    {
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
                "CS1738", // error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                "CS8370", // error CS8370: Feature is not available in C# 7.3. Please use language version X or greater.
                "CS8371", // warning CS8371: Field-targeted attributes on auto-properties are not supported in language version 7.2. Please use language version 7.3 or greater.
                "CS8400", // error CS8400: Feature is not available in C# 8.0. Please use language version X or greater.
                "CS8401", // error CS8401: To use '@$' instead of '$@" for a verbatim interpolated string, please use language version 8.0 or greater.
            });

        public override string UpgradeThisProjectResource => CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0;
        public override string UpgradeAllProjectsResource => CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0;

        public override string SuggestedVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            return RequiredVersion(diagnostics).ToDisplayString();
        }

        public override string AddBetaIfNeeded(string version)
        {
            if (version == "8.0")
            {
                // https://github.com/dotnet/roslyn/issues/29819 Remove once C# 8.0 is RTM
                return "8.0 *beta*";
            }
            return version;
        }

        private static LanguageVersion RequiredVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            LanguageVersion max = 0;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Properties.TryGetValue(DiagnosticPropertyConstants.RequiredLanguageVersion, out string requiredVersion) &&
                    LanguageVersionFacts.TryParse(requiredVersion, out var required))
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
                Contract.ThrowIfFalse(LanguageVersionFacts.TryParse(newVersion, out var parsedNewVersion));
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
            Contract.ThrowIfFalse(LanguageVersionFacts.TryParse(newVersion, out var parsedNewVersion));

            // treat equivalent versions (one generic and one specific) to be a valid upgrade
            return parsedNewVersion.MapSpecifiedToEffectiveVersion() >= parseOptions.LanguageVersion &&
                parseOptions.SpecifiedLanguageVersion.ToDisplayString() != newVersion;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UpgradeProject;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UpgradeProject
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UpgradeProject), Shared]
    internal class CSharpUpgradeProjectCodeFixProvider : AbstractUpgradeProjectCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUpgradeProjectCodeFixProvider()
        {
        }

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
                "CS8314", // error CS8314: An expression of type '{0}' cannot be handled by a pattern of type '{1}' in C# {2}. Please use language version {3} or greater.
                "CS8320", // error CS8320: Feature is not available in C# 7.2. Please use language version X or greater.
                "CS1738", // error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                "CS8370", // error CS8370: Feature is not available in C# 7.3. Please use language version X or greater.
                "CS8371", // warning CS8371: Field-targeted attributes on auto-properties are not supported in language version 7.2. Please use language version 7.3 or greater.
                "CS8400", // error CS8400: Feature is not available in C# 8.0. Please use language version X or greater.
                "CS8401", // error CS8401: To use '@$' instead of '$@" for a verbatim interpolated string, please use language version 8.0 or greater.
                "CS8511", // error CS8511: An expression of type 'T' cannot be handled by a pattern of type '<null>'. Please use language version 'preview' or greater to match an open type with a constant pattern.
                "CS8627", // error CS8627: A nullable type parameter must be known to be a value type or non-nullable reference type unless language version '{0}' or greater is used. Consider changing the language version or adding a 'class', 'struct', or type constraint.
                "CS8652", // error CS8652: The feature '' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                "CS8773", // error CS8773: Feature is not available in C# 9.0. Please use language version X or greater.
                "CS8703", // error CS8703: The modifier '{0}' is not valid for this item in C# {1}. Please use language version '{2}' or greater.
                "CS8706", // error CS8706: '{0}' cannot implement interface member '{1}' in type '{2}' because feature '{3}' is not available in C# {4}. Please use language version '{5}' or greater. 
                "CS8904", // error CS8904: Invalid variance: The type parameter 'T1' must be contravariantly valid on 'I2<T1, T2>.M1(T1)' unless language version 'preview' or greater is used. 'T1' is covariant.
                "CS8912", // error CS8912: Inheriting from a record with a sealed 'Object.ToString' is not supported in C# {0}. Please use language version '{1}' or greater.
                "CS8704", // error CS8704: 'Test1' does not implement interface member 'I1.M1()'. 'Test1.M1()' cannot implicitly implement a non-public member in C# 9.0. Please use language version 'preview' or greater.
                "CS8957", // error CS8957: Conditional expression is not valid in language version '8.0' because a common type was not found between 'int' and '<null>'. To use a target-typed conversion, upgrade to language version '9.0' or greater.
                "CS8967", // error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 8.0. Please use language version preview or greater.

                "CS0171", // error CS0171: Field 'S.Test1' must be fully assigned before control is returned to the caller. Consider updating to language version 'preview' to auto-default the field.
                "CS0188", // error CS0188: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version 'preview' to auto-default the unassigned fields.
                "CS0843", // error CS0843: Auto-implemented property 'S.Test1' must be fully assigned before control is returned to the caller. Consider updating to language version 'preview' to auto-default the property.
                "CS8880", // warning CS8880: Auto-implemented property 'S.Test1' must be fully assigned before control is returned to the caller. Consider updating to language version 'preview' to auto-default the property.
                "CS8881", // warning CS8881: Field 'S.Test1' must be fully assigned before control is returned to the caller. Consider updating to language version 'preview' to auto-default the field.
                "CS8885", // warning CS8885: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version 'preview' to auto-default the unassigned fields.
            });

        public override string UpgradeThisProjectResource => CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0;
        public override string UpgradeAllProjectsResource => CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0;

        public override string SuggestedVersion(ImmutableArray<Diagnostic> diagnostics)
            => RequiredVersion(diagnostics).ToDisplayString();

        private static LanguageVersion RequiredVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            LanguageVersion max = 0;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Properties.TryGetValue(DiagnosticPropertyConstants.RequiredLanguageVersion, out var requiredVersion) &&
                    LanguageVersionFacts.TryParse(requiredVersion, out var required))
                {
                    max = max > required ? max : required;
                }
                else if (diagnostic.Id == "CS8652")
                {
                    max = LanguageVersion.Preview;
                    break;
                }
            }

            return max;
        }

        public override Solution UpgradeProject(Project project, string newVersion)
        {
            if (IsUpgrade(project, newVersion))
            {
                Contract.ThrowIfFalse(LanguageVersionFacts.TryParse(newVersion, out var parsedNewVersion));
                var parseOptions = (CSharpParseOptions)project.ParseOptions!;

                return project.Solution.WithProjectParseOptions(project.Id, parseOptions.WithLanguageVersion(parsedNewVersion));
            }
            else
            {
                // when fixing all projects in a solution, don't downgrade those with newer language versions
                return project.Solution;
            }
        }

        public override bool IsUpgrade(Project project, string newVersion)
        {
            Contract.ThrowIfFalse(LanguageVersionFacts.TryParse(newVersion, out var parsedNewVersion));

            var parseOptions = (CSharpParseOptions)project.ParseOptions!;
            var mappedVersion = parsedNewVersion.MapSpecifiedToEffectiveVersion();

            var workspace = project.Solution.Workspace;

            // treat equivalent versions (one generic and one specific) to be a valid upgrade
            return mappedVersion >= parseOptions.LanguageVersion &&
                parseOptions.SpecifiedLanguageVersion.ToDisplayString() != newVersion &&
                workspace.CanApplyParseOptionChange(parseOptions, parseOptions.WithLanguageVersion(parsedNewVersion), project);
        }
    }
}

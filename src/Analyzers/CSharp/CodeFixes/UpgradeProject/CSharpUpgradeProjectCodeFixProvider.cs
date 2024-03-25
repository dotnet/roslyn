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

namespace Microsoft.CodeAnalysis.CSharp.UpgradeProject;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UpgradeProject), Shared]
internal class CSharpUpgradeProjectCodeFixProvider : AbstractUpgradeProjectCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpUpgradeProjectCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
    [
        "CS8022",
        "CS8023",
        "CS8024",
        "CS8025",
        "CS8026",
        "CS8059",
        "CS8107",
        "CS8302",
        "CS8306",
        "CS8314",
        "CS8320",
        "CS1738",
        "CS8370",
        "CS8371",
        "CS8400",
        "CS8401",
        "CS8511",
        "CS8627",
        "CS8652",
        "CS8773",
        "CS8703",
        "CS8706",
        "CS8904",
        "CS8912",
        "CS8704",
        "CS8957",
        "CS8967",
        "CS0171",
        "CS0188",
        "CS0843",
        "CS8880",
        "CS8881",
        "CS8885",
        "CS8936",
        "CS9058",
        "CS9194",
        "CS9202",
    ];

    public override string UpgradeThisProjectResource => CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0;
    public override string UpgradeAllProjectsResource => CSharpCodeFixesResources.Upgrade_all_csharp_projects_to_language_version_0;

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

        // treat equivalent versions (one generic and one specific) to be a valid upgrade
        return mappedVersion >= parseOptions.LanguageVersion &&
            parseOptions.SpecifiedLanguageVersion.ToDisplayString() != newVersion &&
            project.CanApplyParseOptionChange(parseOptions, parseOptions.WithLanguageVersion(parsedNewVersion));
    }
}

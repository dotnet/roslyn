// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UpgradeProject;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

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

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS8022, CS8023, CS8024, CS8025, CS8026, CS8059);

        protected override ImmutableArray<CodeAction> GetUpgradeProjectCodeActionsAsync(CodeFixContext context)
        {
            var project = context.Document.Project;
            var solution = project.Solution;
            var newVersion = SuggestedVersion(context.Diagnostics);

            var fixOneProjectTitle = string.Format(CSharpFeaturesResources.Upgrade_project_to_csharp_language_version_0,
                newVersion.Display());

            var fixOneProject = new SolutionChangeAction(fixOneProjectTitle, ct => UpgradeProject(project, solution, newVersion));

            var fixAllProjectsTitle = string.Format(CSharpFeaturesResources.Upgrade_all_projects_to_csharp_language_version_0,
                newVersion.Display());

            var fixAllProjects = new SolutionChangeAction(fixAllProjectsTitle, ct => UpgradeProjects(solution, newVersion));

            return new CodeAction[] { fixOneProject, fixAllProjects }.AsImmutable();
        }

        private LanguageVersion SuggestedVersion(ImmutableArray<Diagnostic> diagnostics)
        {
            return diagnostics.Select(d => SuggestedVersion(d.Id)).First();
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

        private static Task<Solution> UpgradeProject(Project project, Solution solution, LanguageVersion version)
        {
            return Task.FromResult(solution.WithProjectParseOptions(project.Id, ((CSharpParseOptions)project.ParseOptions)
                .WithLanguageVersion(version)));
        }

        private async Task<Solution> UpgradeProjects(Solution solution, LanguageVersion version)
        {
            var currentSolution = solution;
            foreach (var project in solution.Projects)
            {
                if (project.Language == LanguageNames.CSharp)
                {
                    currentSolution = await UpgradeProject(project, currentSolution, version).ConfigureAwait(true);
                }
            }

            return currentSolution;
        }
    }

    internal static class LanguageVersionExtensions
    {
        /// <summary>
        /// Displays the version number in the format expected on the command-line (/langver flag).
        /// For instance, "6", "7", "7.1", "latest".
        /// </summary>
        internal static string Display(this LanguageVersion version)
        {
            switch (version)
            {
                case LanguageVersion.CSharp1:
                    return "1";
                case LanguageVersion.CSharp2:
                    return "2";
                case LanguageVersion.CSharp3:
                    return "3";
                case LanguageVersion.CSharp4:
                    return "4";
                case LanguageVersion.CSharp5:
                    return "5";
                case LanguageVersion.CSharp6:
                    return "6";
                case LanguageVersion.CSharp7:
                    return "7";
                case LanguageVersion.Default:
                    return "default";
                case LanguageVersion.Latest:
                    return "latest";
                default:
                    throw ExceptionUtilities.UnexpectedValue(version);
            }
        }
    }
}

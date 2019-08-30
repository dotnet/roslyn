// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.UpgradeProject
{
    internal abstract partial class AbstractUpgradeProjectCodeFixProvider : CodeFixProvider
    {
        public abstract string SuggestedVersion(ImmutableArray<Diagnostic> diagnostics);
        public abstract Solution UpgradeProject(Project project, string version);
        public abstract bool IsUpgrade(Project project, string newVersion);
        public abstract string UpgradeThisProjectResource { get; }
        public abstract string UpgradeAllProjectsResource { get; }

        public override FixAllProvider GetFixAllProvider()
        {
            // This code fix uses a dedicated action for fixing all instances in a solution
            return null;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostics = context.Diagnostics;

            context.RegisterFixes(GetUpgradeProjectCodeActionsAsync(context), diagnostics);
            return Task.CompletedTask;
        }

        protected ImmutableArray<CodeAction> GetUpgradeProjectCodeActionsAsync(CodeFixContext context)
        {
            var project = context.Document.Project;
            var solution = project.Solution;
            var newVersion = SuggestedVersion(context.Diagnostics);

            var result = new List<CodeAction>();
            var language = project.Language;

            var upgradeableProjects = solution.Projects.Where(p => CanUpgrade(p, language, newVersion)).AsImmutable();

            if (upgradeableProjects.Length == 0)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var fixOneProjectTitle = string.Format(UpgradeThisProjectResource, newVersion);
            var fixOneProject = new ProjectOptionsChangeAction(fixOneProjectTitle,
                _ => Task.FromResult(UpgradeProject(project, newVersion)));

            result.Add(fixOneProject);

            if (upgradeableProjects.Length > 1)
            {
                var fixAllProjectsTitle = string.Format(UpgradeAllProjectsResource, newVersion);

                var fixAllProjects = new ProjectOptionsChangeAction(fixAllProjectsTitle,
                    ct => Task.FromResult(UpgradeAllProjects(solution, language, newVersion, ct)));

                result.Add(fixAllProjects);
            }

            return result.AsImmutable();
        }

        public Solution UpgradeAllProjects(Solution solution, string language, string version, CancellationToken cancellationToken)
        {
            var currentSolution = solution;
            foreach (var projectId in solution.Projects.Select(p => p.Id))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentProject = currentSolution.GetProject(projectId);

                if (CanUpgrade(currentProject, language, version))
                {
                    currentSolution = UpgradeProject(currentProject, version);
                }
            }

            return currentSolution;
        }

        private bool CanUpgrade(Project project, string language, string version)
        {
            return project.Language == language && IsUpgrade(project, version);
        }
    }

    internal class ProjectOptionsChangeAction : SolutionChangeAction
    {
        public ProjectOptionsChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
            : base(title, createChangedSolution, equivalenceKey: null)
        {
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<CodeActionOperation>());
    }
}

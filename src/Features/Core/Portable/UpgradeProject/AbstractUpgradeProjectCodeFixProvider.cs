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
        public abstract ImmutableArray<string> SuggestedVersions(ImmutableArray<Diagnostic> diagnostics);
        public abstract Solution UpgradeProject(Project project, string version);
        public abstract string UpgradeThisProjectResource { get; }
        public abstract string UpgradeAllProjectsResource { get; }

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
            var newVersions = SuggestedVersions(context.Diagnostics);
            var result = new List<CodeAction>();
            var language = project.Language;

            foreach (var newVersion in newVersions)
            {
                var fixOneProjectTitle = string.Format(UpgradeThisProjectResource, newVersion);

                var fixOneProject = new ParseOptionsChangeAction(fixOneProjectTitle,
                    _ => Task.FromResult(UpgradeProject(project, newVersion)));

                result.Add(fixOneProject);
            }

            if (solution.Projects.Count(p => p.Language == language) > 1)
            {
                foreach (var newVersion in newVersions)
                {
                    var fixAllProjectsTitle = string.Format(UpgradeAllProjectsResource, newVersion);

                    var fixAllProjects = new ParseOptionsChangeAction(fixAllProjectsTitle,
                        ct => Task.FromResult(UpgradeAllProjects(solution, language, newVersion, ct)));

                    result.Add(fixAllProjects);
                }
            }

            return result.AsImmutable();
        }

        public Solution UpgradeAllProjects(Solution solution, string language, string version, CancellationToken cancellationToken)
        {
            var currentSolution = solution;
            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (project.Language == language)
                {
                    currentSolution = UpgradeProject(currentSolution.GetProject(project.Id), version);
                }
            }

            return currentSolution;
        }
    }

    internal class ParseOptionsChangeAction : SolutionChangeAction
    {
        public ParseOptionsChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
            : base(title, createChangedSolution, equivalenceKey: null)
        {
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<CodeActionOperation>());
    }
}
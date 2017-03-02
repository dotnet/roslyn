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

                CodeAction fixOneProject = new ParseOptionsChangeAction(fixOneProjectTitle,
                    ct => Task.FromResult(UpgradeProject(project, newVersion)));

                result.Add(fixOneProject);
            }

            if (solution.Projects.Count(p => CouldBeUpgraded(p, language)) > 1)
            {
                foreach (var newVersion in newVersions)
                {
                    var fixAllProjectsTitle = string.Format(UpgradeAllProjectsResource, newVersion);

                    CodeAction fixAllProjects = new ParseOptionsChangeAction(fixAllProjectsTitle,
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
                if (CouldBeUpgraded(project, language))
                {
                    currentSolution = UpgradeProject(currentSolution.GetProject(project.Id), version);
                }
            }

            return currentSolution;
        }

        public bool CouldBeUpgraded(Project project, string language)
        {
            return project.Language == language;
        }

        public abstract ImmutableArray<string> SuggestedVersions(ImmutableArray<Diagnostic> diagnostics);

        public abstract string UpgradeThisProjectResource { get; }
        public abstract Solution UpgradeProject(Project project, string version);

        public abstract string UpgradeAllProjectsResource { get; }
    }

    internal class ParseOptionsChangeAction : SolutionChangeAction
    {
        public ParseOptionsChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey = null)
            : base(title, createChangedSolution, equivalenceKey)
        {
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<CodeActionOperation>());
        }
    }
}
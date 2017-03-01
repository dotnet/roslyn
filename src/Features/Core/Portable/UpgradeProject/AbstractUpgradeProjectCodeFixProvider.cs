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

            foreach (var newVersion in newVersions)
            {
                var fixOneProjectTitle = string.Format(UpgradeThisProjectResource, newVersion);

                CodeAction fixOneProject = new ParseOptionsChangeAction(fixOneProjectTitle,
                    ct => Task.FromResult(UpgradeProject(solution, project.Id, newVersion)));

                result.Add(fixOneProject);

                if (solution.Projects.Where(CouldBeUpgradedToo).Count() > 1)
                {
                    var fixAllProjectsTitle = string.Format(UpgradeAllProjectsResource, newVersion);

                    CodeAction fixAllProjects = new ParseOptionsChangeAction(fixAllProjectsTitle,
                        ct => Task.FromResult(UpgradeAllProjects(solution, newVersion)));

                    result.Add(fixAllProjects);
                }
            }

            return result.AsImmutable();
        }

        public abstract IEnumerable<string> SuggestedVersions(ImmutableArray<Diagnostic> diagnostics);
        public abstract bool CouldBeUpgradedToo(Project project);

        public abstract string UpgradeThisProjectResource { get; }
        public abstract Solution UpgradeProject(Solution solution, ProjectId projectId, string version);

        public abstract string UpgradeAllProjectsResource { get; }
        public abstract Solution UpgradeAllProjects(Solution solution, string version);
    }

    internal class ParseOptionsChangeAction : SolutionChangeAction
    {
        public ParseOptionsChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey = null)
            : base(title, createChangedSolution, equivalenceKey)
        {
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<CodeActionOperation>)ImmutableArray<CodeActionOperation>.Empty);
        }
    }
}
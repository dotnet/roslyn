// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.TaskList;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceTaskDiagnosticSourceProvider([Import] IGlobalOptionService globalOptions)
    : AbstractWorkspaceDiagnosticSourceProvider(PullDiagnosticCategories.Task)
{
    public override ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (!ShouldIgnoreContext(context))
        {
            Contract.ThrowIfNull(context.Solution);

            // Only compute task list items for closed files if the option is on for it.
            if (globalOptions.GetTaskListOptions().ComputeForClosedFiles)
            {
                using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);
                foreach (var project in GetProjectsInPriorityOrder(context.Solution, context.SupportedLanguages))
                {
                    foreach (var document in project.Documents)
                    {
                        if (!ShouldSkipDocument(context, document))
                            result.Add(new TaskListDiagnosticSource(document, globalOptions));
                    }
                }

                return new(result.ToImmutableAndClear());
            }
        }

        return new([]);
    }
}

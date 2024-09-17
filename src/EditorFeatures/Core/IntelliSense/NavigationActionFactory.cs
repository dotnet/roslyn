// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense;

internal sealed class NavigationActionFactory(
    Document document,
    IThreadingContext threadingContext,
    IUIThreadOperationExecutor operationExecutor,
    IAsynchronousOperationListener asyncListener,
    Lazy<IStreamingFindUsagesPresenter> streamingPresenter) : INavigationActionFactory
{
    public Action CreateNavigationAction(string navigationTarget)
    {
        // ⚠ PERF: Avoid capturing Solution (including indirectly through Project or Document
        // instances) as part of the navigationAction delegate.
        var workspace = document.Project.Solution.Workspace;
        var documentId = document.Id;

        return () => NavigateToQuickInfoTargetAsync(
            navigationTarget, workspace, documentId, threadingContext, operationExecutor, asyncListener, streamingPresenter.Value).Forget();
    }

    private static async Task NavigateToQuickInfoTargetAsync(
        string navigationTarget,
        Workspace workspace,
        DocumentId documentId,
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor operationExecutor,
        IAsynchronousOperationListener asyncListener,
        IStreamingFindUsagesPresenter streamingPresenter)
    {
        try
        {
            using var token = asyncListener.BeginAsyncOperation(nameof(NavigateToQuickInfoTargetAsync));
            using var context = operationExecutor.BeginExecute(EditorFeaturesResources.IntelliSense, EditorFeaturesResources.Navigating, allowCancellation: true, showProgress: false);

            var cancellationToken = context.UserCancellationToken;
            var solution = workspace.CurrentSolution;
            SymbolKeyResolution resolvedSymbolKey;
            try
            {
                var project = solution.GetRequiredProject(documentId.ProjectId);
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                resolvedSymbolKey = SymbolKey.ResolveString(navigationTarget, compilation, cancellationToken: cancellationToken);
            }
            catch
            {
                // Ignore symbol resolution failures. It likely is just a badly formed URI.
                return;
            }

            if (resolvedSymbolKey.GetAnySymbol() is { } symbol)
            {
                var location = await GoToDefinitionHelpers
                    .GetDefinitionLocationAsync(symbol, solution, threadingContext, streamingPresenter, cancellationToken)
                    .ConfigureAwait(false);

                await location
                    .TryNavigateToAsync(threadingContext, new NavigationOptions(PreferProvisionalTab: true, ActivateTab: true), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
        {
        }
    }
}

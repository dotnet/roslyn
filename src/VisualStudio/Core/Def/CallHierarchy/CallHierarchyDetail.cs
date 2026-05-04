// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

internal sealed class CallHierarchyDetail : ICallHierarchyItemDetails
{
    private readonly CallHierarchyProvider _provider;
    private readonly TextSpan _span;
    private readonly DocumentId _documentId;
    private readonly Workspace _workspace;

    public CallHierarchyDetail(
        CallHierarchyProvider provider,
        Location location,
        Workspace workspace)
    {
        _provider = provider;
        _span = location.SourceSpan;
        _documentId = workspace.CurrentSolution.GetDocumentId(location.SourceTree);
        _workspace = workspace;
        EndColumn = location.GetLineSpan().Span.End.Character;
        EndLine = location.GetLineSpan().EndLinePosition.Line;
        File = location.SourceTree.FilePath;
        StartColumn = location.GetLineSpan().StartLinePosition.Character;
        StartLine = location.GetLineSpan().StartLinePosition.Line;
        Text = ComputeText(location);
    }

    private static string ComputeText(Location location)
    {
        var lineSpan = location.GetLineSpan();
        var start = location.SourceTree.GetText().Lines[lineSpan.StartLinePosition.Line].Start;
        var end = location.SourceTree.GetText().Lines[lineSpan.EndLinePosition.Line].End;
        return location.SourceTree.GetText().GetSubText(TextSpan.FromBounds(start, end)).ToString();
    }

    public string File { get; }
    public string Text { get; }
    public bool SupportsNavigateTo => true;

    public int EndColumn { get; }
    public int EndLine { get; }
    public int StartColumn { get; }
    public int StartLine { get; }

    public void NavigateTo()
    {
        var token = _provider.AsyncListener.BeginAsyncOperation(nameof(NavigateTo));
        NavigateToAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
    }

    private async Task NavigateToAsync()
    {
        using var context = _provider.ThreadOperationExecutor.BeginExecute(
            EditorFeaturesResources.Call_Hierarchy, ServicesVSResources.Navigating, allowCancellation: true, showProgress: false);

        var solution = _workspace.CurrentSolution;
        var document = solution.GetDocument(_documentId);

        if (document == null)
            return;

        var navigator = _workspace.Services.GetService<IDocumentNavigationService>();
        await navigator.TryNavigateToSpanAsync(
            _provider.ThreadingContext, _workspace, document.Id, _span,
            new NavigationOptions(PreferProvisionalTab: true, ActivateTab: false),
            context.UserCancellationToken).ConfigureAwait(false);
    }
}

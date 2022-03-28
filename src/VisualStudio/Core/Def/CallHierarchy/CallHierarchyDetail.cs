// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
{
    internal class CallHierarchyDetail : ICallHierarchyItemDetails
    {
        private readonly CallHierarchyProvider _provider;
        private readonly TextSpan _span;
        private readonly DocumentId _documentId;
        private readonly Workspace _workspace;

        private readonly int _endColumn;
        private readonly int _endLine;
        private readonly string _sourceFile;
        private readonly int _startColumn;
        private readonly int _startLine;
        private readonly string _text;

        public CallHierarchyDetail(
            CallHierarchyProvider provider,
            Location location,
            Workspace workspace)
        {
            _provider = provider;
            _span = location.SourceSpan;
            _documentId = workspace.CurrentSolution.GetDocumentId(location.SourceTree);
            _workspace = workspace;
            _endColumn = location.GetLineSpan().Span.End.Character;
            _endLine = location.GetLineSpan().EndLinePosition.Line;
            _sourceFile = location.SourceTree.FilePath;
            _startColumn = location.GetLineSpan().StartLinePosition.Character;
            _startLine = location.GetLineSpan().StartLinePosition.Line;
            _text = ComputeText(location);
        }

        private static string ComputeText(Location location)
        {
            var lineSpan = location.GetLineSpan();
            var start = location.SourceTree.GetText().Lines[lineSpan.StartLinePosition.Line].Start;
            var end = location.SourceTree.GetText().Lines[lineSpan.EndLinePosition.Line].End;
            return location.SourceTree.GetText().GetSubText(TextSpan.FromBounds(start, end)).ToString();
        }

        public string File => _sourceFile;
        public string Text => _text;
        public bool SupportsNavigateTo => true;

        public int EndColumn => _endColumn;
        public int EndLine => _endLine;
        public int StartColumn => _startColumn;
        public int StartLine => _startLine;

        public void NavigateTo()
        {
            var token = _provider.AsyncListener.BeginAsyncOperation(nameof(NavigateTo));
            NavigateToAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
        }

        private async Task NavigateToAsync()
        {
            using var context = _provider.ThreadOperationExecutor.BeginExecute(
                ServicesVSResources.Call_Hierarchy, ServicesVSResources.Navigating, allowCancellation: true, showProgress: false);

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
}

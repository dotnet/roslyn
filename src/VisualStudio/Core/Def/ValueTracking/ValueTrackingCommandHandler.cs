// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.ShowValueTracking)]
    internal class ValueTrackingCommandHandler : ICommandHandler<ValueTrackingEditorCommandArgs>
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly SVsServiceProvider _serviceProvider1;
        private readonly IThreadingContext _threadingContext;
        private readonly ClassificationTypeMap _typeMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly IGlyphService _glyphService;
        private RoslynPackage? _roslynPackage;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValueTrackingCommandHandler(
            SVsServiceProvider serviceProvider,
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IClassificationFormatMapService classificationFormatMapService,
            IGlyphService glyphService)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
            _serviceProvider1 = serviceProvider;
            _threadingContext = threadingContext;
            _typeMap = typeMap;
            _classificationFormatMapService = classificationFormatMapService;
            _glyphService = glyphService;
        }

        public string DisplayName => "Go to value tracking";

        public CommandState GetCommandState(ValueTrackingEditorCommandArgs args)
            => CommandState.Available;

        public bool ExecuteCommand(ValueTrackingEditorCommandArgs args, CommandExecutionContext executionContext)
        {
            var cancellationToken = executionContext.OperationContext.UserCancellationToken;
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPosition.HasValue)
            {
                return false;
            }

            var textSpan = new TextSpan(caretPosition.Value.Position, 0);
            var sourceTextContainer = args.SubjectBuffer.AsTextContainer();
            var document = sourceTextContainer.GetOpenDocumentInCurrentContext();
            if (document is null)
            {
                return false;
            }

            _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    var selectedSymbol = await GetSelectedSymbolAsync(textSpan, document, cancellationToken).ConfigureAwait(false);
                    if (selectedSymbol is null)
                    {
                        // TODO: Show error dialog
                        return;
                    }

                    var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
                    var location = Location.Create(syntaxTree, textSpan);

                    await ShowToolWindowAsync(args.TextView, selectedSymbol, location, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                });

            return true;
        }

        private static async Task<ISymbol?> GetSelectedSymbolAsync(TextSpan textSpan, Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectedNode = root.FindNode(textSpan);
            if (selectedNode is null)
            {
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedSymbol =
                semanticModel.GetSymbolInfo(selectedNode, cancellationToken).Symbol
                ?? semanticModel.GetDeclaredSymbol(selectedNode, cancellationToken);

            if (selectedSymbol is null)
            {
                return null;
            }

            return selectedSymbol switch
            {
                ILocalSymbol
                or IPropertySymbol { SetMethod: not null }
                or IFieldSymbol { IsReadOnly: false }
                or IEventSymbol
                or IParameterSymbol
                => selectedSymbol,

                _ => null
            };
        }

        private async Task ShowToolWindowAsync(ITextView textView, ISymbol selectedSymbol, Location location, Solution solution, CancellationToken cancellationToken)
        {
            var item = await ValueTrackedItem.TryCreateAsync(solution, location, selectedSymbol, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (item is null)
            {
                return;
            }

            var valueTrackingService = solution.Workspace.Services.GetRequiredService<IValueTrackingService>();
            var classificationFormatMap = _classificationFormatMapService.GetClassificationFormatMap(textView);

            var childItems = await valueTrackingService.TrackValueSourceAsync(item, cancellationToken).ConfigureAwait(false);
            var childViewModels = childItems.SelectAsArray(child => CreateViewModel(child));

            RoslynDebug.AssertNotNull(location.SourceTree);
            var document = solution.GetRequiredDocument(location.SourceTree);

            var sourceText = await location.SourceTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            sourceText.GetLineAndOffset(location.SourceSpan.Start, out var lineStart, out var _);
            sourceText.GetLineAndOffset(location.SourceSpan.End, out var lineEnd, out var _);
            var lineSpan = LineSpan.FromBounds(lineStart, lineEnd);

            var documentSpan = await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(document, location.SourceSpan, cancellationToken).ConfigureAwait(false);
            var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, cancellationToken).ConfigureAwait(false);

            var root = new ValueTrackingTreeItemViewModel(
                document,
                lineSpan,
                sourceText,
                selectedSymbol,
                classificationResult.ClassifiedSpans,
                classificationFormatMap,
                _typeMap,
                _glyphService,
                _threadingContext,
                childViewModels);

            await ShowToolWindowAsync(root, cancellationToken).ConfigureAwait(false);

            ValueTrackingTreeItemViewModel CreateViewModel(ValueTrackedItem valueTrackedItem, ImmutableArray<ValueTrackingTreeItemViewModel> children = default)
                => new ValueTrackedTreeItemViewModel(
                   valueTrackedItem,
                   solution,
                   classificationFormatMap,
                   _typeMap,
                   _glyphService,
                   valueTrackingService,
                   _threadingContext,
                   children);
        }

        private async Task ShowToolWindowAsync(ValueTrackingTreeItemViewModel viewModel, CancellationToken cancellationToken)
        {
            var roslynPackage = await TryGetRoslynPackageAsync(cancellationToken).ConfigureAwait(false);
            if (roslynPackage is null)
            {
                return;
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (ValueTrackingToolWindow.Instance is null)
            {
                var factory = roslynPackage.GetAsyncToolWindowFactory(Guids.ValueTrackingToolWindowId);

                factory.CreateToolWindow(Guids.ValueTrackingToolWindowId, 0, viewModel);
                await factory.InitializeToolWindowAsync(Guids.ValueTrackingToolWindowId, 0);

                ValueTrackingToolWindow.Instance = (ValueTrackingToolWindow)await roslynPackage.ShowToolWindowAsync(
                    typeof(ValueTrackingToolWindow),
                    0,
                    true,
                    roslynPackage.DisposalToken).ConfigureAwait(false);
            }
            else
            {
                ValueTrackingToolWindow.Instance.Root = viewModel;

                await roslynPackage.ShowToolWindowAsync(
                    typeof(ValueTrackingToolWindow),
                    0,
                    false,
                    roslynPackage.DisposalToken).ConfigureAwait(false);
            }
        }

        private async ValueTask<RoslynPackage?> TryGetRoslynPackageAsync(CancellationToken cancellationToken)
        {
            if (_roslynPackage is null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var shell = (IVsShell7?)await _serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
                Assumes.Present(shell);
                await shell.LoadPackageAsync(typeof(RoslynPackage).GUID);

                if (ErrorHandler.Succeeded(((IVsShell)shell).IsPackageLoaded(typeof(RoslynPackage).GUID, out var package)))
                {
                    _roslynPackage = (RoslynPackage)package;
                }
            }

            return _roslynPackage;
        }
    }
}

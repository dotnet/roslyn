// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
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
        private readonly IThreadingContext _threadingContext;
        private readonly ClassificationTypeMap _typeMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly IGlyphService _glyphService;
        private readonly IEditorFormatMapService _formatMapService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValueTrackingCommandHandler(
            SVsServiceProvider serviceProvider,
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IClassificationFormatMapService classificationFormatMapService,
            IGlyphService glyphService,
            IEditorFormatMapService formatMapService)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
            _threadingContext = threadingContext;
            _typeMap = typeMap;
            _classificationFormatMapService = classificationFormatMapService;
            _glyphService = glyphService;
            _formatMapService = formatMapService;
        }

        public string DisplayName => "Go to value tracking";

        public CommandState GetCommandState(ValueTrackingEditorCommandArgs args)
            => CommandState.Available;

        public bool ExecuteCommand(ValueTrackingEditorCommandArgs args, CommandExecutionContext executionContext)
        {
            using var logger = Logger.LogBlock(FunctionId.ValueTracking_Command, CancellationToken.None, LogLevel.Information);

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

            var toolWindow = await GetOrCreateToolWindowAsync(textView, cancellationToken).ConfigureAwait(false);
            if (toolWindow?.ViewModel is null)
            {
                return;
            }

            var valueTrackingService = solution.Workspace.Services.GetRequiredService<IValueTrackingService>();
            var classificationFormatMap = _classificationFormatMapService.GetClassificationFormatMap(textView);

            var childItems = await valueTrackingService.TrackValueSourceAsync(solution, item, cancellationToken).ConfigureAwait(false);
            var childViewModels = childItems.SelectAsArray(child => CreateViewModel(child));

            RoslynDebug.AssertNotNull(location.SourceTree);
            var document = solution.GetRequiredDocument(location.SourceTree);
            var options = ClassificationOptions.From(document.Project);

            var sourceText = await location.SourceTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var documentSpan = await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(document, location.SourceSpan, options, cancellationToken).ConfigureAwait(false);
            var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, options, cancellationToken).ConfigureAwait(false);

            var root = new TreeItemViewModel(
                location.SourceSpan,
                sourceText,
                document.Id,
                document.FilePath ?? document.Name,
                selectedSymbol.GetGlyph(),
                classificationResult.ClassifiedSpans,
                toolWindow.ViewModel,
                _glyphService,
                _threadingContext,
                solution.Workspace,
                children: childViewModels);

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            toolWindow.ViewModel.Roots.Clear();
            toolWindow.ViewModel.Roots.Add(root);

            await ShowToolWindowAsync(cancellationToken).ConfigureAwait(true);

            TreeItemViewModel CreateViewModel(ValueTrackedItem valueTrackedItem, ImmutableArray<TreeItemViewModel> children = default)
            {
                var document = solution.GetRequiredDocument(valueTrackedItem.DocumentId);
                var fileName = document.FilePath ?? document.Name;

                return new ValueTrackedTreeItemViewModel(
                   valueTrackedItem,
                   solution,
                   toolWindow.ViewModel,
                   _glyphService,
                   valueTrackingService,
                   _threadingContext,
                   fileName,
                   children);
            }
        }

        private async Task ShowToolWindowAsync(CancellationToken cancellationToken)
        {
            var roslynPackage = await RoslynPackage.GetOrLoadAsync(_threadingContext, _serviceProvider, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(roslynPackage);

            await roslynPackage.ShowToolWindowAsync(
                    typeof(ValueTrackingToolWindow),
                    0,
                    true,
                    roslynPackage.DisposalToken).ConfigureAwait(false);
        }

        private async Task<ValueTrackingToolWindow?> GetOrCreateToolWindowAsync(ITextView textView, CancellationToken cancellationToken)
        {
            var roslynPackage = await RoslynPackage.GetOrLoadAsync(_threadingContext, _serviceProvider, cancellationToken).ConfigureAwait(false);
            if (roslynPackage is null)
            {
                return null;
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (ValueTrackingToolWindow.Instance is null)
            {
                var factory = roslynPackage.GetAsyncToolWindowFactory(Guids.ValueTrackingToolWindowId);

                var viewModel = new ValueTrackingTreeViewModel(_classificationFormatMapService.GetClassificationFormatMap(textView), _typeMap, _formatMapService);

                factory.CreateToolWindow(Guids.ValueTrackingToolWindowId, 0, viewModel);
                await factory.InitializeToolWindowAsync(Guids.ValueTrackingToolWindowId, 0);

                // FindWindowPaneAsync creates an instance if it does not exist
                ValueTrackingToolWindow.Instance = (ValueTrackingToolWindow)await roslynPackage.FindWindowPaneAsync(
                    typeof(ValueTrackingToolWindow),
                    0,
                    true,
                    roslynPackage.DisposalToken).ConfigureAwait(false);
            }

            // This can happen if the tool window was initialized outside of this command handler. The ViewModel 
            // still needs to be initialized but had no necessary context. Provide that context now in the command handler.
            if (ValueTrackingToolWindow.Instance.ViewModel is null)
            {
                ValueTrackingToolWindow.Instance.ViewModel = new ValueTrackingTreeViewModel(_classificationFormatMapService.GetClassificationFormatMap(textView), _typeMap, _formatMapService);
            }

            return ValueTrackingToolWindow.Instance;
        }
    }
}

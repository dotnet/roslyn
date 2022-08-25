// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
        private readonly IGlobalOptionService _globalOptions;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;
        private readonly IAsynchronousOperationListener _listener;
        private readonly Workspace _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValueTrackingCommandHandler(
            SVsServiceProvider serviceProvider,
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IClassificationFormatMapService classificationFormatMapService,
            IGlyphService glyphService,
            IEditorFormatMapService formatMapService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            IUIThreadOperationExecutor threadOperationExecutor,
            VisualStudioWorkspace workspace)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
            _threadingContext = threadingContext;
            _typeMap = typeMap;
            _classificationFormatMapService = classificationFormatMapService;
            _glyphService = glyphService;
            _formatMapService = formatMapService;
            _globalOptions = globalOptions;
            _threadOperationExecutor = threadOperationExecutor;
            _listener = listenerProvider.GetListener(FeatureAttribute.ValueTracking);
            _workspace = workspace;
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
                    var service = document.Project.Solution.Services.GetRequiredService<IValueTrackingService>();
                    var items = await service.TrackValueSourceAsync(textSpan, document, cancellationToken).ConfigureAwait(false);
                    if (items.Length == 0)
                    {
                        return;
                    }

                    await ShowToolWindowAsync(args.TextView, document, items, cancellationToken).ConfigureAwait(false);
                });

            return true;
        }

        private async Task ShowToolWindowAsync(ITextView textView, Document document, ImmutableArray<ValueTrackedItem> items, CancellationToken cancellationToken)
        {
            var toolWindow = await GetOrCreateToolWindowAsync(textView, cancellationToken).ConfigureAwait(false);
            if (toolWindow?.ViewModel is null)
            {
                return;
            }

            var classificationFormatMap = _classificationFormatMapService.GetClassificationFormatMap(textView);
            var solution = document.Project.Solution;
            var valueTrackingService = solution.Services.GetRequiredService<IValueTrackingService>();
            var rootItemMap = items.GroupBy(i => i.Parent, resultSelector: (key, items) => (parent: key, children: items));

            using var _ = CodeAnalysis.PooledObjects.ArrayBuilder<TreeItemViewModel>.GetInstance(out var rootItems);

            foreach (var (parent, children) in rootItemMap)
            {
                if (parent is null)
                {
                    foreach (var child in children)
                    {
                        var root = await ValueTrackedTreeItemViewModel.CreateAsync(
                            solution, child, children: ImmutableArray<TreeItemViewModel>.Empty, toolWindow.ViewModel, _glyphService, valueTrackingService, _globalOptions, _threadingContext, _listener, _threadOperationExecutor, cancellationToken).ConfigureAwait(false);
                        rootItems.Add(root);
                    }
                }
                else
                {
                    using var _1 = CodeAnalysis.PooledObjects.ArrayBuilder<TreeItemViewModel>.GetInstance(out var childItems);
                    foreach (var child in children)
                    {
                        var childViewModel = await ValueTrackedTreeItemViewModel.CreateAsync(
                            solution, child, children: ImmutableArray<TreeItemViewModel>.Empty, toolWindow.ViewModel, _glyphService, valueTrackingService, _globalOptions, _threadingContext, _listener, _threadOperationExecutor, cancellationToken).ConfigureAwait(false);
                        childItems.Add(childViewModel);
                    }

                    var root = await ValueTrackedTreeItemViewModel.CreateAsync(
                        solution, parent, childItems.ToImmutable(), toolWindow.ViewModel, _glyphService, valueTrackingService, _globalOptions, _threadingContext, _listener, _threadOperationExecutor, cancellationToken).ConfigureAwait(false);
                    rootItems.Add(root);
                }
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            toolWindow.ViewModel.Roots.Clear();
            foreach (var root in rootItems)
            {
                toolWindow.ViewModel.Roots.Add(root);
            }

            await ShowToolWindowAsync(cancellationToken).ConfigureAwait(true);
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

            var window = (ValueTrackingToolWindow)await roslynPackage.FindWindowPaneAsync(
                typeof(ValueTrackingToolWindow),
                0,
                create: true,
                roslynPackage.DisposalToken).ConfigureAwait(false);

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!window.Initialized)
            {
                var viewModel = new ValueTrackingTreeViewModel(_classificationFormatMapService.GetClassificationFormatMap(textView), _typeMap, _formatMapService);
                window.Initialize(viewModel, _workspace, _threadingContext);
            }

            return window;
        }
    }
}

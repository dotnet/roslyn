// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
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

        private RoslynPackage? _roslynPackage;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValueTrackingCommandHandler(SVsServiceProvider serviceProvider, IThreadingContext threadingContext)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
            _threadingContext = threadingContext;
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

            var selectedSymbol = _threadingContext.JoinableTaskFactory.Run(() => GetSelectedSymbolAsync(textSpan, document, cancellationToken));
            if (selectedSymbol is null)
            {
                return false;
            }

            var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
            var location = Location.Create(syntaxTree, textSpan);

            _threadingContext.JoinableTaskFactory.Run(() => ShowToolWindowAsync(selectedSymbol, location, document.Project.Solution, cancellationToken));

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

        private async Task ShowToolWindowAsync(ISymbol selectedSymbol, Location location, Solution solution, CancellationToken cancellationToken)
        {
            var roslynPackage = await TryGetRoslynPackageAsync(cancellationToken).ConfigureAwait(false);

            if (roslynPackage is null)
            {
                return;
            }

            var dataFlowItem = new ValueTrackingTreeItemViewModel(
                   new ValueTrackedItem(location, selectedSymbol),
                   solution,
                   solution.Workspace.Services.GetRequiredService<IValueTrackingService>());

            if (ValueTrackingToolWindow.Instance is null)
            {
                var factory = roslynPackage.GetAsyncToolWindowFactory(Guids.ValueTrackingToolWindowId);

                factory.CreateToolWindow(Guids.ValueTrackingToolWindowId, 0, dataFlowItem);
                await factory.InitializeToolWindowAsync(Guids.ValueTrackingToolWindowId, 0);

                ValueTrackingToolWindow.Instance = (ValueTrackingToolWindow)await roslynPackage.ShowToolWindowAsync(
                    typeof(ValueTrackingToolWindow),
                    0,
                    true,
                    roslynPackage.DisposalToken).ConfigureAwait(false);
            }
            else
            {
                ValueTrackingToolWindow.Instance.Root = dataFlowItem;
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

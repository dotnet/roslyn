// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[Name("CallHierarchy")]
[Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
internal class CallHierarchyCommandHandler : ICommandHandler<ViewCallHierarchyCommandArgs>
{
    private readonly IThreadingContext _threadingContext;
    private readonly IUIThreadOperationExecutor _threadOperationExecutor;
    private readonly IAsynchronousOperationListener _listener;
    private readonly ICallHierarchyPresenter _presenter;
    private readonly CallHierarchyProvider _provider;

    public string DisplayName => EditorFeaturesResources.Call_Hierarchy;

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CallHierarchyCommandHandler(
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor threadOperationExecutor,
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        [ImportMany] IEnumerable<ICallHierarchyPresenter> presenters,
        CallHierarchyProvider provider)
    {
        _threadingContext = threadingContext;
        _threadOperationExecutor = threadOperationExecutor;
        _listener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.CallHierarchy);
        _presenter = presenters.FirstOrDefault();
        _provider = provider;
    }

    public bool ExecuteCommand(ViewCallHierarchyCommandArgs args, CommandExecutionContext context)
    {
        // We're showing our own UI, ensure the editor doesn't show anything itself.
        context.OperationContext.TakeOwnership();
        var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
        ExecuteCommandAsync(args, context)
            .ReportNonFatalErrorAsync()
            .CompletesAsyncOperation(token);

        return true;
    }

    private async Task ExecuteCommandAsync(ViewCallHierarchyCommandArgs args, CommandExecutionContext commandExecutionContext)
    {
        Document document;

        using (var context = _threadOperationExecutor.BeginExecute(
            ServicesVSResources.Call_Hierarchy, ServicesVSResources.Navigating, allowCancellation: true, showProgress: false))
        {
            document = await args.SubjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(
                commandExecutionContext.OperationContext).ConfigureAwait(true);
            if (document == null)
            {
                return;
            }

            var caretPosition = args.TextView.Caret.Position.BufferPosition.Position;
            var cancellationToken = context.UserCancellationToken;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolUnderCaret = await SymbolFinder.FindSymbolAtPositionAsync(
                semanticModel, caretPosition, document.Project.Solution.Services, cancellationToken).ConfigureAwait(false);

            if (symbolUnderCaret != null)
            {
                // Map symbols so that Call Hierarchy works from metadata-as-source
                var mappingService = document.Project.Solution.Services.GetService<ISymbolMappingService>();
                var mapping = await mappingService.MapSymbolAsync(document, symbolUnderCaret, cancellationToken).ConfigureAwait(false);

                if (mapping.Symbol != null)
                {
                    var node = await _provider.CreateItemAsync(mapping.Symbol, mapping.Project, ImmutableArray<Location>.Empty, cancellationToken).ConfigureAwait(false);

                    if (node != null)
                    {
                        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                        _presenter.PresentRoot((CallHierarchyItem)node);
                        return;
                    }
                }
            }

            // Come back to the UI thread so we can give the user an error notification.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        var notificationService = document.Project.Solution.Services.GetService<INotificationService>();
        notificationService.SendNotification(EditorFeaturesResources.Cursor_must_be_on_a_member_name, severity: NotificationSeverity.Information);
    }

    public CommandState GetCommandState(ViewCallHierarchyCommandArgs args)
        => CommandState.Available;
}

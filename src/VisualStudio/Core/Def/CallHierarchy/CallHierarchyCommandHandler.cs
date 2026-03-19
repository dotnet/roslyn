// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[Name("CallHierarchy")]
[Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CallHierarchyCommandHandler(
    IThreadingContext threadingContext,
    IUIThreadOperationExecutor threadOperationExecutor,
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
    [ImportMany] IEnumerable<ICallHierarchyPresenter> presenters,
    CallHierarchyProvider provider) : ICommandHandler<ViewCallHierarchyCommandArgs>
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IUIThreadOperationExecutor _threadOperationExecutor = threadOperationExecutor;
    private readonly IAsynchronousOperationListener _listener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.CallHierarchy);
    private readonly ICallHierarchyPresenter _presenter = presenters.FirstOrDefault();
    private readonly CallHierarchyProvider _provider = provider;

    public string DisplayName
        => EditorFeaturesResources.Call_Hierarchy;

    public CommandState GetCommandState(ViewCallHierarchyCommandArgs args)
        => CommandState.Available;

    public bool ExecuteCommand(ViewCallHierarchyCommandArgs args, CommandExecutionContext context)
    {
        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        var point = args.TextView.Caret.Position.Point.GetPoint(args.SubjectBuffer, PositionAffinity.Predecessor);
        if (point is null)
            return false;

        // We're showing our own UI, ensure the editor doesn't show anything itself.
        context.OperationContext.TakeOwnership();
        var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
        ExecuteCommandAsync(document, point.Value.Position)
            .ReportNonFatalErrorAsync()
            .CompletesAsyncOperation(token);

        return true;
    }

    private async Task ExecuteCommandAsync(Document document, int caretPosition)
    {
        using (var context = _threadOperationExecutor.BeginExecute(
            EditorFeaturesResources.Call_Hierarchy, ServicesVSResources.Navigating, allowCancellation: true, showProgress: false))
        {
            var cancellationToken = context.UserCancellationToken;

            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, caretPosition, preferPrimaryConstructor: true, cancellationToken).ConfigureAwait(false);

            if (symbolAndProject is (var symbol, var project))
            {
                var node = await _provider.CreateItemAsync(symbol, project, callsites: [], cancellationToken).ConfigureAwait(false);

                if (node != null)
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    _presenter.PresentRoot(node);
                    return;
                }
            }

            // Come back to the UI thread so we can give the user an error notification.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        var notificationService = document.Project.Solution.Services.GetRequiredService<INotificationService>();
        notificationService.SendNotification(EditorFeaturesResources.Cursor_must_be_on_a_member_name, severity: NotificationSeverity.Information);
    }
}

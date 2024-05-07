// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess
{
    internal static class ITextViewWindowInProcessExtensions
    {
        public static async Task InvokeQuickInfoAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var broker = await textViewWindow.TestServices.Shell.GetComponentModelServiceAsync<IAsyncQuickInfoBroker>(cancellationToken);
            var session = await broker.TriggerQuickInfoAsync(await textViewWindow.TestServices.Editor.GetActiveTextViewAsync(cancellationToken), cancellationToken: cancellationToken);
            Contract.ThrowIfNull(session);
        }

        public static async Task<string> GetQuickInfoAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await textViewWindow.TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var broker = await textViewWindow.TestServices.Shell.GetComponentModelServiceAsync<IAsyncQuickInfoBroker>(cancellationToken);

            var session = broker.GetSession(view);

            // GetSession will not return null if preceded by a call to InvokeQuickInfo
            Contract.ThrowIfNull(session);

            while (session.State != QuickInfoSessionState.Visible)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(50, cancellationToken).ConfigureAwait(true);
            }

            return QuickInfoToStringConverter.GetStringFromBulkContent(session.Content);
        }

        public static async Task ShowLightBulbAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await textViewWindow.TestServices.Shell.GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);
            var cmdGroup = typeof(VSConstants.VSStd14CmdID).GUID;
            var cmdExecOpt = OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER;

            var cmdID = VSConstants.VSStd14CmdID.ShowQuickFixes;
            object? obj = null;
            shell.PostExecCommand(cmdGroup, (uint)cmdID, (uint)cmdExecOpt, ref obj);

            var view = await textViewWindow.GetActiveTextViewAsync(cancellationToken);
            var broker = await textViewWindow.TestServices.Shell.GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
            await LightBulbHelper.WaitForLightBulbSessionAsync(textViewWindow.TestServices, broker, view, cancellationToken);
        }

        public static async Task InvokeCompletionListAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await textViewWindow.TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.ListMembers, cancellationToken);
            await textViewWindow.TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet, cancellationToken);
        }

        public static async Task<ImmutableArray<Completion>> GetCompletionItemsAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await WaitForCompletionSetAsync(textViewWindow, cancellationToken);

            // It's not known why WaitForCompletionSetAsync fails to stabilize calls to GetCompletionItemsAsync.
            await Task.Delay(TimeSpan.FromSeconds(1));

            var view = await textViewWindow.GetActiveTextViewAsync(cancellationToken);
            if (view is null)
                return ImmutableArray<Completion>.Empty;

            var broker = await textViewWindow.TestServices.Shell.GetComponentModelServiceAsync<ICompletionBroker>(cancellationToken);
            var sessions = broker.GetSessions(view);
            if (sessions.Count != 1)
            {
                throw new InvalidOperationException($"Expected exactly one session in the completion list, but found {sessions.Count}");
            }

            var selectedCompletionSet = sessions[0].SelectedCompletionSet;
            return selectedCompletionSet.Completions.ToImmutableArray();
        }

        public static async Task InvokeCodeActionListAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.Workarounds.WaitForLightBulbAsync(cancellationToken);

            await textViewWindow.InvokeCodeActionListWithoutWaitingAsync(cancellationToken);

            await textViewWindow.TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, cancellationToken);
        }

        public static async Task InvokeCodeActionListWithoutWaitingAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            if (Version.Parse("17.2.32210.308") > await textViewWindow.TestServices.Shell.GetVersionAsync(cancellationToken))
            {
                // Workaround for extremely unstable async lightbulb (can dismiss itself when SuggestedActionsChanged
                // fires while expanding the light bulb).
                await textViewWindow.TestServices.Input.SendAsync((VirtualKeyCode.OEM_PERIOD, VirtualKeyCode.CONTROL), cancellationToken);
                await Task.Delay(5000, cancellationToken);

                await textViewWindow.TestServices.Editor.DismissLightBulbSessionAsync(cancellationToken);
                await Task.Delay(5000, cancellationToken);
            }

            await textViewWindow.ShowLightBulbAsync(cancellationToken);
        }

        public static async Task<bool> IsLightBulbSessionExpandedAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await textViewWindow.GetActiveTextViewAsync(cancellationToken);

            var broker = await textViewWindow.TestServices.Shell.GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
            if (!broker.IsLightBulbSessionActive(view))
            {
                return false;
            }

            var session = broker.GetSession(view);
            if (session == null || !session.IsExpanded)
            {
                return false;
            }

            return true;
        }

        public static Task PlaceCaretAsync(this ITextViewWindowInProcess textViewWindow, string marker, CancellationToken cancellationToken)
            => textViewWindow.PlaceCaretAsync(marker, charsOffset: 0, occurrence: 0, extendSelection: false, selectBlock: false, cancellationToken);

        public static Task PlaceCaretAsync(this ITextViewWindowInProcess textViewWindow, string marker, int charsOffset, CancellationToken cancellationToken)
            => textViewWindow.PlaceCaretAsync(marker, charsOffset, occurrence: 0, extendSelection: false, selectBlock: false, cancellationToken);

        public static async Task PlaceCaretAsync(
            this ITextViewWindowInProcess textViewWindow,
            string marker,
            int charsOffset,
            int occurrence,
            bool extendSelection,
            bool selectBlock,
            CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await textViewWindow.GetActiveTextViewAsync(cancellationToken);

            var dte = await textViewWindow.TestServices.Shell.GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            dte.Find.FindWhat = marker;
            dte.Find.MatchCase = true;
            dte.Find.MatchInHiddenText = true;
            dte.Find.Target = EnvDTE.vsFindTarget.vsFindTargetCurrentDocument;
            dte.Find.Action = EnvDTE.vsFindAction.vsFindActionFind;

            var originalPosition = (await textViewWindow.GetCaretPositionAsync(cancellationToken)).BufferPosition.Position;
            view.Caret.MoveTo(new SnapshotPoint((await textViewWindow.GetBufferContainingCaretAsync(view, cancellationToken))!.CurrentSnapshot, 0));

            if (occurrence > 0)
            {
                var result = EnvDTE.vsFindResult.vsFindResultNotFound;
                for (var i = 0; i < occurrence; i++)
                {
                    result = dte.Find.Execute();
                }

                if (result != EnvDTE.vsFindResult.vsFindResultFound)
                {
                    throw new Exception("Occurrence " + occurrence + " of marker '" + marker + "' not found in text: " + view.TextSnapshot.GetText());
                }
            }
            else
            {
                var result = dte.Find.Execute();
                if (result != EnvDTE.vsFindResult.vsFindResultFound)
                {
                    throw new Exception("Marker '" + marker + "' not found in text: " + view.TextSnapshot.GetText());
                }
            }

            if (charsOffset > 0)
            {
                for (var i = 0; i < charsOffset - 1; i++)
                {
                    view.Caret.MoveToNextCaretPosition();
                }

                view.Selection.Clear();
            }

            if (charsOffset < 0)
            {
                // On the first negative charsOffset, move to anchor-point position, as if the user hit the LEFT key
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, view.Selection.AnchorPoint.Position.Position));

                for (var i = 0; i < -charsOffset - 1; i++)
                {
                    view.Caret.MoveToPreviousCaretPosition();
                }

                view.Selection.Clear();
            }

            if (extendSelection)
            {
                var newPosition = view.Selection.ActivePoint.Position.Position;
                view.Selection.Select(new VirtualSnapshotPoint(view.TextSnapshot, originalPosition), new VirtualSnapshotPoint(view.TextSnapshot, newPosition));
                view.Selection.Mode = selectBlock ? TextSelectionMode.Box : TextSelectionMode.Stream;
            }
        }

        public static async Task<CaretPosition> GetCaretPositionAsync(this ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await textViewWindow.GetActiveTextViewAsync(cancellationToken);

            var subjectBuffer = await textViewWindow.GetBufferContainingCaretAsync(view, cancellationToken);
            Assumes.Present(subjectBuffer);

            return view.Caret.Position;
        }

        private static async Task WaitForCompletionSetAsync(ITextViewWindowInProcess textViewWindow, CancellationToken cancellationToken)
        {
            await textViewWindow.TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet, cancellationToken);
        }
    }
}

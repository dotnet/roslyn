// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;

internal partial class DebuggerTextView
{
    // HACK HACK HACK HACK HACK: We'll use this fake ICompletionSession to trick them into
    // routing commands to us for both completion and sighelp
    private readonly HACK_CompletionSession _hackCompletionSession = new();

    public void HACK_StartCompletionSession(IIntellisenseSession editorSessionOpt)
    {
        HACK_SetShimCompletionSession();
        editorSessionOpt.Dismissed += CompletionOrSignatureHelpSession_Dismissed;
    }

    private void HACK_SetShimCompletionSession()
    {
        // We could choose to use reflection to add a session only when there isn't one set, but
        // this way we'll re-set the field if they accidentally null it out somehow.
        HACK_SetShimCompletionSessionWorker(_hackCompletionSession);

        _hackCompletionSession.Count++;
    }

    private void HACK_RemoveShimCompletionSession()
    {
        _hackCompletionSession.Count--;
        if (_hackCompletionSession.Count == 0)
        {
            HACK_SetShimCompletionSessionWorker(null);
        }
    }

    private void HACK_SetShimCompletionSessionWorker(ICompletionSession completionSession)
    {
        var propertyList = _innerTextView.Properties.PropertyList;
        var shimController = propertyList.Single(x => x.Value != null && x.Value.GetType().Name == "ShimCompletionController").Value;
        var shimControllerType = shimController.GetType();
        var sessionFieldInfo = shimControllerType.GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance);
        sessionFieldInfo.SetValue(shimController, completionSession);
    }

    private void CompletionOrSignatureHelpSession_Dismissed(object sender, EventArgs e)
        => HACK_RemoveShimCompletionSession();

    /// <remarks>
    /// Dev11's debugger intellisense uses the old completion shims and routes commands through
    /// them. Since we use the new editor completion and sighelp brokers for our sessions, the shims
    /// are unaware of any sessions and don't pass us any commands other than typechar. To determine
    /// whether to pass commands or non, the shims simply verify that they have a pointer to an
    /// ICompletionSession. We will use reflection to place an ICompletionSession in the field.
    /// 
    /// Furthermore, Dev11's debugger intellisense does not pass commands on to SignatureHelp at
    /// all. It's therefore impossible to use the arrow keys to navigate overloads, etc. If we give
    /// the CompletionSessionShim an ICompletionSession, though, we still get the commands and our
    /// command handlers can deal with them appropriately. To get commands when only our
    /// SignatureHelp is up, we still must provide an ICompletionSession, which this class provides. 
    /// Note: Any calls to methods in this class will throw, since the completion shims should not
    /// be doing anything.
    /// 
    /// We also include a counter so that we can null out the field when all of our sessions have
    /// actually ended.
    /// 
    /// See CEditCtlStatementCompletion::HandleKeyDown for more information
    /// </remarks>
    internal class HACK_CompletionSession : ICompletionSession
    {
        public int Count = 0;

        public void Commit()
            => throw new NotImplementedException();

        // We've got a bunch of unused events, so disable the unused event warning.
#pragma warning disable 67
        public event EventHandler Committed;

        public ReadOnlyObservableCollection<CompletionSet> CompletionSets
        {
            get { throw new NotImplementedException(); }
        }

        public void Filter()
            => throw new NotImplementedException();

        public bool IsStarted
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public CompletionSet SelectedCompletionSet
        {
            // To prevent them trying to commit, we need to pretend there's nothing actually
            // selected.
            get { return null; }
            set { throw new NotImplementedException(); }
        }

        public event EventHandler<ValueChangedEventArgs<CompletionSet>> SelectedCompletionSetChanged;

        public void Collapse()
            => throw new NotImplementedException();

        public void Dismiss()
        {
            return;
        }

        public event EventHandler Dismissed;

        public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot)
            => throw new NotImplementedException();

        public ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer)
            => throw new NotImplementedException();

        // The shim controller actually does check IsDismissed immediately after checking for a
        // session, so this implementation can't throw. 
        public bool IsDismissed
        {
            get { return false; }
        }

        public bool Match()
            => throw new NotImplementedException();

        public IIntellisensePresenter Presenter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler PresenterChanged;

        public void Recalculate()
            => throw new NotImplementedException();

        public event EventHandler Recalculated;

        public void Start()
            => throw new NotImplementedException();

        public ITextView TextView
        {
            get { throw new NotImplementedException(); }
        }

        public PropertyCollection Properties
        {
            get { throw new NotImplementedException(); }
        }
    }
}

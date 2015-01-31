// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    internal partial class SignatureHelpPresenter
    {
        private class SignatureHelpPresenterSession : ForegroundThreadAffinitizedObject, ISignatureHelpPresenterSession
        {
            private readonly ISignatureHelpBroker _sigHelpBroker;
            private readonly ITextView _textView;
            private readonly ITextBuffer _subjectBuffer;

            public event EventHandler<EventArgs> Dismissed;
            public event EventHandler<SignatureHelpItemEventArgs> ItemSelected;

            private IBidirectionalMap<SignatureHelpItem, Signature> _signatureMap;

            private IList<SignatureHelpItem> _signatureHelpItems;
            private SignatureHelpItem _selectedItem;

            private ISignatureHelpSession _editorSessionOpt;
            private bool _ignoreSelectionStatusChangedEvent;

            public SignatureHelpPresenterSession(
                ISignatureHelpBroker sigHelpBroker,
                ITextView textView,
                ITextBuffer subjectBuffer)
            {
                _sigHelpBroker = sigHelpBroker;
                _textView = textView;
                _subjectBuffer = subjectBuffer;
            }

            public void PresentItems(
                ITrackingSpan triggerSpan,
                IList<SignatureHelpItem> signatureHelpItems,
                SignatureHelpItem selectedItem,
                int? selectedParameter)
            {
                _signatureHelpItems = signatureHelpItems;
                _selectedItem = selectedItem;

                // Create all the editor sigantures for the sig help items we have.
                this.CreateSignatures(triggerSpan, selectedParameter);

                // It's a new list of items.  Either create the editor session if this is the
                // first time, or ask the editor session that we already have to recalculate.
                if (_editorSessionOpt == null)
                {
                    // We're tracking the caret.  Don't have the editor do it. 
                    _editorSessionOpt = _sigHelpBroker.CreateSignatureHelpSession(
                        _textView,
                        triggerSpan.GetStartTrackingPoint(PointTrackingMode.Negative),
                        trackCaret: false);

                    var debugTextView = _textView as IDebuggerTextView;
                    if (debugTextView != null && !debugTextView.IsImmediateWindow)
                    {
                        debugTextView.HACK_StartCompletionSession(_editorSessionOpt);
                    }

                    _editorSessionOpt.Dismissed += (s, e) => OnEditorSessionDismissed();
                    _editorSessionOpt.SelectedSignatureChanged += OnSelectedSignatureChanged;
                }

                // So here's the deal.  We cannot create the editor session and give it the right
                // signatures (even though we know what they are).  Instead, the sessino with
                // call back into the ISignatureHelpSourceProvider (which is us) to get those
                // values. It will pass itself along with the calls back into
                // ISignatureHelpSourceProvider. So, in order to make that connection work, we
                // add properties to the session so that we can call back into ourselves, get
                // the signatures and add it to the session.
                if (!_editorSessionOpt.Properties.ContainsProperty(s_augmentSessionKey))
                {
                    _editorSessionOpt.Properties.AddProperty(s_augmentSessionKey, this);
                }

                try
                {
                    // Don't want to get any callbacks while we do this.
                    _ignoreSelectionStatusChangedEvent = true;

                    _editorSessionOpt.Recalculate();

                    // Now let the editor know what the currently selected item is.
                    Contract.Requires(_signatureMap.ContainsKey(selectedItem));
                    Contract.ThrowIfNull(_signatureMap);

                    var defaultValue = _signatureMap.GetValueOrDefault(_selectedItem);
                    if (_editorSessionOpt != null)
                    {
                        _editorSessionOpt.SelectedSignature = defaultValue;
                    }
                }
                finally
                {
                    _ignoreSelectionStatusChangedEvent = false;
                }
            }

            private void CreateSignatures(
                ITrackingSpan triggerSpan,
                int? selectedParameter)
            {
                _signatureMap = BidirectionalMap<SignatureHelpItem, Signature>.Empty;

                foreach (var item in _signatureHelpItems)
                {
                    _signatureMap = _signatureMap.Add(item, new Signature(triggerSpan, item));
                }

                this.SetCurrentParameterForAllItems(selectedParameter);
            }

            private void SetCurrentParameterForAllItems(int? selectedParameter)
            {
                foreach (var item in _signatureHelpItems)
                {
                    this.SetCurrentParameter(item, selectedParameter);
                }
            }

            private void SetCurrentParameter(SignatureHelpItem item, int? selectedParameter)
            {
                Contract.ThrowIfFalse(_signatureMap.ContainsKey(item));
                var signature = _signatureMap.GetValueOrDefault(item);

                if (selectedParameter.HasValue)
                {
                    if (selectedParameter.Value < item.Parameters.Count)
                    {
                        // If the selected parameter is within the range of parameters of this item then set
                        // that as the current parameter.
                        signature.CurrentParameter = signature.Parameters[selectedParameter.Value];
                        return;
                    }
                    else if (item.IsVariadic)
                    {
                        // It wasn't in range, but the item takes an unlmiited number of parameters.  So
                        // just set current parameter to the last parameter (the variadic one).
                        signature.CurrentParameter = signature.Parameters.Last();
                        return;
                    }
                }

                // It was out of bounds, there is no current parameter now.
                signature.CurrentParameter = null;
            }

            private void OnEditorSessionDismissed()
            {
                AssertIsForeground();

                var dismissed = this.Dismissed;
                if (dismissed != null)
                {
                    dismissed(this, new EventArgs());
                }
            }

            private void OnSelectedSignatureChanged(object sender, SelectedSignatureChangedEventArgs eventArgs)
            {
                AssertIsForeground();

                if (_ignoreSelectionStatusChangedEvent)
                {
                    return;
                }

                SignatureHelpItem helpItem;
                Contract.ThrowIfFalse(_signatureMap.TryGetKey((Signature)eventArgs.NewSelectedSignature, out helpItem));

                var helpItemSelected = this.ItemSelected;
                if (helpItemSelected != null && helpItem != null)
                {
                    helpItemSelected(this, new SignatureHelpItemEventArgs(helpItem));
                }
            }

            public void Dismiss()
            {
                AssertIsForeground();

                if (_editorSessionOpt == null)
                {
                    // No editor session, nothing to do here.
                    return;
                }

                _editorSessionOpt.Dismiss();
                _editorSessionOpt = null;
            }

            private bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command)
            {
                var target = _editorSessionOpt != null
                    ? _editorSessionOpt.Presenter as IIntellisenseCommandTarget
                    : null;

                return target != null && target.ExecuteKeyboardCommand(command);
            }

            public void SelectPreviousItem()
            {
                ExecuteKeyboardCommand(IntellisenseKeyboardCommand.Up);
            }

            public void SelectNextItem()
            {
                ExecuteKeyboardCommand(IntellisenseKeyboardCommand.Down);
            }

            // Call backs from our ISignatureHelpSourceProvider.  Used to actually populate the vs
            // session.
            internal void AugmentSignatureHelpSession(IList<ISignature> signatures)
            {
                signatures.Clear();
                signatures.AddRange(_signatureHelpItems.Select(_signatureMap.GetValueOrDefault));
            }
        }
    }
}

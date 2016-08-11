// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.QuickInfo;
using System.Windows;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo.Presentation
{
    internal partial class QuickInfoPresenter
    {
        private class QuickInfoPresenterSession : ForegroundThreadAffinitizedObject, IQuickInfoPresenterSession
        {
            private readonly IQuickInfoBroker _quickInfoBroker;
            private readonly ITextView _textView;
            private readonly ITextBuffer _subjectBuffer;
            private readonly ImmutableArray<Lazy<QuickInfoPresentationProvider, QuickInfoPresentationProviderInfo>> _presentationProviders;

            private IQuickInfoSession _editorSessionOpt;

            private QuickInfoData _item;
            private ITrackingSpan _triggerSpan;

            public event EventHandler<EventArgs> Dismissed;

            public QuickInfoPresenterSession(IQuickInfoBroker quickInfoBroker, ITextView textView, ITextBuffer subjectBuffer, ImmutableArray<Lazy<QuickInfoPresentationProvider, QuickInfoPresentationProviderInfo>> presentationProviders)
                : this(quickInfoBroker, textView, subjectBuffer, null, presentationProviders)
            {
            }

            public QuickInfoPresenterSession(IQuickInfoBroker quickInfoBroker, ITextView textView, ITextBuffer subjectBuffer, IQuickInfoSession sessionOpt, ImmutableArray<Lazy<QuickInfoPresentationProvider, QuickInfoPresentationProviderInfo>> presentationProviders)
            {
                _quickInfoBroker = quickInfoBroker;
                _textView = textView;
                _subjectBuffer = subjectBuffer;
                _editorSessionOpt = sessionOpt;
                _presentationProviders = presentationProviders;
            }

            public void PresentItem(ITrackingSpan triggerSpan, QuickInfoData item, bool trackMouse)
            {
                AssertIsForeground();

                _triggerSpan = triggerSpan;
                _item = item;

                // It's a new list of items.  Either create the editor session if this is the first time, or ask the
                // editor session that we already have to recalculate.
                if (_editorSessionOpt == null || _editorSessionOpt.IsDismissed)
                {
                    // We're tracking the caret.  Don't have the editor do it.
                    var triggerPoint = triggerSpan.GetStartTrackingPoint(PointTrackingMode.Negative);

                    _editorSessionOpt = _quickInfoBroker.CreateQuickInfoSession(_textView, triggerPoint, trackMouse: trackMouse);
                    _editorSessionOpt.Dismissed += (s, e) => OnEditorSessionDismissed();
                }

                // So here's the deal.  We cannot create the editor session and give it the right
                // signatures (even though we know what they are).  Instead, the session with
                // call back into the ISignatureHelpSourceProvider (which is us) to get those
                // values. It will pass itself along with the calls back into
                // ISignatureHelpSourceProvider. So, in order to make that connection work, we
                // add properties to the session so that we can call back into ourselves, get
                // the signatures and add it to the session.
                if (!_editorSessionOpt.Properties.ContainsProperty(s_augmentSessionKey))
                {
                    _editorSessionOpt.Properties.AddProperty(s_augmentSessionKey, this);
                }

                _editorSessionOpt.Recalculate();
            }

            public void Dismiss()
            {
                AssertIsForeground();

                if (_editorSessionOpt == null)
                {
                    // No editor session, nothing to do here.
                    return;
                }

                if (_item == null)
                {
                    // We don't have an item, so we're being asked to augment a session.
                    // Since we didn't put anything in the session, don't dismiss it either.
                    return;
                }

                _editorSessionOpt.Dismiss();
                _editorSessionOpt = null;
            }

            private void OnEditorSessionDismissed()
            {
                AssertIsForeground();
                this.Dismissed?.Invoke(this, new EventArgs());
            }

            internal void AugmentQuickInfoSession(IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                applicableToSpan = _triggerSpan;

                var content = CreateContent(_item.Element, _subjectBuffer.CurrentSnapshot);
                if (content != null)
                {
                    quickInfoContent.Add(content);
                }
            }

            private FrameworkElement CreateContent(QuickInfoElement element, ITextSnapshot snapshot)
            {
                QuickInfoPresentationProvider provider;

                if (_presentationProviders.TryGetValue(element.Kind, out provider)
                    || _presentationProviders.TryGetValue(QuickInfoElementKinds.Text, out provider))
                {
                    return provider.CreatePresentation(element, snapshot);
                }

                return null;
            }
        }
    }
}

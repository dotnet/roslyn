// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EventHookup
{
    /// <summary>
    /// Need to trick ShimQuickInfoController into leaving Quick Info Sessions created by
    /// EventHookupCommandHandler visible even through buffer changed events. We must set its 
    /// private _session field via reflection.
    /// </summary>
    [Export(typeof(IHACK_EventHookupDismissalOnBufferChangePreventerService))]
    internal sealed class HACK_EventHookupDismissalOnBufferChangePreventerService : IHACK_EventHookupDismissalOnBufferChangePreventerService
    {
        public void HACK_EnsureQuickInfoSessionNotDismissedPrematurely(ITextView textView)
        {
            // Need an IQuickInfoSession with Properties containing an IVsTextTipData
            HACK_SetShimQuickInfoSessionWorker(textView, new HACK_QuickInfoSession());
        }

        public void HACK_OnQuickInfoSessionDismissed(ITextView textView)
        {
            HACK_SetShimQuickInfoSessionWorker(textView, null);
        }

#pragma warning disable CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
        private void HACK_SetShimQuickInfoSessionWorker(ITextView textView, IQuickInfoSession quickInfoSession)
        {
            var properties = textView.Properties.PropertyList;
            var shimController = properties.Single(p => p.Value != null && p.Value.GetType().Name == "ShimQuickInfoController").Value;
            var sessionField = shimController.GetType().GetField("_session", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sessionField.SetValue(shimController, quickInfoSession);
        }

        /// <summary>
        /// The Properties property must contain an IVsTextTipData (which is never used), but no 
        /// other methods need to be implemented.
        /// </summary>
        private class HACK_QuickInfoSession : IQuickInfoSession
        {
#pragma warning disable 67
            public event EventHandler Dismissed;
            public event EventHandler Recalculated;
            public event EventHandler ApplicableToSpanChanged;
            public event EventHandler PresenterChanged;
#pragma warning restore 67
#pragma warning restore CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
            public PropertyCollection Properties
            {
                get
                {
                    var collection = new PropertyCollection();
                    collection.AddProperty(typeof(IVsTextTipData), new HACK_VsTextTipData());
                    return collection;
                }
            }

            public Microsoft.VisualStudio.Text.ITrackingSpan ApplicableToSpan
            {
                get { throw new NotImplementedException(); }
            }

            public BulkObservableCollection<object> QuickInfoContent
            {
                get { throw new NotImplementedException(); }
            }

            public bool TrackMouse
            {
                get { throw new NotImplementedException(); }
            }

            public void Collapse()
            {
                throw new NotImplementedException();
            }

            public void Dismiss()
            {
                throw new NotImplementedException();
            }

            public Microsoft.VisualStudio.Text.SnapshotPoint? GetTriggerPoint(Microsoft.VisualStudio.Text.ITextSnapshot textSnapshot)
            {
                throw new NotImplementedException();
            }

            public Microsoft.VisualStudio.Text.ITrackingPoint GetTriggerPoint(Microsoft.VisualStudio.Text.ITextBuffer textBuffer)
            {
                throw new NotImplementedException();
            }

            public bool IsDismissed
            {
                get { throw new NotImplementedException(); }
            }

            public bool Match()
            {
                throw new NotImplementedException();
            }

            public IIntellisensePresenter Presenter
            {
                get { throw new NotImplementedException(); }
            }

            public void Recalculate()
            {
                throw new NotImplementedException();
            }

            public void Start()
            {
                throw new NotImplementedException();
            }

            public ITextView TextView
            {
                get { throw new NotImplementedException(); }
            }
        }

        /// <summary>
        /// None of the methods need to be implemented.
        /// </summary>
        private class HACK_VsTextTipData : IVsTextTipData
        {
            public int GetContextStream(out int pos, out int length)
            {
                throw new NotImplementedException();
            }

            public int GetTipFontInfo(int chars, uint[] pdwFontAttr)
            {
                throw new NotImplementedException();
            }

            public int GetTipText(string[] pbstrText, out int getFontInfo)
            {
                throw new NotImplementedException();
            }

            public void OnDismiss()
            {
                throw new NotImplementedException();
            }

            public void UpdateView()
            {
                throw new NotImplementedException();
            }
        }
    }
}

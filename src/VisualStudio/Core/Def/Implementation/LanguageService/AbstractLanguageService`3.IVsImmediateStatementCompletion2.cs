﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService, TProject> : IVsImmediateStatementCompletion2
    {
        protected Dictionary<IVsTextView, DebuggerIntelliSenseFilter<TPackage, TLanguageService, TProject>> filters =
            new Dictionary<IVsTextView, DebuggerIntelliSenseFilter<TPackage, TLanguageService, TProject>>();

        int IVsImmediateStatementCompletion2.EnableStatementCompletion(int enable, int startIndex, int endIndex, IVsTextView textView)
        {
            DebuggerIntelliSenseFilter<TPackage, TLanguageService, TProject> filter;
            if (filters.TryGetValue(textView, out filter))
            {
                filter.Enabled = enable != 0;
            }

            return VSConstants.S_OK;
        }

        int IVsImmediateStatementCompletion2.InstallStatementCompletion(int install, IVsTextView textView, int initialEnable)
        {
            // We'll install a filter whenever the debugger asks, but it won't do anything but call
            // the next filter until the context is set. To ensure that we correctly install and
            // uninstall from the many possible textviews we can work on, we maintain a dictionary
            // of textview->filters.
            if (install != 0)
            {
                DebuggerIntelliSenseFilter<TPackage, TLanguageService, TProject> filter;
                if (this.filters.ContainsKey(textView))
                {
                    // We already have a filter in this textview. Return.
                    return VSConstants.S_OK;
                }
                else
                {
                    filter = new DebuggerIntelliSenseFilter<TPackage, TLanguageService, TProject>(this,
                        this.EditorAdaptersFactoryService.GetWpfTextView(textView),
                        this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>(),
                        this.Package.ComponentModel.GetService<ICommandHandlerServiceFactory>());
                    this.filters[textView] = filter;

                    IOleCommandTarget nextFilter;
                    Marshal.ThrowExceptionForHR(textView.AddCommandFilter(filter, out nextFilter));
                    filter.SetNextFilter(nextFilter);
                }
            }
            else
            {
                Marshal.ThrowExceptionForHR(textView.RemoveCommandFilter(this.filters[textView]));
                this.filters[textView].Dispose();
                this.filters.Remove(textView);
            }

            return VSConstants.S_OK;
        }

        int IVsImmediateStatementCompletion2.SetCompletionContext(string filePath,
            IVsTextLines buffer,
            TextSpan[] currentStatementSpan,
            object punkContext,
            IVsTextView textView)
        {
            var waitIndicator = this.Package.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(ServicesVSResources.Debugger, ServicesVSResources.Loading_immediate_window,
                allowCancel: false,
                action: waitContext =>
                {
                    var bufferAndContext = GetCompletionContextAsync(filePath, buffer, currentStatementSpan, punkContext, textView).WaitAndGetResult(waitContext.CancellationToken);
                    if (bufferAndContext.Item1 != null)
                    {
                        if (bufferAndContext.Item2 != null)
                        {
                            this.filters[textView].SetContext(bufferAndContext.Item2);
                        }
                        else
                        {
                            this.filters[textView].RemoveContext();
                        }
                    }
                });

            return VSConstants.S_OK;
        }

        private async Task<ValueTuple<IVsTextLines, AbstractDebuggerIntelliSenseContext>> GetCompletionContextAsync(
            string filePath,
            IVsTextLines buffer,
            TextSpan[] currentStatementSpan,
            object punkContext,
            IVsTextView textView)
        {
            // The immediate window is always marked read-only and the language service is
            // responsible for asking the buffer to make itself writable. We'll have to do that for
            // commit, so we need to drag the IVsTextLines around, too.
            IVsTextLines debuggerBuffer;
            Marshal.ThrowExceptionForHR(textView.GetBuffer(out debuggerBuffer));

            var view = EditorAdaptersFactoryService.GetWpfTextView(textView);
            AbstractDebuggerIntelliSenseContext context = null;

            // Sometimes, they give us a null context buffer. In that case, there's probably not any
            // work to do.
            if (buffer != null)
            {
                var contextBuffer = EditorAdaptersFactoryService.GetDataBuffer(buffer);
                context = CreateContext(view, textView, debuggerBuffer, contextBuffer, currentStatementSpan);
                if (!await context.TryInitializeAsync().ConfigureAwait(false))
                {
                    context = null;
                }
            }

            return ValueTuple.Create(buffer, context);
        }

        // Let our deriving language services build up an appropriate context.
        protected abstract AbstractDebuggerIntelliSenseContext CreateContext(IWpfTextView view,
            IVsTextView vsTextView,
            IVsTextLines debuggerBuffer,
            ITextBuffer contextBuffer,
            Microsoft.VisualStudio.TextManager.Interop.TextSpan[] currentStatementSpan);

        #region Methods that are never called

        int IVsImmediateStatementCompletion2.EnableStatementCompletion_Deprecated(int enable, int startIndex, int endIndex)
        {
            Debug.Assert(false);
            return VSConstants.S_OK;
        }

        int IVsImmediateStatementCompletion2.GetFilter(IVsTextView textView, out IVsTextViewFilter filter)
        {
            // They never even call this, so just make it compile
            Debug.Assert(false);
            filter = null;
            return VSConstants.S_OK;
        }

        int IVsImmediateStatementCompletion.EnableStatementCompletion_Deprecated(int enable, int startIndex, int endIndex)
        {
            Debug.Assert(false);
            return VSConstants.S_OK;
        }

        int IVsImmediateStatementCompletion.InstallStatementCompletion(int install, IVsTextView textView, int initialEnable)
        {
            Debug.Assert(false);
            return VSConstants.S_OK;
        }

        int IVsImmediateStatementCompletion.SetCompletionContext_Deprecated(string filePath, IVsTextLines buffer, TextSpan[] currentStatementSpan, object punkContext)
        {
            Debug.Assert(false);
            return VSConstants.S_OK;
        }

        int IVsImmediateStatementCompletion2.SetCompletionContext_Deprecated(string filepath, IVsTextLines buffer, TextSpan[] currentStatementSpan, object punkContext)
        {
            Debug.Assert(false);
            return VSConstants.S_OK;
        }
        #endregion
    }
}

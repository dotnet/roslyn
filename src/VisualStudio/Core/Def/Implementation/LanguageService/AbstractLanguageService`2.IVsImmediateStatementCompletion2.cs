// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : IVsImmediateStatementCompletion2
    {
        protected Dictionary<IVsTextView, DebuggerIntelliSenseFilter<TPackage, TLanguageService>> filters =
            new Dictionary<IVsTextView, DebuggerIntelliSenseFilter<TPackage, TLanguageService>>();

        int IVsImmediateStatementCompletion2.EnableStatementCompletion(int enable, int startIndex, int endIndex, IVsTextView textView)
        {
            if (filters.TryGetValue(textView, out var filter))
            {
                if (enable != 0)
                {
                    filter.EnableCompletion();
                }
                else
                {
                    filter.DisableCompletion();
                }
            }

            // Debugger wants Roslyn to return OK in all cases, 
            // for example, even if Rolsyn tried to enable the one already enabled.
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
                DebuggerIntelliSenseFilter<TPackage, TLanguageService> filter;
                if (!this.filters.ContainsKey(textView))
                {
                    filter = new DebuggerIntelliSenseFilter<TPackage, TLanguageService>(this,
                        this.EditorAdaptersFactoryService.GetWpfTextView(textView),
                        this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>(),
                        this.Package.ComponentModel.GetService<ICommandHandlerServiceFactory>(),
                        this.Package.ComponentModel.GetService<IFeatureServiceFactory>());
                    this.filters[textView] = filter;
                    Marshal.ThrowExceptionForHR(textView.AddCommandFilter(filter, out var nextFilter));
                    filter.SetNextFilter(nextFilter);
                }

                this.filters[textView].SetContentType(install: true);
            }
            else
            {
                Marshal.ThrowExceptionForHR(textView.RemoveCommandFilter(this.filters[textView]));
                this.filters[textView].SetContentType(install: false);
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
            // The immediate window is always marked read-only and the language service is
            // responsible for asking the buffer to make itself writable. We'll have to do that for
            // commit, so we need to drag the IVsTextLines around, too.
            Marshal.ThrowExceptionForHR(textView.GetBuffer(out var debuggerBuffer));

            var view = EditorAdaptersFactoryService.GetWpfTextView(textView);

            // Sometimes, they give us a null context buffer. In that case, there's probably not any
            // work to do.
            if (buffer != null)
            {
                var contextBuffer = EditorAdaptersFactoryService.GetDataBuffer(buffer);

                if (!contextBuffer.ContentType.IsOfType(this.ContentTypeName))
                {
                    FatalError.ReportWithoutCrash(
                        new ArgumentException($"Expected content type {this.ContentTypeName} " +
                        $"but got buffer of content type {contextBuffer.ContentType}"));

                    return VSConstants.E_FAIL;
                }

                // Clean the old context in any case upfront: 
                // even if we fail to initialize, the old context must be cleaned.
                this.filters[textView].RemoveContext();

                var context = CreateContext(view, textView, debuggerBuffer, contextBuffer, currentStatementSpan);
                if (context.TryInitialize())
                {
                    this.filters[textView].SetContext(context);
                }
            }

            return VSConstants.S_OK;
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

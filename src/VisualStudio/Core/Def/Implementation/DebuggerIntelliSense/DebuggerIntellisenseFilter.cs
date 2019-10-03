// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
{
    internal class DebuggerIntelliSenseFilter<TPackage, TLanguageService> : AbstractVsTextViewFilter<TPackage, TLanguageService>, IDisposable, IFeatureController
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        private readonly IFeatureServiceFactory _featureServiceFactory;
        private AbstractDebuggerIntelliSenseContext _context;
        private IOleCommandTarget _originalNextCommandFilter;
        private IFeatureDisableToken _completionDisabledToken;

        public DebuggerIntelliSenseFilter(
            AbstractLanguageService<TPackage, TLanguageService> languageService,
            IWpfTextView wpfTextView,
            IVsEditorAdaptersFactoryService adapterFactory,
            IFeatureServiceFactory featureServiceFactory)
            : base(languageService, wpfTextView, adapterFactory)
        {
            _featureServiceFactory = featureServiceFactory;
        }

        internal void EnableCompletion()
        {
            if (_completionDisabledToken == null)
            {
                return;
            }

            _completionDisabledToken.Dispose();
            _completionDisabledToken = null;
        }

        internal void DisableCompletion()
        {
            var featureService = _featureServiceFactory.GetOrCreate(WpfTextView);
            if (_completionDisabledToken == null)
            {
                _completionDisabledToken = featureService.Disable(PredefinedEditorFeatureNames.Completion, this);
            }
        }

        internal void SetNextFilter(IOleCommandTarget nextFilter)
        {
            _originalNextCommandFilter = nextFilter;
            SetNextFilterWorker();
        }

        private void SetNextFilterWorker()
        {
            // We have a new _originalNextCommandFilter or new _context, reset NextCommandTarget chain based on their values.
            // The chain is formed like this: 
            //     IVsCommandHandlerServiceAdapter (our command handlers migrated to the modern editor commanding)
            //         -> original next command filter
            var nextCommandFilter = _originalNextCommandFilter;
            // The next filter is set in response to debugger calling IVsImmediateStatementCompletion2.InstallStatementCompletion(),
            // followed IVsImmediateStatementCompletion2.SetCompletionContext() that sets the context.
            // Check context in case debugger hasn't called IVsImmediateStatementCompletion2.SetCompletionContext() yet - before that
            // we cannot set up command handling on correct view and buffer.
            if (_context != null)
            {
                // Chain in editor command handler service. It will execute all our command handlers migrated to the modern editor commanding
                // on the same text view and buffer as this.CurrentHandlers.
                var componentModel = (IComponentModel)LanguageService.SystemServiceProvider.GetService(typeof(SComponentModel));
                var vsCommandHandlerServiceAdapterFactory = componentModel.GetService<IVsCommandHandlerServiceAdapterFactory>();
                var vsCommandHandlerServiceAdapter = vsCommandHandlerServiceAdapterFactory.Create(ConvertTextView(),
                    GetSubjectBufferContainingCaret(), // our override doesn't actually check the caret and always returns _context.Buffer
                    nextCommandFilter);
                nextCommandFilter = vsCommandHandlerServiceAdapter;
            }

            this.NextCommandTarget = nextCommandFilter;
        }

        // If they haven't given us a context, or we aren't enabled, we should pass along to the next thing in the chain,
        // instead of trying to have our command handlers to work.
        public override int Exec(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_context == null)
            {
                return NextCommandTarget.Exec(pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            // NOTE: at this point base.Exec will still call NextCommandTarget.Exec like above, but we
            // are skipping some special handling in a few cases, and also enabling GC Low Latency mode.
            return base.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
        }

        // Our immediate window filter has to override this behavior in the default
        // AbstractOLECommandTarget because they'll send us SCROLLUP when the key typed was CANCEL
        // (see below)
        protected override int ExecuteVisualStudio2000(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut)
        {
            // We have to ask the buffer to make itself writable, if it isn't already
            _context.DebuggerTextLines.GetStateFlags(out var bufferFlags);
            _context.DebuggerTextLines.SetStateFlags((uint)((BUFFERSTATEFLAGS)bufferFlags & ~BUFFERSTATEFLAGS.BSF_USER_READONLY));

            var result = VSConstants.S_OK;

            // If the caret is outside our projection, defer to the next command target.
            var caretPosition = _context.DebuggerTextView.GetCaretPoint(_context.Buffer);
            if (caretPosition == null)
            {
                return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            switch ((VSConstants.VSStd2KCmdID)commandId)
            {
                // If we see a RETURN, and we're in the immediate window, we'll want to rebuild
                // spans after all the other command handlers have run.
                case VSConstants.VSStd2KCmdID.RETURN:
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
                    _context.RebuildSpans();
                    break;

                // After handling typechar of '?', start completion.
                case VSConstants.VSStd2KCmdID.TYPECHAR:
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);

                    if ((char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn) == '?')
                    {
                        if (_context.CompletionStartsOnQuestionMark)
                        {
                            // The subject buffer passed in through the command
                            // target isn't the one we want, because we've
                            // definitely remapped buffers. Ask our context for
                            // the real subject buffer.
                            NextCommandTarget.Exec(VSConstants.VSStd2K, (uint)VSConstants.VSStd2KCmdID.SHOWMEMBERLIST,
                                executeInformation, pvaIn, pvaOut);
                        }
                    }

                    break;

                default:
                    return base.ExecuteVisualStudio2000(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            _context.DebuggerTextLines.SetStateFlags(bufferFlags);

            return result;
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            Contract.ThrowIfNull(_context);
            return _context.Buffer;
        }

        protected override ITextView ConvertTextView()
        {
            return _context.DebuggerTextView;
        }

        internal void SetContext(AbstractDebuggerIntelliSenseContext context)
        {
            // If there was an old context, it must be cleaned before calling SetContext.
            Debug.Assert(_context == null);
            _context = context;
            this.SetNextFilterWorker();
        }

        internal void RemoveContext()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        internal void SetContentType(bool install)
            => _context?.SetContentType(install);

        public void Dispose()
        {
            if (_completionDisabledToken != null)
            {
                _completionDisabledToken.Dispose();
            }

            RemoveContext();
        }
    }
}

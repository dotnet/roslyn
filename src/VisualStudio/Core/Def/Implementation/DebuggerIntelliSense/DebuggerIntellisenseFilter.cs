// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
    internal class DebuggerIntelliSenseFilter<TPackage, TLanguageService> : AbstractVsTextViewFilter<TPackage, TLanguageService>, IDisposable
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        private readonly ICommandHandlerServiceFactory _commandFactory;
        private readonly IWpfTextView _wpfTextView;
        private AbstractDebuggerIntelliSenseContext _context;
        private IOleCommandTarget _originalNextCommandFilter;

        internal bool Enabled { get; set; }

        public DebuggerIntelliSenseFilter(
            AbstractLanguageService<TPackage, TLanguageService> languageService,
            IWpfTextView wpfTextView,
            IVsEditorAdaptersFactoryService adapterFactory,
            ICommandHandlerServiceFactory commandFactory)
            : base(languageService, wpfTextView, adapterFactory, commandFactory)
        {
            _wpfTextView = wpfTextView;
            _commandFactory = commandFactory;
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
            // this.CurrentHandlers (legacy command handlers) 
            //     -> IVsCommandHandlerServiceAdapter (our command handlers migrated to the modern editor commanding)
            //            -> original next command filter
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

        internal void SetCommandHandlers(ITextBuffer buffer)
        {
            this.CurrentHandlers = _commandFactory.GetService(buffer);
            SetNextFilterWorker();
        }

        // If they haven't given us a context, or we aren't enabled, we should pass along to the next thing in the chain,
        // instead of trying to have our command handlers to work.
        public override int Exec(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_context == null || !Enabled)
            {
                return NextCommandTarget.Exec(pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return base.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
        }

        // Our immediate window filter has to override this behavior in the default
        // AbstractOLECommandTarget because they'll send us SCROLLUP when the key typed was CANCEL
        // (see below)
        protected override int ExecuteVisualStudio2000(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            // We have to ask the buffer to make itself writable, if it isn't already
            _context.DebuggerTextLines.GetStateFlags(out var bufferFlags);
            _context.DebuggerTextLines.SetStateFlags((uint)((BUFFERSTATEFLAGS)bufferFlags & ~BUFFERSTATEFLAGS.BSF_USER_READONLY));

            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;

            // If the caret is outside our projection, defer to the next command target.
            var caretPosition = _context.DebuggerTextView.GetCaretPoint(_context.Buffer);
            if (caretPosition == null)
            {
                return NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            void executeNextCommandTarget()
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            switch ((VSConstants.VSStd2KCmdID)commandId)
            {
                // HACK: If you look at EditCtlStatementCompletion.cpp, they translate CANCEL to
                // SCROLLUP to do some hacking around their own command infrastructure and the
                // legacy stuff they interfaced with. That means we get SCROLLUP if the user
                // types escape, so treat SCROLLUP like CANCEL. It's actually a CANCEL.
                case VSConstants.VSStd2KCmdID.SCROLLUP:
                    ExecuteCancel(subjectBuffer, contentType, () =>
                    {
                        // We cannot just pass executeNextCommandTarget becuase it would execute SCROLLUP
                        var cancelCmdGroupId = VSConstants.VSStd2K;
                        NextCommandTarget.Exec(ref cancelCmdGroupId, (uint)VSConstants.VSStd2KCmdID.CANCEL, executeInformation, pvaIn, pvaOut);
                    });
                    break;

                // If we see a RETURN, and we're in the immediate window, we'll want to rebuild
                // spans after all the other command handlers have run.
                case VSConstants.VSStd2KCmdID.RETURN:
                    ExecuteReturn(subjectBuffer, contentType, executeNextCommandTarget);
                    _context.RebuildSpans();
                    break;

                // After handling typechar of '?', start completion.
                case VSConstants.VSStd2KCmdID.TYPECHAR:
                    ExecuteTypeCharacter(pvaIn, subjectBuffer, contentType, executeNextCommandTarget);
                    if ((char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn) == '?')
                    {
                        if (_context.CompletionStartsOnQuestionMark)
                        {
                            // The subject buffer passed in through the command
                            // target isn't the one we want, because we've
                            // definitely remapped buffers. Ask our context for
                            // the real subject buffer.
                            ExecuteInvokeCompletionList(_context.Buffer, _context.ContentType, () =>
                            {
                                // We cannot just pass executeNextCommandTarget becuase it would execute TYPECHAR
                                var showMemberListCmdGroupId = VSConstants.VSStd2K;
                                NextCommandTarget.Exec(ref showMemberListCmdGroupId, (uint)VSConstants.VSStd2KCmdID.SHOWMEMBERLIST,
                                    executeInformation, pvaIn, pvaOut);
                            });
                        }
                    }

                    break;

                default:
                    return base.ExecuteVisualStudio2000(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
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
            // We're never notified of being disabled in the immediate window, so the
            // best we can do is only keep resources from one context alive at a time.
            Dispose();

            _context = context;
            this.SetCommandHandlers(context.Buffer);
        }

        internal void RemoveContext()
        {
            Dispose();
            _context = null;
        }

        internal void SetContentType(bool install)
            => _context?.SetContentType(install);

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
            }
        }
    }
}

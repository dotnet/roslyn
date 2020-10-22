//
// AbstractNavigationCommandHandler.cs
//
// Copyright (c) 2019 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationCommandHandlers
{
    public abstract class AbstractNavigationCommandHandler<TCommandArgs> :
        VSCommanding.ICommandHandler<TCommandArgs> where TCommandArgs : Microsoft.VisualStudio.Text.Editor.Commanding.EditorCommandArgs
    {
        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;
        //private readonly IAsynchronousOperationListener _asyncListener;

        internal AbstractNavigationCommandHandler(
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters)
        {
            Contract.ThrowIfNull(streamingPresenters);
            _streamingPresenters = streamingPresenters;
        }

        public VSCommanding.CommandState GetCommandState(TCommandArgs args)
        {
            return VSCommanding.CommandState.Available;
        }

        public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
        {
            var snapshotSpans = (args.TextView.Selection as ITextSelection)?.GetSnapshotSpansOnBuffer(args.SubjectBuffer);
            if (snapshotSpans == null)
                return false;

            if (snapshotSpans.Count == 1)
            {
                var selectedSpan = snapshotSpans [0];
                ITextSnapshot snapshot = args.SubjectBuffer.CurrentSnapshot;
                Document document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    // Do a find-refs at the *start* of the selection.  That way if the
                    // user has selected a symbol that has another symbol touching it
                    // on the right (i.e.  Goo++  ), then we'll do the find-refs on the
                    // symbol selected, not the symbol following.
                    if (TryExecuteCommand(selectedSpan.Start, document, context))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected abstract bool TryExecuteCommand(int caretPosition, Document document, CommandExecutionContext context);

        internal IStreamingFindUsagesPresenter GetStreamingPresenter()
        {
            try
            {
                return _streamingPresenters.FirstOrDefault()?.Value;
            }
            catch
            {
                return null;
            }
        }

        public virtual string DisplayName => nameof(AbstractNavigationCommandHandler<TCommandArgs>);
    }
}
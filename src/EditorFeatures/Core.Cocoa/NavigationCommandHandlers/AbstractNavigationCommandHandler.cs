// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationCommandHandlers
{
    internal abstract class AbstractNavigationCommandHandler<TCommandArgs> :
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
            var snapshotSpans = args.TextView.Selection?.GetSnapshotSpansOnBuffer(args.SubjectBuffer);
            if (snapshotSpans == null)
                return false;

            if (snapshotSpans.Count == 1)
            {
                var selectedSpan = snapshotSpans[0];
                var snapshot = args.SubjectBuffer.CurrentSnapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
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

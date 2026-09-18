// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal readonly partial struct RequestContext
{
    /// <summary>
    /// Shares a completed value or one resolution task across context copies, until any copy clears it.
    /// </summary>
    private sealed class SolutionContextState
    {
        private LspWorkspaceManager.LspContext? _value;
        private Task<LspWorkspaceManager.LspContext>? _task;

        public SolutionContextState(ValueTask<LspWorkspaceManager.LspContext> context)
        {
            // Consume the ValueTask once; it may be backed by a single-consumer IValueTaskSource.
            if (context.IsCompletedSuccessfully)
                _value = context.Result;
            else
                _task = context.AsTask();
        }

        public async ValueTask<LspWorkspaceManager.LspContext> GetValueAsync(CancellationToken cancellationToken)
        {
            Task<LspWorkspaceManager.LspContext> task;
            // This private state never escapes RequestContext, so it also serves as the gate.
            lock (this)
            {
                if (_value is { } value)
                    return value;

                task = _task ?? throw new InvalidOperationException();
            }

            var result = await task.WithCancellation(cancellationToken).ConfigureAwait(false);
            lock (this)
            {
                if (ReferenceEquals(_task, task))
                {
                    _value = result;
                    _task = null;
                }
                else if (_value is { } value)
                {
                    return value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return result;
        }

        public void Clear()
        {
            lock (this)
            {
                _value = null;
                _task = null;
            }
        }
    }
}

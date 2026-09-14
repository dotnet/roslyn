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
    private sealed class SolutionContext
    {
        private readonly object _gate = new();

        private LspWorkspaceManager.LspContext _initialValue;
        private Task<LspWorkspaceManager.LspContext>? _resolvedValue;
        private bool _isCleared;

        public SolutionContext(LspWorkspaceManager.DeferredLspContext context)
        {
            _initialValue = context.InitialValue;
            _resolvedValue = context.ResolvedValue;
        }

        public LspWorkspaceManager.LspContext GetInitialValue()
        {
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                return _initialValue;
            }
        }

        public async ValueTask<LspWorkspaceManager.LspContext> GetValueAsync(
            CancellationToken cancellationToken)
        {
            Task<LspWorkspaceManager.LspContext> resolvedValue;
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                Contract.ThrowIfNull(_resolvedValue);
                resolvedValue = _resolvedValue;
            }

            var value = await resolvedValue.WithCancellation(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                return value;
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _initialValue = default;
                _resolvedValue = null;
                _isCleared = true;
            }
        }
    }
}

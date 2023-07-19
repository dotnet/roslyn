// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

internal sealed class UIThreadOperationContextLongRunningOperationProgressAdapter(IUIThreadOperationContext operationContext)
    : ILongRunningOperationProgress
{
    /// <summary>
    /// Cached last retrieved scope information to avoid unnecessary reallocations.
    /// </summary>
    private Tuple<IUIThreadOperationScope, LongRunningOperationScope>? _lastScopePair;

    public void Dispose()
        => operationContext.Dispose();

    public ILongRunningOperationScope CurrentScope
    {
        get
        {
            var lastScopePair = _lastScopePair;

            var lastUIScope = operationContext.Scopes.Last();
            if (lastUIScope != lastScopePair?.Item1)
            {
                lastScopePair = Tuple.Create(lastUIScope, new LongRunningOperationScope(lastUIScope, allowDispose: false));
                _lastScopePair = lastScopePair;
            }

            return lastScopePair.Item2;
        }
    }

    public ILongRunningOperationScope AddScope(string description)
        => new LongRunningOperationScope(operationContext.AddScope(operationContext.AllowCancellation, description), allowDispose: true);

    private sealed class LongRunningOperationScope(IUIThreadOperationScope scope, bool allowDispose) : ILongRunningOperationScope
    {
        public IProgress<CodeAnalysis.Utilities.ProgressInfo> Progress { get; } = new Progress(scope.Progress);

        public void Dispose()
        {
            if (!allowDispose)
                throw new InvalidOperationException("Cannot call Dispose on a IOperationScope returned by IOperationContext.Scopes");

            scope.Dispose();
        }

        public string Description
        {
            get => scope.Description;
            set => scope.Description = value;
        }
    }

    private sealed class Progress(IProgress<VisualStudio.Utilities.ProgressInfo> progress) : IProgress<CodeAnalysis.Utilities.ProgressInfo>
    {
        public void Report(CodeAnalysis.Utilities.ProgressInfo value)
            => progress.Report(new VisualStudio.Utilities.ProgressInfo(value.CompletedItems, value.TotalItems));
    }
}

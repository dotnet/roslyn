// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// Wrapper around a <see cref="IUIThreadOperationContext"/> adapting it to be an <see cref="IOperationContext"/>.
    /// </summary>
    internal class UIOperationContext : IOperationContext
    {
        private readonly IUIThreadOperationContext _operationContext;

        public UIOperationContext(IUIThreadOperationContext operationContext)
        {
            _operationContext = operationContext;
        }

        public CancellationToken CancellationToken => _operationContext.UserCancellationToken;

        public string Description => _operationContext.Description;

        public IEnumerable<IOperationScope> Scopes => _operationContext.Scopes.Select(s => new UIOperationScope(s, allowDispose: false));

        public IOperationScope AddScope(string description)
            => new UIOperationScope(_operationContext.AddScope(allowCancellation: false, description), allowDispose: true);

        public void Dispose()
            => _operationContext.Dispose();

        private class UIOperationScope : IOperationScope
        {
            private readonly IUIThreadOperationScope _operationScope;
            private readonly bool _allowDispose;

            public UIOperationScope(IUIThreadOperationScope operationScope, bool allowDispose)
            {
                _operationScope = operationScope;
                _allowDispose = allowDispose;
            }

            public string Description
            {
                get => _operationScope.Description;
                set => _operationScope.Description = value;
            }

            public IProgress<CodeAnalysis.Utilities.ProgressInfo> Progress
                => new Progress(_operationScope.Progress);

            public void Dispose()
            {
                if (!_allowDispose)
                    throw new InvalidOperationException("Cannot call Dispose on a IOperationScope returned by IOperationContext.Scopes");

                _operationScope.Dispose();
            }
        }

        private class Progress : IProgress<CodeAnalysis.Utilities.ProgressInfo>
        {
            private readonly IProgress<VisualStudio.Utilities.ProgressInfo> _progress;

            public Progress(IProgress<VisualStudio.Utilities.ProgressInfo> progress)
            {
                _progress = progress;
            }

            public void Report(CodeAnalysis.Utilities.ProgressInfo value)
                => _progress.Report(new VisualStudio.Utilities.ProgressInfo(value.CompletedItems, value.TotalItems));
        }
    }
}

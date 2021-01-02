// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal interface IOperationContextFactory : IWorkspaceService
    {
        IOperationContext CreateOperationContext(string title, string description, bool allowCancellation, bool showProgress);
    }

    [ExportWorkspaceService(typeof(IOperationContextFactory)), Shared]
    internal class DefaultOperationContextFactory : IOperationContextFactory
    {
        [ImportingConstructor]
        public DefaultOperationContextFactory()
        {
        }

        public IOperationContext CreateOperationContext(string title, string description, bool allowCancellation, bool showProgress)
            => DefaultOperationContext.Instance;

        private class DefaultOperationContext : IOperationContext
        {
            public static readonly IOperationContext Instance = new DefaultOperationContext();

            private DefaultOperationContext()
            {
            }

            public string Description => "";

            public IEnumerable<IOperationScope> Scopes => Array.Empty<IOperationScope>();

            public IOperationScope AddScope(string description)
                => DefaultOperationScope.Instance;

            public void Dispose()
            {
            }
        }

        private class DefaultOperationScope : IOperationScope
        {
            public static readonly IOperationScope Instance = new DefaultOperationScope();

            private DefaultOperationScope()
            {
            }

            public string Description { get => ""; set { } }

            public IProgress<ProgressInfo> Progress => DefaultProgress.Instance;

            public void Dispose()
            {
            }
        }

        private class DefaultProgress : IProgress<ProgressInfo>
        {
            public static readonly IProgress<ProgressInfo> Instance = new DefaultProgress();

            private DefaultProgress()
            {
            }

            public void Report(ProgressInfo value)
            {
            }
        }
    }
}

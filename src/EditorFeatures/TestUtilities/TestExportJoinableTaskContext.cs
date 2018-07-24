// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Threading;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // Starting with 15.3 the editor took a dependency on JoinableTaskContext
    // in Text.Logic and Intellisense layers as an editor host provided service.
    internal class TestExportJoinableTaskContext
    {
        private JoinableTaskContext _joinableTaskContext;

        public TestExportJoinableTaskContext()
        {
            var synchronizationContext = SynchronizationContext.Current;
            try
            {
                if (synchronizationContext is AsyncTestSyncContext asyncTestSyncContext)
                {
                    SynchronizationContext innerSynchronizationContext = null;
                    asyncTestSyncContext.Send(
                        _ =>
                        {
                            innerSynchronizationContext = SynchronizationContext.Current;
                        },
                        null);

                    SynchronizationContext.SetSynchronizationContext(innerSynchronizationContext);
                }

                _joinableTaskContext = ThreadingContext.CreateJoinableTaskContext();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            }
        }

        [Export]
        private JoinableTaskContext JoinableTaskContext => _joinableTaskContext;
    }
}

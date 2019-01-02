// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// Implements <see cref="IThreadingContext"/>, which provides an implementation of
    /// <see cref="VisualStudio.Threading.JoinableTaskFactory"/> to Roslyn code.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="VisualStudio.Threading.JoinableTaskFactory"/> is constructed from the
    /// <see cref="VisualStudio.Threading.JoinableTaskContext"/> provided by the MEF container, if available. If no
    /// <see cref="VisualStudio.Threading.JoinableTaskContext"/> is available, a new instance is constructed using the
    /// synchronization context of the current thread as the main thread.</para>
    /// </remarks>
    [Export(typeof(IThreadingContext))]
    [Shared]
    internal sealed partial class ThreadingContext : IThreadingContext
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ThreadingContext([Import(AllowDefault = true)] JoinableTaskContext joinableTaskContext)
        {
            if (joinableTaskContext is null)
            {
                (joinableTaskContext, _) = CreateJoinableTaskContext();
            }

            HasMainThread = joinableTaskContext.MainThread.IsAlive;
            JoinableTaskContext = joinableTaskContext;
            JoinableTaskFactory = joinableTaskContext.Factory;
        }

        internal static (JoinableTaskContext joinableTaskContext, SynchronizationContext synchronizationContext) CreateJoinableTaskContext()
        {
            Thread mainThread;
            SynchronizationContext synchronizationContext;
            switch (ForegroundThreadDataInfo.CreateDefault(ForegroundThreadDataKind.Unknown))
            {
                case ForegroundThreadDataKind.JoinableTask:
                    throw new NotSupportedException($"A {nameof(VisualStudio.Threading.JoinableTaskContext)} already exists, but we have no way to obtain it.");

                case ForegroundThreadDataKind.Wpf:
                case ForegroundThreadDataKind.WinForms:
                case ForegroundThreadDataKind.MonoDevelopGtk:
                case ForegroundThreadDataKind.MonoDevelopXwt:
                case ForegroundThreadDataKind.StaUnitTest:
                    // The current thread is the main thread, and provides a suitable synchronization context
                    mainThread = Thread.CurrentThread;
                    synchronizationContext = SynchronizationContext.Current;
                    break;

                case ForegroundThreadDataKind.ForcedByPackageInitialize:
                case ForegroundThreadDataKind.Unknown:
                default:
                    // The current thread is not known to be the main thread; we have no way to know if the
                    // synchronization context of the current thread will behave in a manner consistent with main thread
                    // synchronization contexts, so we use DenyExecutionSynchronizationContext to track any attempted
                    // use of it.
                    var denyExecutionSynchronizationContext = new DenyExecutionSynchronizationContext(SynchronizationContext.Current);
                    mainThread = denyExecutionSynchronizationContext.MainThread;
                    synchronizationContext = denyExecutionSynchronizationContext;
                    break;
            }

            return (new JoinableTaskContext(mainThread, synchronizationContext), synchronizationContext);
        }

        /// <inheritdoc/>
        public bool HasMainThread
        {
            get;
        }

        /// <inheritdoc/>
        public JoinableTaskContext JoinableTaskContext
        {
            get;
        }

        /// <inheritdoc/>
        public JoinableTaskFactory JoinableTaskFactory
        {
            get;
        }
    }
}

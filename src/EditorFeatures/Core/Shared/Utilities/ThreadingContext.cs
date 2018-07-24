// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
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
               joinableTaskContext = CreateJoinableTaskContext();
            }

            HasMainThread = joinableTaskContext.MainThread.IsAlive;
            JoinableTaskContext = joinableTaskContext;
            JoinableTaskFactory = joinableTaskContext.Factory;
        }

        internal static JoinableTaskContext CreateJoinableTaskContext()
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
                    mainThread = Thread.CurrentThread;
                    synchronizationContext = SynchronizationContext.Current;
                    break;

                case ForegroundThreadDataKind.ForcedByPackageInitialize:
                case ForegroundThreadDataKind.Unknown:
                default:
                    var denyExecutionSynchronizationContext = new DenyExecutionSynchronizationContext(SynchronizationContext.Current);
                    mainThread = denyExecutionSynchronizationContext.MainThread;
                    synchronizationContext = denyExecutionSynchronizationContext;
                    break;
            }

            return new JoinableTaskContext(mainThread, synchronizationContext);
        }

        public bool HasMainThread
        {
            get;
        }

        public JoinableTaskContext JoinableTaskContext
        {
            get;
        }

        public JoinableTaskFactory JoinableTaskFactory
        {
            get;
        }
    }
}

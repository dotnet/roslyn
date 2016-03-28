// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IThreadHandlingFactory
    {
        public static IThreadHandling Create()
        {
            return new MockThreadHandling();
        }

        private class MockThreadHandling : IThreadHandling
        {
            private readonly JoinableTaskContextNode _context;
            private readonly JoinableTaskFactory _asyncPump;

            public MockThreadHandling()
            {
                _context = new JoinableTaskContextNode(new JoinableTaskContext());
                _asyncPump = _context.Factory;
            }

            public JoinableTaskFactory AsyncPump
            {
                get { return _asyncPump; }
            }

            public bool IsOnMainThread
            {
                get { return Thread.CurrentThread == _context.MainThread; }
            }

            public JoinableTaskContextNode JoinableTaskContext
            {
                get { return _context; }
            }

            public JoinableTaskFactory.MainThreadAwaitable SwitchToUIThread(StrongBox<bool> yielded)
            {
                return AsyncPump.SwitchToMainThreadAsync();
            }

            public void ExecuteSynchronously(Func<Task> asyncAction)
            {
                var task = asyncAction();
                task.GetAwaiter().GetResult();
            }

            public T ExecuteSynchronously<T>(Func<Task<T>> asyncAction)
            {
                var task = asyncAction();
                return task.GetAwaiter().GetResult();
            }

            public void VerifyOnUIThread()
            {
                throw new NotImplementedException();
            }

            public void Fork(Func<Task> asyncAction, JoinableTaskFactory factory = null, UnconfiguredProject unconfiguredProject = null, ConfiguredProject configuredProject = null, ErrorReportSettings watsonReportSettings = null, ProjectFaultSeverity faultSeverity = ProjectFaultSeverity.Recoverable, ForkOptions options = ForkOptions.Default)
            {
                throw new NotImplementedException();
            }
        }
    }
}

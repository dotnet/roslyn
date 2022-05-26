// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// Provides a <see cref="VisualStudio.Threading.JoinableTaskContext"/> which Roslyn code can use for transitioning
    /// to the main thread and/or waiting for asynchronous operations when required.
    /// </summary>
    internal interface IThreadingContext
    {
        /// <summary>
        /// Gets a value indicating whether the threading context is configured with a main thread that can be used for
        /// scheduling operations.
        /// </summary>
        /// <remarks>
        /// <para>Existence of a main thread is a requirement for correct runtime behavior (in production) of all types
        /// that depend on <see cref="IThreadingContext"/>, so code is generally not expected to check this property.
        /// However, in some lightweight testing scenarios the main thread will not be used, and the test code avoids
        /// setting up the main thread. This property improves the ability to detect incorrectly configured tests (where
        /// a main thread is expected but not provided) and produce a meaningful error for developers.</para>
        /// </remarks>
        bool HasMainThread
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="VisualStudio.Threading.JoinableTaskContext"/> for use in Roslyn code.
        /// </summary>
        JoinableTaskContext JoinableTaskContext
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="VisualStudio.Threading.JoinableTaskFactory"/> for use in Roslyn code.
        /// </summary>
        JoinableTaskFactory JoinableTaskFactory
        {
            get;
        }

        /// <summary>
        /// Gets a <see cref="CancellationToken"/> indicating that disposal has been requested for the threading
        /// context.
        /// </summary>
        CancellationToken DisposalToken
        {
            get;
        }

        /// <summary>
        /// Runs an asynchronous operation. If the operation is not complete prior to the time the threading context is
        /// disposed, the operation will block the shutdown.
        /// </summary>
        /// <remarks>
        /// <para>The callback function <paramref name="func"/> is invoked synchronously prior to this method returning.
        /// The entire asynchronous operation performed by <paramref name="func"/> will block shutdown of the threading
        /// context.</para>
        ///
        /// <para>If the threading context is already disposed at the time this is called, the operation is cancelled
        /// without calling <paramref name="func"/>.</para>
        /// </remarks>
        /// <param name="func">The callback function that performs an asynchronous operation.</param>
        JoinableTask RunWithShutdownBlockAsync(Func<CancellationToken, Task> func);
    }
}

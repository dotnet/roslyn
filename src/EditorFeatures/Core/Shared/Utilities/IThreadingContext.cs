// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}

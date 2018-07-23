// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    [Export(typeof(IThreadingContext))]
    [Shared]
    internal sealed class ThreadingContext : IThreadingContext
    {
        public static ThreadingContext Invalid { get; } = new ThreadingContext();

        public bool HasMainThread => throw new NotImplementedException();

        public JoinableTaskContext JoinableTaskContext => throw new NotImplementedException();

        public JoinableTaskFactory JoinableTaskFactory => throw new NotImplementedException();
    }
}

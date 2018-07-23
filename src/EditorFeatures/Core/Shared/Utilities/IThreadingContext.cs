// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal interface IThreadingContext
    {
        bool HasMainThread
        {
            get;
        }

        JoinableTaskContext JoinableTaskContext
        {
            get;
        }

        JoinableTaskFactory JoinableTaskFactory
        {
            get;
        }
    }
}

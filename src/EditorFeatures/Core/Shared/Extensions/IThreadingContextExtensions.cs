// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IThreadingContextExtensions
    {
        public static void ThrowIfNotOnUIThread(this IThreadingContext threadingContext)
        {
            Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);
        }
    }
}

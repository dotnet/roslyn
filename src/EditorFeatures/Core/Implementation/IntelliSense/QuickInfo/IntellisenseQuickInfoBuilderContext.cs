// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    /// <summary>
    /// Context to build content for quick info item for intellisense.
    /// </summary>
    internal sealed class IntellisenseQuickInfoBuilderContext
    {
        public IntellisenseQuickInfoBuilderContext(
            Document document,
            IThreadingContext? threadingContext,
            Lazy<IStreamingFindUsagesPresenter>? streamingPresenter)
        {
            Document = document;
            ThreadingContext = threadingContext;
            StreamingPresenter = streamingPresenter;
        }

        public Document Document { get; }
        public IThreadingContext? ThreadingContext { get; }
        public Lazy<IStreamingFindUsagesPresenter>? StreamingPresenter { get; }
    }
}

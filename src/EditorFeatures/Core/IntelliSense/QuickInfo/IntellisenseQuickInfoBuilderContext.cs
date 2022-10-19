// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    /// <summary>
    /// Context to build content for quick info item for intellisense.
    /// </summary>
    internal sealed class IntellisenseQuickInfoBuilderContext
    {
        public IntellisenseQuickInfoBuilderContext(
            Document document,
            ClassificationOptions classificationOptions,
            IThreadingContext? threadingContext,
            IUIThreadOperationExecutor? operationExecutor,
            IAsynchronousOperationListener? asynchronousOperationListener,
            Lazy<IStreamingFindUsagesPresenter>? streamingPresenter)
        {
            Document = document;
            ClassificationOptions = classificationOptions;
            ThreadingContext = threadingContext;
            OperationExecutor = operationExecutor;
            StreamingPresenter = streamingPresenter;
            AsynchronousOperationListener = asynchronousOperationListener;
        }

        public Document Document { get; }
        public ClassificationOptions ClassificationOptions { get; }
        public IThreadingContext? ThreadingContext { get; }
        public IUIThreadOperationExecutor? OperationExecutor { get; }
        public IAsynchronousOperationListener? AsynchronousOperationListener { get; }
        public Lazy<IStreamingFindUsagesPresenter>? StreamingPresenter { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

/// <summary>
/// Context to build content for quick info item for intellisense.
/// </summary>
internal sealed class IntellisenseQuickInfoBuilderContext(
    Document document,
    ClassificationOptions classificationOptions,
    LineFormattingOptions lineFormattingOptions,
    IThreadingContext? threadingContext,
    IUIThreadOperationExecutor? operationExecutor,
    IAsynchronousOperationListener? asynchronousOperationListener,
    Lazy<IStreamingFindUsagesPresenter>? streamingPresenter)
{
    public Document Document { get; } = document;
    public ClassificationOptions ClassificationOptions { get; } = classificationOptions;
    public LineFormattingOptions LineFormattingOptions { get; } = lineFormattingOptions;
    public IThreadingContext? ThreadingContext { get; } = threadingContext;
    public IUIThreadOperationExecutor? OperationExecutor { get; } = operationExecutor;
    public IAsynchronousOperationListener? AsynchronousOperationListener { get; } = asynchronousOperationListener;
    public Lazy<IStreamingFindUsagesPresenter>? StreamingPresenter { get; } = streamingPresenter;
}

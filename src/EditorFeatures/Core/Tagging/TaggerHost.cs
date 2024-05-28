// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

[Export(typeof(TaggerHost)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TaggerHost(
    IThreadingContext threadingContext,
    IGlobalOptionService globalOptions,
    [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
    IAsynchronousOperationListenerProvider asyncListenerProvider)
{
    public IThreadingContext ThreadingContext { get; } = threadingContext;
    public IGlobalOptionService GlobalOptions { get; } = globalOptions;
    public ITextBufferVisibilityTracker? VisibilityTracker { get; } = visibilityTracker;
    public IAsynchronousOperationListenerProvider AsyncListenerProvider { get; } = asyncListenerProvider;
    public TaggerMainThreadManager TaggerMainThreadManager { get; } = new(threadingContext, asyncListenerProvider);
}

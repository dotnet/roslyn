// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;

namespace Microsoft.CodeAnalysis.Classification;

/// <summary>
/// This is the tagger we use for view classification scenarios.  It is used for classifying code in the editor.  We
/// use a view tagger so that we can only classify what's in view, and not the whole file.  Intentionally not
/// exported.  It is consumed by the <see cref="TotalClassificationTaggerProvider"/> instead.
/// </summary>
internal partial class SemanticClassificationViewTaggerProvider(
    IThreadingContext threadingContext,
    ClassificationTypeMap typeMap,
    IGlobalOptionService globalOptions,
    [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
    IAsynchronousOperationListenerProvider listenerProvider) : AbstractSemanticOrEmbeddedClassificationViewTaggerProvider(threadingContext, typeMap, globalOptions, visibilityTracker, listenerProvider, ClassificationType.Semantic)
{
}

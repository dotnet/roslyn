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
/// This is the tagger we use for classifying the embedded language string literals currently visible in the editor
/// view.  Intentionally not exported.  It is consumed by the <see cref="TotalClassificationTaggerProvider"/>
/// instead.
/// </summary>
internal partial class EmbeddedLanguageClassificationViewTaggerProvider(
    IThreadingContext threadingContext,
    ClassificationTypeMap typeMap,
    IGlobalOptionService globalOptions,
    [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
    IAsynchronousOperationListenerProvider listenerProvider) : AbstractSemanticOrEmbeddedClassificationViewTaggerProvider(threadingContext, typeMap, globalOptions, visibilityTracker, listenerProvider, ClassificationType.EmbeddedLanguage)
{
}

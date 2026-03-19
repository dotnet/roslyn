// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

/// <summary>
/// This is the tagger we use for view classification scenarios.  It is used for classifying code in the editor.  We
/// use a view tagger so that we can only classify what's in view, and not the whole file.  Intentionally not
/// exported.  It is consumed by the <see cref="TotalClassificationTaggerProvider"/> instead.
/// </summary>
internal sealed class SemanticClassificationViewTaggerProvider(TaggerHost taggerHost, ClassificationTypeMap typeMap)
    : AbstractSemanticOrEmbeddedClassificationViewTaggerProvider(taggerHost, typeMap, ClassificationType.Semantic);

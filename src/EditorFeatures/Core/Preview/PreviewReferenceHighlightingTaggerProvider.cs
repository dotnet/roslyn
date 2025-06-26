// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview;

/// <summary>
/// Special tagger we use for previews that is told precisely which spans to
/// reference highlight.
/// </summary>
[Export(typeof(ITaggerProvider))]
[TagType(typeof(NavigableHighlightTag))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TextViewRole(TextViewRoles.PreviewRole)]
internal sealed class PreviewReferenceHighlightingTaggerProvider
    : AbstractPreviewTaggerProvider<NavigableHighlightTag>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PreviewReferenceHighlightingTaggerProvider()
        : base(PredefinedPreviewTaggerKeys.ReferenceHighlightingSpansKey, ReferenceHighlightTag.Instance)
    {
    }
}

[Export(typeof(ITaggerProvider))]
[TagType(typeof(NavigableHighlightTag))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TextViewRole(TextViewRoles.PreviewRole)]
internal sealed class PreviewWrittenReferenceHighlightingTaggerProvider
    : AbstractPreviewTaggerProvider<NavigableHighlightTag>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PreviewWrittenReferenceHighlightingTaggerProvider()
        : base(PredefinedPreviewTaggerKeys.WrittenReferenceHighlightingSpansKey, WrittenReferenceHighlightTag.Instance)
    {
    }
}

[Export(typeof(ITaggerProvider))]
[TagType(typeof(NavigableHighlightTag))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TextViewRole(TextViewRoles.PreviewRole)]
internal sealed class PreviewDefinitionHighlightingTaggerProvider
    : AbstractPreviewTaggerProvider<NavigableHighlightTag>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PreviewDefinitionHighlightingTaggerProvider()
        : base(PredefinedPreviewTaggerKeys.DefinitionHighlightingSpansKey, DefinitionHighlightTag.Instance)
    {
    }
}

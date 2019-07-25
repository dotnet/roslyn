// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    /// <summary>
    /// Special tagger we use for previews that is told precisely which spans to
    /// reference highlight.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(NavigableHighlightTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TextViewRole(TextViewRoles.PreviewRole)]
    internal class PreviewReferenceHighlightingTaggerProvider
        : AbstractPreviewTaggerProvider<NavigableHighlightTag>
    {
        [ImportingConstructor]
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
    internal class PreviewWrittenReferenceHighlightingTaggerProvider
        : AbstractPreviewTaggerProvider<NavigableHighlightTag>
    {
        [ImportingConstructor]
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
    internal class PreviewDefinitionHighlightingTaggerProvider
        : AbstractPreviewTaggerProvider<NavigableHighlightTag>
    {
        [ImportingConstructor]
        public PreviewDefinitionHighlightingTaggerProvider()
            : base(PredefinedPreviewTaggerKeys.DefinitionHighlightingSpansKey, DefinitionHighlightTag.Instance)
        {
        }
    }
}

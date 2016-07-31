using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    /// <summary>
    /// Special tagger we use for previews that is told precisely which spans to
    /// reference highlight.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(NavigableHighlightTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(TextViewRoles.PreviewRole)]
    internal class PreviewReferenceHighlightingTaggerProvider
        : AbstractPreviewTaggerProvider<NavigableHighlightTag>
    {
        [ImportingConstructor]
        public PreviewReferenceHighlightingTaggerProvider() :
            base(PredefinedPreviewTaggerKeys.ReferenceHighlightingSpansKey, ReferenceHighlightTag.Instance)
        {
        }
    }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(NavigableHighlightTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(TextViewRoles.PreviewRole)]
    internal class PreviewDefinitionHighlightingTaggerProvider
        : AbstractPreviewTaggerProvider<NavigableHighlightTag>
    {
        [ImportingConstructor]
        public PreviewDefinitionHighlightingTaggerProvider() :
            base(PredefinedPreviewTaggerKeys.DefinitionHighlightingSpansKey, DefinitionHighlightTag.Instance)
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(WrittenReferenceHighlightTag.TagId)]
    [UserVisible(true)]
    internal class WrittenReferenceHighlightTagDefinition : MarkerFormatDefinition
    {
        public WrittenReferenceHighlightTagDefinition()
        {
            // NOTE: This is the same color used by the editor for reference highlighting
            this.BackgroundColor = Color.FromRgb(219, 224, 204);
            this.DisplayName = EditorFeaturesResources.HighlightedWrittenReference;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal partial class DefinitionItem
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionItem"/> that sits on top of a 
        /// <see cref="DocumentSpan"/>.
        /// </summary>
        // internal for testing purposes.
        internal sealed class DefaultDefinitionItem : CommonDefinitionItem
        {
            public DefaultDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<TaggedText> nameDisplayParts,
                ImmutableArray<TaggedText> originationParts,
                ImmutableArray<DocumentSpan> sourceSpans,
                ImmutableDictionary<string, string> properties,
                ImmutableDictionary<string, string> displayableProperties,
                bool displayIfNoReferences)
                : base(tags, displayParts, nameDisplayParts, originationParts,
                       sourceSpans, properties, displayableProperties, displayIfNoReferences)
            {
            }

            protected override bool CanNavigateToSource(CancellationToken cancellationToken)
                => SourceSpans[0].CanNavigateTo(cancellationToken);

            protected override bool TryNavigateToSource(bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
                => SourceSpans[0].TryNavigateTo(showInPreviewTab, activateTab, cancellationToken);

            public DetachedDefinitionItem Detach()
                => new(Tags, DisplayParts, NameDisplayParts, OriginationParts, SourceSpans.FirstOrDefault(), Properties, DisplayableProperties, DisplayIfNoReferences);
        }
    }
}

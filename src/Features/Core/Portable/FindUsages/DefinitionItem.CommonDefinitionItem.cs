// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal partial class DefinitionItem
    {
        internal abstract class CommonDefinitionItem : DefinitionItem
        {
            internal sealed override bool IsExternal => false;

            public CommonDefinitionItem(
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

            protected abstract bool CanNavigateToSource(CancellationToken cancellationToken);
            protected abstract bool TryNavigateToSource(bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken);

            public sealed override bool CanNavigateTo(Workspace workspace, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return CanNavigateToMetadataSymbol(workspace, symbolKey, Properties);

                return CanNavigateToSource(cancellationToken);
            }

            public sealed override bool TryNavigateTo(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                if (Properties.ContainsKey(NonNavigable))
                    return false;

                if (Properties.TryGetValue(MetadataSymbolKey, out var symbolKey))
                    return TryNavigateToMetadataSymbol(workspace, symbolKey, Properties);

                return TryNavigateToSource(showInPreviewTab, activateTab, cancellationToken);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal partial class DefinitionItem
    {
        internal sealed class DetachedDefinitionItem : CommonDefinitionItem
        {
            private readonly Workspace? _workspace;
            private readonly DocumentId? _documentId;
            private readonly TextSpan _sourceSpan;

            public DetachedDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<TaggedText> nameDisplayParts,
                ImmutableArray<TaggedText> originationParts,
                DocumentSpan sourceSpan,
                ImmutableDictionary<string, string> properties,
                ImmutableDictionary<string, string> displayableProperties,
                bool displayIfNoReferences)
                : base(tags,
                       displayParts,
                       nameDisplayParts,
                       originationParts,
                       ImmutableArray<DocumentSpan>.Empty,
                       properties,
                       displayableProperties,
                       displayIfNoReferences)
            {
                _workspace = sourceSpan.Document?.Project.Solution.Workspace;
                _documentId = sourceSpan.Document?.Id;
                _sourceSpan = sourceSpan.SourceSpan;
            }

            protected override bool CanNavigateToSource(CancellationToken cancellationToken)
            {
                if (_workspace == null)
                    return false;

                var document = _workspace.CurrentSolution.GetDocument(_documentId);
                if (document == null)
                    return false;

                var documentSpan = new DocumentSpan(document, _sourceSpan);
                return documentSpan.CanNavigateTo(cancellationToken);
            }

            protected override bool TryNavigateToSource(bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                if (_workspace == null)
                    return false;

                var document = _workspace.CurrentSolution.GetDocument(_documentId);
                if (document == null)
                    return false;

                var documentSpan = new DocumentSpan(document, _sourceSpan);
                return documentSpan.TryNavigateTo(showInPreviewTab, activateTab, cancellationToken);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    internal class ExternalFilePeekableItem : PeekableItem
    {
        private readonly FileLinePositionSpan _span;
        private readonly IPeekRelationship _relationship;

        public ExternalFilePeekableItem(
            FileLinePositionSpan span,
            IPeekRelationship relationship,
            IPeekResultFactory peekResultFactory)
            : base(peekResultFactory)
        {
            _span = span;
            _relationship = relationship;
        }

        public override IEnumerable<IPeekRelationship> Relationships
        {
            get { return SpecializedCollections.SingletonEnumerable(_relationship); }
        }

        public override IPeekResultSource GetOrCreateResultSource(string relationshipName)
            => new ResultSource(this);

        private sealed class ResultSource : IPeekResultSource
        {
            private readonly ExternalFilePeekableItem _peekableItem;

            public ResultSource(ExternalFilePeekableItem peekableItem)
                => _peekableItem = peekableItem;

            public void FindResults(string relationshipName, IPeekResultCollection resultCollection, CancellationToken cancellationToken, IFindPeekResultsCallback callback)
            {
                if (relationshipName != _peekableItem._relationship.Name)
                {
                    return;
                }

                resultCollection.Add(PeekHelpers.CreateDocumentPeekResult(_peekableItem._span.Path, _peekableItem._span.Span, _peekableItem._span.Span, _peekableItem.PeekResultFactory));
            }
        }
    }
}

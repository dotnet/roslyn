﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public sealed class SymbolItem : RoslynNavigationBarItem
        {
            public readonly string Name;
            public readonly bool IsObsolete;

            public readonly SymbolItemLocation Location;

            public SymbolItem(
                string name,
                string text,
                Glyph glyph,
                bool isObsolete,
                SymbolItemLocation location,
                ImmutableArray<RoslynNavigationBarItem> childItems = default,
                int indent = 0,
                bool bolded = false)
                : base(
                      RoslynNavigationBarItemKind.Symbol,
                      text,
                      glyph,
                      bolded,
                      grayed: location.OtherDocumentInfo != null,
                      indent,
                      childItems)
            {

                Name = name;
                IsObsolete = isObsolete;
                Location = location;
            }

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.SymbolItem(Text, Glyph, Name, IsObsolete, Location, SerializableNavigationBarItem.Dehydrate(ChildItems), Indent, Bolded, Grayed);
        }

        [DataContract]
        public readonly struct SymbolItemLocation
        {
            /// <summary>
            /// The entity spans and navigation span in the originating document where this symbol was found.  Any time
            /// the caret is within any of the entity spans, the item should be appropriately 'selected' in whatever UI
            /// is displaying these.  The navigation span is the location in the starting document that should be
            /// navigated to when this item is selected If this symbol's location is in another document then this will
            /// be <see langword="null"/>.
            /// </summary>
            /// <remarks>Exactly one of <see cref="InDocumentInfo"/> and <see cref="OtherDocumentInfo"/> will be
            /// non-null.</remarks>
            [DataMember(Order = 0)]
            public readonly (ImmutableArray<TextSpan> spans, TextSpan navigationSpan)? InDocumentInfo;

            /// <summary>
            /// The document and navigation span this item should navigate to when the definition is not in the
            /// originating document. This is used for partial symbols where a child symbol is declared in another file,
            /// but should still be shown in the UI when in a part in a different file.
            /// </summary>
            /// <remarks>Exactly one of <see cref="InDocumentInfo"/> and <see cref="OtherDocumentInfo"/> will be
            /// non-null.</remarks>
            [DataMember(Order = 1)]
            public readonly (DocumentId documentId, TextSpan navigationSpan)? OtherDocumentInfo;

            public SymbolItemLocation(
                (ImmutableArray<TextSpan> spans, TextSpan navigationSpan)? inDocumentInfo,
                (DocumentId documentId, TextSpan navigationSpan)? otherDocumentInfo)
            {
                Contract.ThrowIfTrue(inDocumentInfo == null && otherDocumentInfo == null, "Both locations were null");
                Contract.ThrowIfTrue(inDocumentInfo != null && otherDocumentInfo != null, "Both locations were not null");

                if (inDocumentInfo != null)
                {
                    Contract.ThrowIfTrue(inDocumentInfo.Value.spans.IsEmpty, "If location is in document, it must have non-empty spans");
                }

                InDocumentInfo = inDocumentInfo;
                OtherDocumentInfo = otherDocumentInfo;
            }
        }
    }
}

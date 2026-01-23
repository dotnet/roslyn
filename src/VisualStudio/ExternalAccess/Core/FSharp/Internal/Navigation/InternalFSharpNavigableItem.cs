// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Navigation;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Navigation;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
#endif

internal class InternalFSharpNavigableItem : INavigableItem
{
    private readonly INavigableItem.NavigableDocument _navigableDocument;

    public InternalFSharpNavigableItem(FSharpNavigableItem item)
    {
        Glyph = FSharpGlyphHelpers.ConvertTo(item.Glyph);
        DisplayTaggedParts = item.DisplayTaggedParts;
        Document = item.Document;
        _navigableDocument = INavigableItem.NavigableDocument.FromDocument(item.Document);
        SourceSpan = item.SourceSpan;
    }

    public Glyph Glyph { get; }

    public ImmutableArray<TaggedText> DisplayTaggedParts { get; }

    public bool DisplayFileLocation => true;

    public bool IsImplicitlyDeclared => false;

    public Document Document { get; }

    INavigableItem.NavigableDocument INavigableItem.Document => _navigableDocument;

    public TextSpan SourceSpan { get; }

    public bool IsStale => false;

    public ImmutableArray<INavigableItem> ChildItems => [];
}

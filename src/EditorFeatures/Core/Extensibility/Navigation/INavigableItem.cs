// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Navigation
{
    internal interface INavigableItem
    {
        Glyph Glyph { get; }
        string DisplayName { get; }

        Document Document { get; }
        TextSpan SourceSpan { get; }

        ImmutableArray<INavigableItem> ChildItems { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public abstract class AbstractGenerateCodeItem : RoslynNavigationBarItem
        {
            public readonly SymbolKey DestinationTypeSymbolKey;

            protected AbstractGenerateCodeItem(RoslynNavigationBarItemKind kind, string text, Glyph glyph, SymbolKey destinationTypeSymbolKey)
                : base(kind, text, glyph, bolded: false, grayed: false, indent: 0, childItems: default, ImmutableArray<TextSpan>.Empty)
            {
                DestinationTypeSymbolKey = destinationTypeSymbolKey;
            }
        }
    }
}

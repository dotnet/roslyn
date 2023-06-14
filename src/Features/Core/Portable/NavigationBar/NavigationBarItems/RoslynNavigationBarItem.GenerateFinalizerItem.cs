// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public class GenerateFinalizer(string text, SymbolKey destinationTypeSymbolKey) : AbstractGenerateCodeItem(RoslynNavigationBarItemKind.GenerateFinalizer, text, Glyph.MethodProtected, destinationTypeSymbolKey), IEquatable<GenerateFinalizer>
        {
            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.GenerateFinalizer(Text, DestinationTypeSymbolKey);

            public override bool Equals(object? obj)
                => Equals(obj as GenerateFinalizer);

            public bool Equals(GenerateFinalizer? other)
                => base.Equals(other);

            public override int GetHashCode()
                => throw new NotImplementedException();
        }
    }
}

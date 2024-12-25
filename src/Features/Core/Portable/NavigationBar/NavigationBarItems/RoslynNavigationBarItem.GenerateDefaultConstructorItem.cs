// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.NavigationBar;

internal abstract partial class RoslynNavigationBarItem
{
    public class GenerateDefaultConstructor(string text, SymbolKey destinationTypeSymbolKey) : AbstractGenerateCodeItem(RoslynNavigationBarItemKind.GenerateDefaultConstructor, text, Glyph.MethodPublic, destinationTypeSymbolKey), IEquatable<GenerateDefaultConstructor>
    {
        protected internal override SerializableNavigationBarItem Dehydrate()
            => SerializableNavigationBarItem.GenerateDefaultConstructor(Text, DestinationTypeSymbolKey);

        public override bool Equals(object? obj)
            => Equals(obj as GenerateDefaultConstructor);

        public bool Equals(GenerateDefaultConstructor? other)
            => base.Equals(other);

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}

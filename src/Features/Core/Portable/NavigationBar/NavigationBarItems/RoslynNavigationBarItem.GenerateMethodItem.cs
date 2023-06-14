// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public class GenerateMethod(string text, Glyph glyph, SymbolKey destinationTypeSymbolId, SymbolKey methodToReplicateSymbolId) : AbstractGenerateCodeItem(RoslynNavigationBarItemKind.GenerateMethod, text, glyph, destinationTypeSymbolId), IEquatable<GenerateMethod>
        {
            public readonly SymbolKey MethodToReplicateSymbolKey = methodToReplicateSymbolId;

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.GenerateMethod(Text, Glyph, DestinationTypeSymbolKey, MethodToReplicateSymbolKey);

            public override bool Equals(object? obj)
                => Equals(obj as GenerateMethod);

            public bool Equals(GenerateMethod? other)
                => base.Equals(other) &&
                   MethodToReplicateSymbolKey.Equals(other.MethodToReplicateSymbolKey);

            public override int GetHashCode()
                => throw new NotImplementedException();
        }
    }
}

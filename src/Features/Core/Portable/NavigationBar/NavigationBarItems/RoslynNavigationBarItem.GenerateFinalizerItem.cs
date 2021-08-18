// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public class GenerateFinalizer : AbstractGenerateCodeItem
        {
            public GenerateFinalizer(string text, SymbolKey destinationTypeSymbolKey)
                : base(RoslynNavigationBarItemKind.GenerateFinalizer, text, Glyph.MethodProtected, destinationTypeSymbolKey)
            {
            }

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.GenerateFinalizer(Text, DestinationTypeSymbolKey);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public class GenerateEventHandlerItem : AbstractGenerateCodeItem
        {
            public readonly string ContainerName;
            public readonly SymbolKey EventSymbolKey;

            public GenerateEventHandlerItem(string eventName, Glyph glyph, string containerName, SymbolKey eventSymbolKey, SymbolKey destinationTypeSymbolKey)
                : base(RoslynNavigationBarItemKind.GenerateEventHandler, eventName, glyph, destinationTypeSymbolKey)
            {
                ContainerName = containerName;
                EventSymbolKey = eventSymbolKey;
            }
        }
    }
}

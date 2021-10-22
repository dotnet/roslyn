﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public class GenerateEventHandler : AbstractGenerateCodeItem, IEquatable<GenerateEventHandler>
        {
            public readonly string ContainerName;
            public readonly SymbolKey EventSymbolKey;

            public GenerateEventHandler(string text, Glyph glyph, string containerName, SymbolKey eventSymbolKey, SymbolKey destinationTypeSymbolKey)
                : base(RoslynNavigationBarItemKind.GenerateEventHandler, text, glyph, destinationTypeSymbolKey)
            {
                ContainerName = containerName;
                EventSymbolKey = eventSymbolKey;
            }

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.GenerateEventHandler(Text, Glyph, ContainerName, EventSymbolKey, DestinationTypeSymbolKey);

            public override bool Equals(object? obj)
                => Equals(obj as GenerateEventHandler);

            public bool Equals(GenerateEventHandler? other)
                => base.Equals(other) &&
                   ContainerName == other.ContainerName &&
                   EventSymbolKey.Equals(other.EventSymbolKey);

            public override int GetHashCode()
                => throw new NotImplementedException();
        }
    }
}

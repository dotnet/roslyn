// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        [DataContract]
        public class GenerateEventHandler : AbstractGenerateCodeItem
        {
            [DataMember(Order = 9)]
            public readonly string ContainerName;

            [DataMember(Order = 10)]
            public readonly SymbolKey EventSymbolKey;

            public GenerateEventHandler(string eventName, Glyph glyph, string containerName, SymbolKey eventSymbolKey, SymbolKey destinationTypeSymbolKey)
                : base(RoslynNavigationBarItemKind.GenerateEventHandler, eventName, glyph, destinationTypeSymbolKey)
            {
                ContainerName = containerName;
                EventSymbolKey = eventSymbolKey;
            }
        }
    }
}

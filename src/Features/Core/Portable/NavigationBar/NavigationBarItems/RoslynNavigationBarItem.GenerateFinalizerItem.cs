// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        [DataContract]
        public class GenerateFinalizer : AbstractGenerateCodeItem
        {
            public GenerateFinalizer(string text, SymbolKey destinationTypeSymbolKey)
                : base(RoslynNavigationBarItemKind.GenerateFinalizer, text, Glyph.MethodProtected, destinationTypeSymbolKey)
            {
            }
        }
    }
}

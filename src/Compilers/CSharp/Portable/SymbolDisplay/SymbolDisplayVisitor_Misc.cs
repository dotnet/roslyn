// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor
    {
        internal override void VisitPreprocessing(IPreprocessingSymbol symbol)
        {
            var part = new SymbolDisplayPart(SymbolDisplayPartKind.PreprocessingName, symbol, symbol.Name);
            builder.Add(part);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor
    {
        private void AddPreprocessingName(IPreprocessingSymbol preprocessing)
        {
            var part = new SymbolDisplayPart(SymbolDisplayPartKind.PreprocessingName, preprocessing, preprocessing.Name);
            builder.Add(part);
        }

        /// <summary>
        /// Visits a symbol, and specifically handles symbol types that do not support visiting.
        /// </summary>
        /// <param name="symbol">The symbol to visit.</param>
        public void VisitSymbol(ISymbol symbol)
        {
            if (symbol is IPreprocessingSymbol preprocessingSymbol)
            {
                AddPreprocessingName(preprocessingSymbol);
                return;
            }

            symbol.Accept(this);
        }
    }
}

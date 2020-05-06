// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static ILabelSymbol Label(
            string name)
            => new LabelSymbol(
                name);

        public static ILabelSymbol With(
            this ILabelSymbol label,
            Optional<string> name = default)
        {
            return new LabelSymbol(
                name.GetValueOr(label.Name));
        }

        private class LabelSymbol : Symbol, ILabelSymbol
        {
            public LabelSymbol(
                string name)
            {
                Name = name;
            }

            public override string Name { get; }
            public override SymbolKind Kind => SymbolKind.Label;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitLabel(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitLabel(this);

            #region default implementation

            public IMethodSymbol ContainingMethod => throw new NotImplementedException();

            #endregion
        }
    }
}

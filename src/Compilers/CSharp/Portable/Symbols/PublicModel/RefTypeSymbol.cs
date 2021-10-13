// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    // PROTOTYPE(delegate-type-arg): public model?
    // internal sealed class RefTypeSymbol : TypeSymbol
    // {
    //     private readonly Symbols.RefTypeSymbol _underlying;

    //     internal override Symbols.TypeSymbol UnderlyingTypeSymbol => _underlying;
    //     internal override Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;
    //     internal override CSharp.Symbol UnderlyingSymbol => _underlying;

    //     protected override void Accept(SymbolVisitor visitor)
    //     {
    //         _underlying.ReferencedTypeWithAnnotations.Accept
    //     }

    //     protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     protected override ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation)
    //     {
    //         throw new System.NotImplementedException();
    //     }
    // }
}

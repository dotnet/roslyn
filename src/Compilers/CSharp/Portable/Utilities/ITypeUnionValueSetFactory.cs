// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal interface ITypeUnionValueSetFactory
    {
        TypeUnionValueSet AllValues(ConversionsBase conversions);
        TypeUnionValueSet FromTypeMatch(TypeSymbol type, ConversionsBase conversions, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo);
        TypeUnionValueSet FromNullMatch(ConversionsBase conversions);
        TypeUnionValueSet FromNonNullMatch(ConversionsBase conversions);
    }
}

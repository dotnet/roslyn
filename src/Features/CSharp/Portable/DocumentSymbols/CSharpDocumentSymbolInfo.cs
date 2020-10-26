// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentSymbols;

namespace Microsoft.CodeAnalysis.CSharp.DocumentSymbols
{
    internal class CSharpDocumentSymbolInfo : DocumentSymbolInfo
    {
        private static readonly SymbolDisplayFormat s_typeFormat =
            SymbolDisplayFormat.CSharpErrorMessageFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeVariance);

        private static readonly SymbolDisplayFormat s_memberFormat =
            new(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                               SymbolDisplayMemberOptions.IncludeExplicitInterface,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                                  SymbolDisplayParameterOptions.IncludeName |
                                  SymbolDisplayParameterOptions.IncludeDefaultValue |
                                  SymbolDisplayParameterOptions.IncludeParamsRefOut,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                      SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral |
                                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public CSharpDocumentSymbolInfo(ISymbol Symbol, ImmutableArray<DocumentSymbolInfo> ChildSymbols)
            : base(Symbol, ChildSymbols)
        { }

        protected override string FormatSymbol()
        {
            return Symbol.ToDisplayString(Symbol is ITypeSymbol ? s_typeFormat : s_memberFormat);
        }
    }
}

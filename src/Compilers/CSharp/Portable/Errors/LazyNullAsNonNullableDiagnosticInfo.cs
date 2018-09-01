// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyNullAsNonNullableDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly TypeSymbolWithAnnotations _possiblyNullableTypeSymbol;

        internal LazyNullAsNonNullableDiagnosticInfo (TypeSymbolWithAnnotations possiblyNullableTypeSymbol)
        {
            _possiblyNullableTypeSymbol = possiblyNullableTypeSymbol;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            if (_possiblyNullableTypeSymbol.IsNullable == false)
            {
                return new CSDiagnosticInfo(ErrorCode.WRN_NullAsNonNullable);
            }

            return null;
        }
    }
}

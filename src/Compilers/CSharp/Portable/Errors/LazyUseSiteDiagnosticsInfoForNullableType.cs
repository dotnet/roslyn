// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyUseSiteDiagnosticsInfoForNullableType : LazyDiagnosticInfo
    {
        private readonly TypeSymbolWithAnnotations _possiblyNullableTypeSymbol;

        internal LazyUseSiteDiagnosticsInfoForNullableType(TypeSymbolWithAnnotations possiblyNullableTypeSymbol)
        {
            _possiblyNullableTypeSymbol = possiblyNullableTypeSymbol;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            if (_possiblyNullableTypeSymbol.IsNullableType())
            {
                return _possiblyNullableTypeSymbol.TypeSymbol.OriginalDefinition.GetUseSiteDiagnostic();
            }
            else if (_possiblyNullableTypeSymbol.TypeSymbol.IsUnconstrainedTypeParameter())
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_NullableUnconstrainedTypeParameter);
            }

            return null;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LasyUseSiteDiagnosticsInfoForNullableType : LasyDiagnosticsInfo
    {
        private readonly TypeSymbolWithAnnotations _possiblyNullableTypeSymbol;

        internal LasyUseSiteDiagnosticsInfoForNullableType(TypeSymbolWithAnnotations possiblyNullableTypeSymbol)
        {
            _possiblyNullableTypeSymbol = possiblyNullableTypeSymbol;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            if (_possiblyNullableTypeSymbol.IsNullableType())
            {
                return _possiblyNullableTypeSymbol.TypeSymbol.OriginalDefinition.GetUseSiteDiagnostic();
            }

            return null;
        }
    }
}

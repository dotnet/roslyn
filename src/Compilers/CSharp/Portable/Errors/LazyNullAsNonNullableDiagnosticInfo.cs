// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyNullAsNonNullableDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly TypeSymbolWithAnnotations _possiblyNullableType;

        internal LazyNullAsNonNullableDiagnosticInfo (TypeSymbolWithAnnotations possiblyNullableType)
        {
            _possiblyNullableType = possiblyNullableType;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            if (_possiblyNullableType.IsNullable == false)
            {
                return new CSDiagnosticInfo(ErrorCode.WRN_NullAsNonNullable);
            }

            return null;
        }
    }
}

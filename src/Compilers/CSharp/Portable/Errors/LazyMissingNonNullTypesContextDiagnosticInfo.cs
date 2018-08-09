// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyMissingNonNullTypesContextDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly INonNullTypesContext _context;
        private readonly TypeSymbolWithAnnotations _type;

        internal LazyMissingNonNullTypesContextDiagnosticInfo(INonNullTypesContext context, TypeSymbolWithAnnotations type)
        {
            _context = context;
            _type = type;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            if (!_type.IsValueType && _context.NonNullTypes != true)
            {
                return new CSDiagnosticInfo(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation);
            }
            return null;
        }
    }
}

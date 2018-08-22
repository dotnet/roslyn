// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A lazily calculated diagnostic for missing [NonNullTypes(true)].
    /// </summary>
    internal sealed class LazyMissingNonNullTypesContextDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly CSharpCompilation _compilation;
        private readonly INonNullTypesContext _context;
        private readonly TypeSymbolWithAnnotations _type;

        internal LazyMissingNonNullTypesContextDiagnosticInfo(CSharpCompilation compilation, INonNullTypesContext context, TypeSymbolWithAnnotations type)
        {
            _compilation = compilation;
            _context = context;
            _type = type;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            return _type.IsValueType ? null : Symbol.ReportNullableReferenceTypesIfNeeded(_compilation, _context);
        }
    }
}

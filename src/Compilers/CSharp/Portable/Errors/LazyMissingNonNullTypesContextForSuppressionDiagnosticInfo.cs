// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A lazily calculated diagnostic for using a suppression (!) outside of a [NonNullTypes(true/false)] context.
    /// </summary>
    internal sealed class LazyMissingNonNullTypesContextForSuppressionDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly INonNullTypesContext _context;

        internal LazyMissingNonNullTypesContextForSuppressionDiagnosticInfo(INonNullTypesContext context)
        {
            _context = context;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            return _context.NonNullTypes == null ? new CSDiagnosticInfo(ErrorCode.WRN_MissingNonNullTypesContext) : null;
        }
    }
}

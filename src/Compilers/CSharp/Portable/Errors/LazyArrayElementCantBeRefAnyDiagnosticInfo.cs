// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyArrayElementCantBeRefAnyDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly TypeWithAnnotations _possiblyRestrictedTypeSymbol;

        internal LazyArrayElementCantBeRefAnyDiagnosticInfo(TypeWithAnnotations possiblyRestrictedTypeSymbol)
        {
            _possiblyRestrictedTypeSymbol = possiblyRestrictedTypeSymbol;
        }

        private LazyArrayElementCantBeRefAnyDiagnosticInfo(LazyArrayElementCantBeRefAnyDiagnosticInfo original, DiagnosticSeverity severity) : base(original, severity)
        {
            _possiblyRestrictedTypeSymbol = original._possiblyRestrictedTypeSymbol;
        }

        protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
        {
            return new LazyArrayElementCantBeRefAnyDiagnosticInfo(this, severity);
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            if (_possiblyRestrictedTypeSymbol.IsRestrictedType())
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_ArrayElementCantBeRefAny, _possiblyRestrictedTypeSymbol.Type);
            }

            return null;
        }
    }
}

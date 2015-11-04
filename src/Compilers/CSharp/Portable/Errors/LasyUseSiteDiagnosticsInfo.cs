// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LasyUseSiteDiagnosticsInfo : DiagnosticInfo
    {
        private DiagnosticInfo _lazyActualObsoleteDiagnostic;

        private readonly TypeSymbolWithAnnotations _possiblyNullableTypeSymbol;

        internal LasyUseSiteDiagnosticsInfo(TypeSymbolWithAnnotations possiblyNullableTypeSymbol)
            : base(CSharp.MessageProvider.Instance, (int)ErrorCode.Unknown)
        {
            _possiblyNullableTypeSymbol = possiblyNullableTypeSymbol;
        }

        internal override DiagnosticInfo GetResolvedInfo()
        {
            if (_lazyActualObsoleteDiagnostic == null)
            {
                if (_possiblyNullableTypeSymbol.IsNullableType())
                {
                    var info = _possiblyNullableTypeSymbol.TypeSymbol.OriginalDefinition.GetUseSiteDiagnostic();

                    if (info != null)
                    {
                        Interlocked.CompareExchange(ref _lazyActualObsoleteDiagnostic, info, null);
                        return _lazyActualObsoleteDiagnostic;
                    }
                }

                // Make this a Void diagnostic.
                Interlocked.CompareExchange(ref _lazyActualObsoleteDiagnostic, CSDiagnosticInfo.VoidDiagnosticInfo, null);
            }

            return _lazyActualObsoleteDiagnostic;
        }
    }
}

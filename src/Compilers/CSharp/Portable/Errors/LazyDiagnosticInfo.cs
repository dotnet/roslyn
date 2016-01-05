// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class LasyDiagnosticsInfo : DiagnosticInfo
    {
        private DiagnosticInfo _lazyInfo;

        private readonly TypeSymbolWithAnnotations _possiblyNullableTypeSymbol;

        protected LasyDiagnosticsInfo()
            : base(CSharp.MessageProvider.Instance, (int)ErrorCode.Unknown)
        {
        }

        internal sealed override DiagnosticInfo GetResolvedInfo()
        {
            if (_lazyInfo == null)
            {
                Interlocked.CompareExchange(ref _lazyInfo, ResolveInfo() ?? CSDiagnosticInfo.VoidDiagnosticInfo, null);
            }

            return _lazyInfo;
        }

        protected abstract DiagnosticInfo ResolveInfo();
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyObsoleteDiagnosticInfo : DiagnosticInfo
    {
        private DiagnosticInfo lazyActualObsoleteDiagnostic;

        private readonly Symbol symbol;
        private readonly Symbol containingSymbol;
        private readonly BinderFlags binderFlags;

        internal LazyObsoleteDiagnosticInfo(Symbol symbol, Symbol containingSymbol, BinderFlags binderFlags)
            : base(CSharp.MessageProvider.Instance, (int)ErrorCode.Unknown)
        {
            this.symbol = symbol;
            this.containingSymbol = containingSymbol;
            this.binderFlags = binderFlags;
            this.lazyActualObsoleteDiagnostic = null;
        }

        internal override DiagnosticInfo GetResolvedInfo()
        {
            if (lazyActualObsoleteDiagnostic == null)
            {
                // A symbol's Obsoleteness may not have been calculated yet if the symbol is coming
                // from a different compilation's source. In that case, force completion of attributes.
                symbol.ForceCompleteObsoleteAttribute();

                if (symbol.ObsoleteState == ThreeState.True)
                {
                    var inObsoleteContext = ObsoleteAttributeHelpers.GetObsoleteContextState(containingSymbol, forceComplete: true);
                    Debug.Assert(inObsoleteContext != ThreeState.Unknown);

                    if (inObsoleteContext == ThreeState.False)
                    {
                        DiagnosticInfo info = ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(symbol, binderFlags);
                        if (info != null)
                        {
                            Interlocked.CompareExchange(ref this.lazyActualObsoleteDiagnostic, info, null);
                            return lazyActualObsoleteDiagnostic;
                        }
                    }
                }

                // If this symbol is not obsolete or is in an obsolete context, we don't want to report any diagnostics.
                // Therefore make this a Void diagnostic.
                Interlocked.CompareExchange(ref this.lazyActualObsoleteDiagnostic, CSDiagnosticInfo.VoidDiagnosticInfo, null);
            }

            return lazyActualObsoleteDiagnostic;
        }
    }
}

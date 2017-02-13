// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyObsoleteDiagnosticInfo : DiagnosticInfo
    {
        private DiagnosticInfo _lazyActualObsoleteDiagnostic;

        private readonly Symbol _symbol;
        private readonly Symbol _containingSymbol;
        private readonly BinderFlags _binderFlags;

        internal LazyObsoleteDiagnosticInfo(Symbol symbol, Symbol containingSymbol, BinderFlags binderFlags)
            : base(CSharp.MessageProvider.Instance, (int)ErrorCode.Unknown)
        {
            _symbol = symbol;
            _containingSymbol = containingSymbol;
            _binderFlags = binderFlags;
            _lazyActualObsoleteDiagnostic = null;
        }

        internal override DiagnosticInfo GetResolvedInfo()
        {
            if (_lazyActualObsoleteDiagnostic == null)
            {
                // A symbol's Obsoleteness may not have been calculated yet if the symbol is coming
                // from a different compilation's source. In that case, force completion of attributes.
                _symbol.ForceCompleteObsoleteAttribute();

                if (_symbol.ObsoleteState == ThreeState.True)
                {
                    var inObsoleteContext = ObsoleteAttributeHelpers.GetObsoleteContextState(_containingSymbol, forceComplete: true);
                    Debug.Assert(inObsoleteContext != ThreeState.Unknown);

                    if (inObsoleteContext == ThreeState.False)
                    {
                        DiagnosticInfo info = ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(_symbol, _binderFlags);
                        if (info != null)
                        {
                            Interlocked.CompareExchange(ref _lazyActualObsoleteDiagnostic, info, null);
                            return _lazyActualObsoleteDiagnostic;
                        }
                    }
                }

                // If this symbol is not obsolete or is in an obsolete context, we don't want to report any diagnostics.
                // Therefore make this a Void diagnostic.
                Interlocked.CompareExchange(ref _lazyActualObsoleteDiagnostic, CSDiagnosticInfo.VoidDiagnosticInfo, null);
            }

            return _lazyActualObsoleteDiagnostic;
        }

        public override bool Equals(object obj)
        {
            if (obj is LazyObsoleteDiagnosticInfo lod)
            {
                return this.GetResolvedInfo().Equals(lod.GetResolvedInfo()) &&
                       this._symbol == lod._symbol &&
                       this._containingSymbol == lod._containingSymbol &&
                       this._binderFlags == lod._binderFlags &&
                       base.Equals(obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                this._lazyActualObsoleteDiagnostic,
                Hash.Combine(this._symbol,
                Hash.Combine(this._containingSymbol,
                Hash.Combine(_binderFlags.GetHashCode(),
                base.GetHashCode()))));
        }
    }
}

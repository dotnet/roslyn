// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyObsoleteDiagnosticInfo : LazyDiagnosticsInfo
    {
        private readonly object _symbolOrSymbolWithAnnotations;
        private readonly Symbol _containingSymbol;
        private readonly BinderFlags _binderFlags;

        internal LazyObsoleteDiagnosticInfo(Symbol symbol, Symbol containingSymbol, BinderFlags binderFlags)
        {
            _symbolOrSymbolWithAnnotations = symbol;
            _containingSymbol = containingSymbol;
            _binderFlags = binderFlags;
        }

        internal LazyObsoleteDiagnosticInfo(SymbolWithAnnotations symbol, Symbol containingSymbol, BinderFlags binderFlags)
        {
            _symbolOrSymbolWithAnnotations = symbol;
            _containingSymbol = containingSymbol;
            _binderFlags = binderFlags;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            // A symbol's Obsoleteness may not have been calculated yet if the symbol is coming
            // from a different compilation's source. In that case, force completion of attributes.
            var symbol = (_symbolOrSymbolWithAnnotations as Symbol) ?? ((SymbolWithAnnotations)_symbolOrSymbolWithAnnotations).Symbol;
            symbol.ForceCompleteObsoleteAttribute();

            if (symbol.ObsoleteState == ThreeState.True)
            {
                var inObsoleteContext = ObsoleteAttributeHelpers.GetObsoleteContextState(_containingSymbol, forceComplete: true);
                Debug.Assert(inObsoleteContext != ThreeState.Unknown);

                var kind = ObsoleteAttributeHelpers.GetObsoleteDiagnosticKind(_symbol, _containingSymbol, forceComplete: true);
                Debug.Assert(kind != ObsoleteDiagnosticKind.Lazy);
                Debug.Assert(kind != ObsoleteDiagnosticKind.LazyPotentiallySuppressed);

                var info = (kind == ObsoleteDiagnosticKind.Diagnostic) ?
                    ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(_symbol, _binderFlags) :
                    null;

                // If this symbol is not obsolete or is in an obsolete context, we don't want to report any diagnostics.
                // Therefore make this a Void diagnostic.
                Interlocked.CompareExchange(ref _lazyActualObsoleteDiagnostic, info ?? CSDiagnosticInfo.VoidDiagnosticInfo, null);
            }

            return null;
        }
    }
}

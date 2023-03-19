// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyObsoleteDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly object _symbolOrSymbolWithAnnotations;
        private readonly Symbol _containingSymbol;
        private readonly BinderFlags _binderFlags;

        internal LazyObsoleteDiagnosticInfo(object symbol, Symbol containingSymbol, BinderFlags binderFlags)
        {
            Debug.Assert(symbol is Symbol || symbol is TypeWithAnnotations);
            _symbolOrSymbolWithAnnotations = symbol;
            _containingSymbol = containingSymbol;
            _binderFlags = binderFlags;
        }

        private LazyObsoleteDiagnosticInfo(LazyObsoleteDiagnosticInfo original, DiagnosticSeverity severity) : base(original, severity)
        {
            _symbolOrSymbolWithAnnotations = original._symbolOrSymbolWithAnnotations;
            _containingSymbol = original._containingSymbol;
            _binderFlags = original._binderFlags;
        }

        protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
        {
            return new LazyObsoleteDiagnosticInfo(this, severity);
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            // A symbol's Obsoleteness may not have been calculated yet if the symbol is coming
            // from a different compilation's source. In that case, force completion of attributes.
            var symbol = (_symbolOrSymbolWithAnnotations as Symbol) ?? ((TypeWithAnnotations)_symbolOrSymbolWithAnnotations).Type;
            symbol.ForceCompleteObsoleteAttribute();

            var kind = ObsoleteAttributeHelpers.GetObsoleteDiagnosticKind(symbol, _containingSymbol, forceComplete: true);
            Debug.Assert(kind != ObsoleteDiagnosticKind.Lazy);
            Debug.Assert(kind != ObsoleteDiagnosticKind.LazyPotentiallySuppressed);

            // If this symbol is not obsolete or is in an obsolete context, we don't want to report any diagnostics.
            // Therefore return null.
            return (kind == ObsoleteDiagnosticKind.Diagnostic) ?
                ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(symbol, _binderFlags) :
                null;
        }
    }
}

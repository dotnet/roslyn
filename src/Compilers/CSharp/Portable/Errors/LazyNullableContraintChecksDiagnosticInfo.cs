// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A lazily calculated diagnostic for constraint check nullability.
    /// </summary>
    internal sealed class LazyNullableContraintChecksDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly NamedTypeSymbol _type;
        private readonly ConversionsBase _conversions;
        private readonly Compilation _compilation;

        internal LazyNullableContraintChecksDiagnosticInfo(NamedTypeSymbol type, ConversionsBase conversions, Compilation compilation)
        {
            _type = type;
            _conversions = conversions;
            _compilation = compilation;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var warningsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            // CheckTypeConstraints should only add nullability warnings to warningsBuilder.
            ConstraintsHelper.CheckTypeConstraints(
                _type,
                _conversions,
                _compilation,
                diagnosticsBuilder,
                warningsBuilder,
                ref useSiteDiagnosticsBuilder);
            // If there are multiple constraint check warnings, we'll report the first one only.
            var diagnostic = (warningsBuilder.Count == 0) ? null : warningsBuilder[0].DiagnosticInfo;
            useSiteDiagnosticsBuilder?.Free();
            warningsBuilder.Free();
            diagnosticsBuilder.Free();
            return diagnostic;
        }
    }
}

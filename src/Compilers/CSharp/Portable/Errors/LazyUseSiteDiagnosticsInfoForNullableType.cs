// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyUseSiteDiagnosticsInfoForNullableType : LazyDiagnosticInfo
    {
        private readonly LanguageVersion _languageVersion;
        private readonly TypeWithAnnotations _possiblyNullableTypeSymbol;

        internal LazyUseSiteDiagnosticsInfoForNullableType(LanguageVersion languageVersion, TypeWithAnnotations possiblyNullableTypeSymbol)
        {
            _languageVersion = languageVersion;
            _possiblyNullableTypeSymbol = possiblyNullableTypeSymbol;
        }

        private LazyUseSiteDiagnosticsInfoForNullableType(LazyUseSiteDiagnosticsInfoForNullableType original, DiagnosticSeverity severity) : base(original, severity)
        {
            _languageVersion = original._languageVersion;
            _possiblyNullableTypeSymbol = original._possiblyNullableTypeSymbol;
        }

        protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
        {
            return new LazyUseSiteDiagnosticsInfoForNullableType(this, severity);
        }

        protected override DiagnosticInfo? ResolveInfo()
        {
            if (_possiblyNullableTypeSymbol.IsNullableType())
            {
                return _possiblyNullableTypeSymbol.Type.OriginalDefinition.GetUseSiteInfo().DiagnosticInfo;
            }
            return Binder.GetNullableUnconstrainedTypeParameterDiagnosticIfNecessary(_languageVersion, _possiblyNullableTypeSymbol);
        }
    }
}

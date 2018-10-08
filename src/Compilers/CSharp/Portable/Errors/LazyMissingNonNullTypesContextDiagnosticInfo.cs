﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A lazily calculated diagnostic for missing [NonNullTypes(true)].
    /// </summary>
    internal sealed class LazyMissingNonNullTypesContextDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly CSharpCompilation _compilation;
        private readonly INonNullTypesContext _context;
        private readonly TypeSymbolWithAnnotations _type;

        internal LazyMissingNonNullTypesContextDiagnosticInfo(CSharpCompilation compilation, INonNullTypesContext context, TypeSymbolWithAnnotations type)
        {
            _compilation = compilation;
            _context = context;
            _type = type;
        }

        protected override DiagnosticInfo ResolveInfo()
        {
            return ReportNullableReferenceTypesIfNeeded(_compilation, _context, _type);
        }

        /// <summary>
        /// A `?` annotation on a type that isn't a value type causes:
        /// - an error before C# 8.0
        /// - a warning outside of a NonNullTypes context
        /// </summary>
        public static DiagnosticInfo ReportNullableReferenceTypesIfNeeded(CSharpCompilation compilation, INonNullTypesContext context, TypeSymbolWithAnnotations type)
        {
            return !type.IsNull && (type.IsValueType || type.IsErrorType()) ? null : ReportNullableReferenceTypesIfNeeded(compilation, context);
        }

        private static DiagnosticInfo ReportNullableReferenceTypesIfNeeded(CSharpCompilation compilation, INonNullTypesContext nonNullTypesContext)
        {
            var featureID = MessageID.IDS_FeatureStaticNullChecking;
            if (!compilation.IsFeatureEnabled(featureID))
            {
                LanguageVersion availableVersion = compilation.LanguageVersion;
                LanguageVersion requiredVersion = featureID.RequiredVersion();

                return new CSDiagnosticInfo(availableVersion.GetErrorCode(), featureID.Localize(), new CSharpRequiredLanguageVersion(requiredVersion));
            }
            else if (nonNullTypesContext.NonNullTypes != true)
            {
                return new CSDiagnosticInfo(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation);
            }

            return null;
        }
    }
}


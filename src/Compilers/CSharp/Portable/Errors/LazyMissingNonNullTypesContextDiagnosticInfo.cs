// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A lazily calculated diagnostic for missing [NonNullTypes(true)].
    /// </summary>
    internal sealed class LazyMissingNonNullTypesContextDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly TypeSymbolWithAnnotations _type;
        private readonly DiagnosticInfo _info;

        private LazyMissingNonNullTypesContextDiagnosticInfo(TypeSymbolWithAnnotations type, DiagnosticInfo info)
        {
            Debug.Assert(!type.IsNull);
            _type = type;
            _info = info;
        }

        public static void AddAll(bool isNullableEnabled, TypeSymbolWithAnnotations type, Location location, DiagnosticBag diagnostics)
        {
            var rawInfos = ArrayBuilder<DiagnosticInfo>.GetInstance();
            GetRawDiagnosticInfos(isNullableEnabled, (CSharpParseOptions)location.SourceTree.Options, rawInfos);
            foreach (var rawInfo in rawInfos)
            {
                diagnostics.Add(new LazyMissingNonNullTypesContextDiagnosticInfo(type, rawInfo), location);
            }
            rawInfos.Free();
        }

        private static void GetRawDiagnosticInfos(bool isNullableEnabled, CSharpParseOptions options, ArrayBuilder<DiagnosticInfo> infos)
        {
            const MessageID featureId = MessageID.IDS_FeatureNullableReferenceTypes;
            var info = featureId.GetFeatureAvailabilityDiagnosticInfo(options);
            if (!(info is null))
            {
                infos.Add(info);
            }

            if (!isNullableEnabled && info?.Severity != DiagnosticSeverity.Error)
            {
                infos.Add(new CSDiagnosticInfo(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation));
            }
        }

        private static bool IsNullableReference(TypeSymbolWithAnnotations type)
            => type.IsNull || !(type.IsValueType || type.IsErrorType());

        protected override DiagnosticInfo ResolveInfo() => IsNullableReference(_type) ? _info : null;

        /// <summary>
        /// A `?` annotation on a type that isn't a value type causes:
        /// - an error before C# 8.0
        /// - a warning outside of a NonNullTypes context
        /// </summary>
        public static void ReportNullableReferenceTypesIfNeeded(bool isNullableEnabled, TypeSymbolWithAnnotations type, Location location, DiagnosticBag diagnostics)
        {
            if (IsNullableReference(type))
            {
                ReportNullableReferenceTypesIfNeeded(isNullableEnabled, location, diagnostics);
            }
        }

        public static void ReportNullableReferenceTypesIfNeeded(bool isNullableEnabled, Location location, DiagnosticBag diagnostics)
        {
            var rawInfos = ArrayBuilder<DiagnosticInfo>.GetInstance();
            GetRawDiagnosticInfos(isNullableEnabled, (CSharpParseOptions)location.SourceTree.Options, rawInfos);
            foreach (var rawInfo in rawInfos)
            {
                diagnostics.Add(rawInfo, location);
            }
            rawInfos.Free();
        }
    }
}


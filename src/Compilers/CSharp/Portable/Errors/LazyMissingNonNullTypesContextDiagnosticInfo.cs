// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A lazily calculated diagnostic for use of nullable annotations outside of a '#nullable' annotations context.
    /// </summary>
    internal sealed class LazyMissingNonNullTypesContextDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly TypeWithAnnotations _type;
        private readonly DiagnosticInfo _info;

        private LazyMissingNonNullTypesContextDiagnosticInfo(TypeWithAnnotations type, DiagnosticInfo info)
        {
            Debug.Assert(type.HasType);
            _type = type;
            _info = info;
        }

        public static void AddAll(bool isNullableEnabled, bool isGeneratedCode, TypeWithAnnotations type, Location location, DiagnosticBag diagnostics)
        {
            var rawInfos = ArrayBuilder<DiagnosticInfo>.GetInstance();
            GetRawDiagnosticInfos(isNullableEnabled, isGeneratedCode, (CSharpSyntaxTree)location.SourceTree, rawInfos);
            foreach (var rawInfo in rawInfos)
            {
                diagnostics.Add(new LazyMissingNonNullTypesContextDiagnosticInfo(type, rawInfo), location);
            }
            rawInfos.Free();
        }

#nullable enable
        private static void GetRawDiagnosticInfos(bool isNullableEnabled, bool isGeneratedCode, CSharpSyntaxTree tree, ArrayBuilder<DiagnosticInfo> infos)
        {
            const MessageID featureId = MessageID.IDS_FeatureNullableReferenceTypes;
            var info = featureId.GetFeatureAvailabilityDiagnosticInfo(tree.Options);
            if (info is object)
            {
                infos.Add(info);
            }

            if (!isNullableEnabled && info?.Severity != DiagnosticSeverity.Error)
            {
                var code = isGeneratedCode
                    ? ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode
                    : ErrorCode.WRN_MissingNonNullTypesContextForAnnotation;
                infos.Add(new CSDiagnosticInfo(code));
            }
        }
#nullable restore

        private static bool IsNullableReference(TypeSymbol type)
            => type is null || !(type.IsValueType || type.IsErrorType());

        protected override DiagnosticInfo ResolveInfo() => IsNullableReference(_type.Type) ? _info : null;

        /// <summary>
        /// A `?` annotation on a type that isn't a value type causes:
        /// - an error before C# 8.0
        /// - a warning outside of a NonNullTypes context
        /// </summary>
        public static void ReportNullableReferenceTypesIfNeeded(
            bool isNullableEnabled,
            bool isGeneratedCode,
            TypeWithAnnotations type,
            Location location,
            DiagnosticBag diagnostics)
        {
            if (IsNullableReference(type.Type))
            {
                ReportNullableReferenceTypesIfNeeded(isNullableEnabled, isGeneratedCode, location, diagnostics);
            }
        }

        public static void ReportNullableReferenceTypesIfNeeded(
            bool isNullableEnabled,
            bool isGeneratedCode,
            Location location,
            DiagnosticBag diagnostics)
        {
            var rawInfos = ArrayBuilder<DiagnosticInfo>.GetInstance();
            GetRawDiagnosticInfos(isNullableEnabled, isGeneratedCode, (CSharpSyntaxTree)location.SourceTree, rawInfos);
            foreach (var rawInfo in rawInfos)
            {
                diagnostics.Add(rawInfo, location);
            }
            rawInfos.Free();
        }
    }
}


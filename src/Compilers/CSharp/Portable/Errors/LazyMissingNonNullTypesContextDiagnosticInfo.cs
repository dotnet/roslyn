// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
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

#nullable enable
        /// <summary>
        /// A `?` annotation on a type that isn't a value type causes:
        /// - an error before C# 8.0
        /// - a warning outside of a NonNullTypes context
        /// </summary>
        public static void AddAll(Binder binder, SyntaxToken questionToken, TypeWithAnnotations? type, DiagnosticBag diagnostics)
        {
            var location = questionToken.GetLocation();

            var rawInfos = ArrayBuilder<DiagnosticInfo>.GetInstance();
            GetRawDiagnosticInfos(binder, questionToken, rawInfos);
            foreach (var rawInfo in rawInfos)
            {
                var info = (type.HasValue) ? new LazyMissingNonNullTypesContextDiagnosticInfo(type.Value, rawInfo) : rawInfo;
                diagnostics.Add(info, location);
            }

            rawInfos.Free();
        }

        private static void GetRawDiagnosticInfos(Binder binder, SyntaxToken questionToken, ArrayBuilder<DiagnosticInfo> infos)
        {
            Debug.Assert(questionToken.SyntaxTree != null);
            var tree = (CSharpSyntaxTree)questionToken.SyntaxTree;

            const MessageID featureId = MessageID.IDS_FeatureNullableReferenceTypes;
            var info = featureId.GetFeatureAvailabilityDiagnosticInfo(tree.Options);
            if (info is object)
            {
                infos.Add(info);
            }

            if (info?.Severity != DiagnosticSeverity.Error && !binder.AreNullableAnnotationsEnabled(questionToken))
            {
                var code = tree.IsGeneratedCode(binder.Compilation.Options.SyntaxTreeOptionsProvider, CancellationToken.None)
                    ? ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode
                    : ErrorCode.WRN_MissingNonNullTypesContextForAnnotation;
                infos.Add(new CSDiagnosticInfo(code));
            }
        }
#nullable disable

        internal static bool IsNullableReference(TypeSymbol type)
            => type is null || !(type.IsValueType || type.IsErrorType());

        protected override DiagnosticInfo ResolveInfo() => IsNullableReference(_type.Type) ? _info : null;
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.Lightup
{
    internal readonly struct AnalysisResultWrapper
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.Diagnostics.AnalysisResult";
        internal static readonly Type? WrappedType = typeof(Diagnostic).GetTypeInfo().Assembly.GetType(WrappedTypeName);
        private static readonly Func<object, ImmutableArray<DiagnosticAnalyzer>> s_analyzers;
        private static readonly Func<object, ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>> s_syntaxDiagnostics;
        private static readonly Func<object, ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>> s_semanticDiagnostics;
        private static readonly Func<object, ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>> s_additionalFileDiagnostics;
        private static readonly Func<object, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> s_compilationDiagnostics;
        private static readonly Func<object, ImmutableArray<Diagnostic>> s_getAllDiagnostics;

        private readonly object _instance;

        static AnalysisResultWrapper()
        {
            s_analyzers = LightupHelpers.CreatePropertyAccessor<object, ImmutableArray<DiagnosticAnalyzer>>(WrappedType, nameof(Analyzers), ImmutableArray<DiagnosticAnalyzer>.Empty);
            s_syntaxDiagnostics = LightupHelpers.CreatePropertyAccessor<object, ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>>(WrappedType, nameof(SyntaxDiagnostics), ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>.Empty);
            s_semanticDiagnostics = LightupHelpers.CreatePropertyAccessor<object, ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>>(WrappedType, nameof(SemanticDiagnostics), ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>.Empty);
            s_additionalFileDiagnostics = LightupHelpers.CreatePropertyAccessor<object, ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>>(WrappedType, nameof(AdditionalFileDiagnostics), ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>.Empty);
            s_compilationDiagnostics = LightupHelpers.CreatePropertyAccessor<object, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>(WrappedType, nameof(CompilationDiagnostics), ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>.Empty);

            if (WrappedType is not null
                && WrappedType.GetMethod(nameof(GetAllDiagnostics), Type.EmptyTypes) is { } methodInfo)
            {
                var analysisResultParameter = Expression.Parameter(typeof(object), "analysisResult");
                Expression instance = Expression.Convert(analysisResultParameter, WrappedType);

                Expression<Func<object, ImmutableArray<Diagnostic>>> expression =
                    Expression.Lambda<Func<object, ImmutableArray<Diagnostic>>>(
                        Expression.Call(instance, methodInfo),
                        analysisResultParameter);
                s_getAllDiagnostics = expression.Compile();
            }
            else
            {
                s_getAllDiagnostics = analysisResult => throw new NotSupportedException();
            }
        }

        private AnalysisResultWrapper(object instance)
        {
            _instance = instance;
        }

        public ImmutableArray<DiagnosticAnalyzer> Analyzers => s_analyzers(_instance);

        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SyntaxDiagnostics => s_syntaxDiagnostics(_instance);

        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SemanticDiagnostics => s_semanticDiagnostics(_instance);

        public ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> AdditionalFileDiagnostics => s_additionalFileDiagnostics(_instance);

        public ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> CompilationDiagnostics => s_compilationDiagnostics(_instance);

        ////public ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> AnalyzerTelemetryInfo { get; }

        public static AnalysisResultWrapper FromInstance(object instance)
        {
            if (instance == null)
            {
                return default;
            }

            if (!IsInstance(instance))
            {
                throw new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'");
            }

            return new AnalysisResultWrapper(instance);
        }

        public static bool IsInstance(object value)
        {
            if (value is null)
            {
                return false;
            }

            if (WrappedType is null)
            {
                return false;
            }

            return WrappedType.IsAssignableFrom(value.GetType());
        }

        public ImmutableArray<Diagnostic> GetAllDiagnostics()
            => s_getAllDiagnostics(_instance);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    // TODO: This should be updated to follow the flow of array creation expressions
    // that are eventually converted to and leave a given method as IEnumerable<T> once we have
    // the ability to do more thorough data-flow analysis in diagnostic analyzers.
    /// <summary>
    /// RS0001: <inheritdoc cref="UseSpecializedCollectionsEmptyEnumerableTitle"/>
    /// RS0002: <inheritdoc cref="UseSpecializedCollectionsSingletonEnumerableTitle"/>
    /// </summary>
    public abstract class SpecializedEnumerableCreationAnalyzer : DiagnosticAnalyzer
    {
        internal const string SpecializedCollectionsMetadataName = "Roslyn.Utilities.SpecializedCollections";
        internal const string LinqEnumerableMetadataName = "System.Linq.Enumerable";
        internal const string EmptyMethodName = "Empty";

        internal static readonly DiagnosticDescriptor UseEmptyEnumerableRule = new(
            RoslynDiagnosticIds.UseEmptyEnumerableRuleId,
            CreateLocalizableResourceString(nameof(UseSpecializedCollectionsEmptyEnumerableTitle)),
            CreateLocalizableResourceString(nameof(UseSpecializedCollectionsEmptyEnumerableMessage)),
            DiagnosticCategory.RoslynDiagnosticsPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor UseSingletonEnumerableRule = new(
            RoslynDiagnosticIds.UseSingletonEnumerableRuleId,
            CreateLocalizableResourceString(nameof(UseSpecializedCollectionsSingletonEnumerableTitle)),
            CreateLocalizableResourceString(nameof(UseSpecializedCollectionsSingletonEnumerableMessage)),
            DiagnosticCategory.RoslynDiagnosticsPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(UseEmptyEnumerableRule, UseSingletonEnumerableRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(context =>
            {
                INamedTypeSymbol? specializedCollectionsSymbol = context.Compilation.GetOrCreateTypeByMetadataName(SpecializedCollectionsMetadataName);
                if (specializedCollectionsSymbol == null)
                {
                    // TODO: In the future, we may want to run this analyzer even if the SpecializedCollections
                    // type cannot be found in this compilation. In some cases, we may want to add a reference
                    // to SpecializedCollections as a linked file or an assembly that contains it. With this
                    // check, we will not warn where SpecializedCollections is not yet referenced.
                    return;
                }

                INamedTypeSymbol? genericEnumerableSymbol = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIEnumerable1);
                if (genericEnumerableSymbol == null)
                {
                    return;
                }

                INamedTypeSymbol? linqEnumerableSymbol = context.Compilation.GetOrCreateTypeByMetadataName(LinqEnumerableMetadataName);
                if (linqEnumerableSymbol == null)
                {
                    return;
                }

                if (linqEnumerableSymbol.GetMembers(EmptyMethodName).FirstOrDefault() is not IMethodSymbol genericEmptyEnumerableSymbol ||
                    genericEmptyEnumerableSymbol.Arity != 1 ||
                    !genericEmptyEnumerableSymbol.Parameters.IsEmpty)
                {
                    return;
                }

                GetCodeBlockStartedAnalyzer(context, genericEnumerableSymbol, genericEmptyEnumerableSymbol);
            });
        }

        protected abstract void GetCodeBlockStartedAnalyzer(CompilationStartAnalysisContext context, INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol);

        protected abstract class AbstractCodeBlockStartedAnalyzer<TLanguageKindEnum> where TLanguageKindEnum : struct
        {
            private readonly INamedTypeSymbol _genericEnumerableSymbol;
            private readonly IMethodSymbol _genericEmptyEnumerableSymbol;

            protected AbstractCodeBlockStartedAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                _genericEnumerableSymbol = genericEnumerableSymbol;
                _genericEmptyEnumerableSymbol = genericEmptyEnumerableSymbol;
            }

            protected abstract void GetSyntaxAnalyzer(CodeBlockStartAnalysisContext<TLanguageKindEnum> context, INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol);

            public void Initialize(CodeBlockStartAnalysisContext<TLanguageKindEnum> context)
            {
                if (context.OwningSymbol is IMethodSymbol methodSymbol &&
    Equals(methodSymbol.ReturnType.OriginalDefinition, _genericEnumerableSymbol))
                {
                    GetSyntaxAnalyzer(context, _genericEnumerableSymbol, _genericEmptyEnumerableSymbol);
                }
            }
        }

        protected abstract class AbstractSyntaxAnalyzer
        {
            protected INamedTypeSymbol GenericEnumerableSymbol { get; }
            private readonly IMethodSymbol _genericEmptyEnumerableSymbol;

            protected AbstractSyntaxAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                this.GenericEnumerableSymbol = genericEnumerableSymbol;
                _genericEmptyEnumerableSymbol = genericEmptyEnumerableSymbol;
            }

            public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(UseEmptyEnumerableRule, UseSingletonEnumerableRule);

            protected bool ShouldAnalyzeArrayCreationExpression(SyntaxNode expression, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                TypeInfo typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

                return typeInfo.ConvertedType != null &&
                    Equals(typeInfo.ConvertedType.OriginalDefinition, GenericEnumerableSymbol) &&
                    typeInfo.Type is IArrayTypeSymbol arrayType &&
                    arrayType.Rank == 1;
            }

            protected void AnalyzeMemberAccessName(SyntaxNode name, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                if (semanticModel.GetSymbolInfo(name, cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                    Equals(methodSymbol.OriginalDefinition, _genericEmptyEnumerableSymbol))
                {
                    addDiagnostic(name.Parent.CreateDiagnostic(UseEmptyEnumerableRule));
                }
            }

            protected static void AnalyzeArrayLength(int length, SyntaxNode arrayCreationExpression, Action<Diagnostic> addDiagnostic)
            {
                if (length == 0)
                {
                    addDiagnostic(arrayCreationExpression.CreateDiagnostic(UseEmptyEnumerableRule));
                }
                else if (length == 1)
                {
                    addDiagnostic(arrayCreationExpression.CreateDiagnostic(UseSingletonEnumerableRule));
                }
            }
        }
    }
}

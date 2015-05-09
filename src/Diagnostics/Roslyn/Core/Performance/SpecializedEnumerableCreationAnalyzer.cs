﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    // TODO: This should be updated to follow the flow of array creation expressions
    // that are eventually converted to and leave a given method as IEnumerable<T> once we have
    // the ability to do more thorough data-flow analysis in diagnostic analyzers.
    public abstract class SpecializedEnumerableCreationAnalyzer : DiagnosticAnalyzer
    {
        internal const string SpecializedCollectionsMetadataName = "Roslyn.Utilities.SpecializedCollections";
        internal const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
        internal const string LinqEnumerableMetadataName = "System.Linq.Enumerable";
        internal const string EmptyMethodName = "Empty";

        private static readonly LocalizableString s_localizableTitleUseEmptyEnumerable = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseEmptyEnumerableDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageUseEmptyEnumerable = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseEmptyEnumerableMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        internal static readonly DiagnosticDescriptor UseEmptyEnumerableRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.UseEmptyEnumerableRuleId,
            s_localizableTitleUseEmptyEnumerable,
            s_localizableMessageUseEmptyEnumerable,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly LocalizableString s_localizableTitleUseSingletonEnumerable = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseSingletonEnumerableDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageUseSingletonEnumerable = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseSingletonEnumerableMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        internal static readonly DiagnosticDescriptor UseSingletonEnumerableRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.UseSingletonEnumerableRuleId,
            s_localizableTitleUseSingletonEnumerable,
            s_localizableMessageUseSingletonEnumerable,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(UseEmptyEnumerableRule, UseSingletonEnumerableRule); }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(
                (context) =>
                {
                    var specializedCollectionsSymbol = context.Compilation.GetTypeByMetadataName(SpecializedCollectionsMetadataName);
                    if (specializedCollectionsSymbol == null)
                    {
                        // TODO: In the future, we may want to run this analyzer even if the SpecializedCollections
                        // type cannot be found in this compilation. In some cases, we may want to add a reference
                        // to SpecializedCollections as a linked file or an assembly that contains it. With this
                        // check, we will not warn where SpecializedCollections is not yet referenced.
                        return;
                    }

                    var genericEnumerableSymbol = context.Compilation.GetTypeByMetadataName(IEnumerableMetadataName);
                    if (genericEnumerableSymbol == null)
                    {
                        return;
                    }

                    var linqEnumerableSymbol = context.Compilation.GetTypeByMetadataName(LinqEnumerableMetadataName);
                    if (linqEnumerableSymbol == null)
                    {
                        return;
                    }

                    var genericEmptyEnumerableSymbol = linqEnumerableSymbol.GetMembers(EmptyMethodName).FirstOrDefault() as IMethodSymbol;
                    if (genericEmptyEnumerableSymbol == null ||
                        genericEmptyEnumerableSymbol.Arity != 1 ||
                        genericEmptyEnumerableSymbol.Parameters.Length != 0)
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

            public AbstractCodeBlockStartedAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                _genericEnumerableSymbol = genericEnumerableSymbol;
                _genericEmptyEnumerableSymbol = genericEmptyEnumerableSymbol;
            }

            protected abstract void GetSyntaxAnalyzer(CodeBlockStartAnalysisContext<TLanguageKindEnum> context, INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol);

            public void Initialize(CodeBlockStartAnalysisContext<TLanguageKindEnum> context)
            {
                var methodSymbol = context.OwningSymbol as IMethodSymbol;
                if (methodSymbol != null &&
                    methodSymbol.ReturnType.OriginalDefinition == _genericEnumerableSymbol)
                {
                    GetSyntaxAnalyzer(context, _genericEnumerableSymbol, _genericEmptyEnumerableSymbol);
                }
            }
        }

        protected abstract class AbstractSyntaxAnalyzer
        {
            protected INamedTypeSymbol genericEnumerableSymbol;
            private readonly IMethodSymbol _genericEmptyEnumerableSymbol;

            public AbstractSyntaxAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                this.genericEnumerableSymbol = genericEnumerableSymbol;
                _genericEmptyEnumerableSymbol = genericEmptyEnumerableSymbol;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(UseEmptyEnumerableRule, UseSingletonEnumerableRule); }
            }

            protected bool ShouldAnalyzeArrayCreationExpression(SyntaxNode expression, SemanticModel semanticModel)
            {
                var typeInfo = semanticModel.GetTypeInfo(expression);
                var arrayType = typeInfo.Type as IArrayTypeSymbol;

                return typeInfo.ConvertedType != null &&
                    typeInfo.ConvertedType.OriginalDefinition == this.genericEnumerableSymbol &&
                    arrayType != null &&
                    arrayType.Rank == 1;
            }

            protected void AnalyzeMemberAccessName(SyntaxNode name, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic)
            {
                var methodSymbol = semanticModel.GetSymbolInfo(name).Symbol as IMethodSymbol;
                if (methodSymbol != null &&
                    methodSymbol.OriginalDefinition == _genericEmptyEnumerableSymbol)
                {
                    addDiagnostic(Diagnostic.Create(UseEmptyEnumerableRule, name.Parent.GetLocation()));
                }
            }

            protected static void AnalyzeArrayLength(int length, SyntaxNode arrayCreationExpression, Action<Diagnostic> addDiagnostic)
            {
                if (length == 0)
                {
                    addDiagnostic(Diagnostic.Create(UseEmptyEnumerableRule, arrayCreationExpression.GetLocation()));
                }
                else if (length == 1)
                {
                    addDiagnostic(Diagnostic.Create(UseSingletonEnumerableRule, arrayCreationExpression.GetLocation()));
                }
            }
        }
    }
}

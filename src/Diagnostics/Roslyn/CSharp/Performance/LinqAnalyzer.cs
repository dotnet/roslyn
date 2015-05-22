// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LinqAnalyzer : DiagnosticAnalyzer
    {
        private const string IReadOnlyListMetadataName = "System.Collections.Generic.IReadOnlyList`1";
        private const string IListMetadataName = "System.Collections.Generic.IList`1";
        private const string EnumerableMetadataName = "System.Linq.Enumerable";

        private static readonly LocalizableString s_localizableMessageAndTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DoNotUseLinqOnIndexableCollectionMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DoNotUseLinqOnIndexableCollectionDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        public static readonly DiagnosticDescriptor DoNotCallLastOnIndexableDescriptor = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DoNotCallLinqOnIndexable,
            s_localizableMessageAndTitle,
            s_localizableMessageAndTitle,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(DoNotCallLastOnIndexableDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var listType = context.Compilation.GetTypeByMetadataName(IListMetadataName);
            var readonlyListType = context.Compilation.GetTypeByMetadataName(IReadOnlyListMetadataName);
            var enumerableType = context.Compilation.GetTypeByMetadataName(EnumerableMetadataName);
            if (readonlyListType != null && enumerableType != null && listType != null)
            {
                context.RegisterSyntaxNodeAction(nodeContext => AnalyzeCall(nodeContext, enumerableType, readonlyListType, listType), SyntaxKind.InvocationExpression);
            }
        }

        /// <summary>
        /// The Enumerable.Last method will only special case indexable types that implement <see cref="IList{T}" />.  Types 
        /// which implement only <see cref="IReadOnlyList{T}"/> will be treated the same as IEnumerable{T} and go through a 
        /// full enumeration.  This method identifies such types.
        /// 
        /// At this point it only identifies <see cref="IReadOnlyList{T}"/> directly but could easily be extended to support
        /// any type which has an index and count property.  
        /// </summary>
        private bool IsTypeWithInefficientLinqMethods(SyntaxNodeAnalysisContext context, ExpressionSyntax targetSyntax, ITypeSymbol readonlyListType, ITypeSymbol listType)
        {
            var targetTypeInfo = context.SemanticModel.GetTypeInfo(targetSyntax);
            if (targetTypeInfo.Type == null)
            {
                return false;
            }

            var targetType = targetTypeInfo.Type;

            // If this type is simply IReadOnlyList<T> then no further checking is needed.  
            if (targetType.TypeKind == TypeKind.Interface && targetType.OriginalDefinition.Equals(readonlyListType))
            {
                return true;
            }

            bool implementsReadOnlyList = false;
            bool implementsList = false;
            foreach (var current in targetType.AllInterfaces)
            {
                if (current.OriginalDefinition.Equals(readonlyListType))
                {
                    implementsReadOnlyList = true;
                }

                if (current.OriginalDefinition.Equals(listType))
                {
                    implementsList = true;
                }
            }

            return implementsReadOnlyList && !implementsList;
        }

        /// <summary>
        /// This method attempts to normalize out the difference between extension and non-extension
        /// method invocations.  It will return the non-extension method form of the <see cref="IMethodSymbol" />
        /// and the <see cref="ExpressionSyntax" /> which corresponds to the first argument of that particular 
        /// form of the method
        /// </summary>
        private bool TryNormalizeMethodCall(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invokeSyntax, out IMethodSymbol methodSymbol, out ExpressionSyntax thisSyntax)
        {
            thisSyntax = null;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invokeSyntax, context.CancellationToken);
            methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            if (methodSymbol.ReducedFrom == null)
            {
                var arguments = invokeSyntax.ArgumentList.Arguments;
                if (arguments.Count == 0)
                {
                    return false;
                }

                thisSyntax = arguments[0].Expression;
                return thisSyntax != null;
            }
            else
            {
                var memberSyntax = invokeSyntax.Expression as MemberAccessExpressionSyntax;
                if (memberSyntax == null)
                {
                    return false;
                }

                methodSymbol = methodSymbol.ReducedFrom;
                thisSyntax = memberSyntax.Expression;
                return thisSyntax != null;
            }
        }

        /// <summary>
        /// Is this a method on <see cref="Enumerable" /> which takes only a single parameter?
        /// </summary>
        /// <remarks>
        /// Many of the methods we target, like Last, have overloads that take a filter delegate.  It is 
        /// completely appropriate to use such methods even with <see cref="IReadOnlyList{T}" />.  Only the single parameter
        /// ones are suspect
        /// </remarks>
        private bool IsSingleParameterLinqMethod(IMethodSymbol methodSymbol, ITypeSymbol enumerableType)
        {
            Debug.Assert(methodSymbol.ReducedFrom == null);
            return
                methodSymbol.ContainingSymbol.Equals(enumerableType) &&
                methodSymbol.Parameters.Length == 1;
        }

        /// <summary>
        /// Get the TypeInfo value for the argument passed to Enumerable.Last.  This method needs to account 
        /// for both extension method syntax and direct call syntax
        /// </summary>
        private bool TryGetTargetTypeInfo(SemanticModel semanticModel, InvocationExpressionSyntax invokeSyntax, MemberAccessExpressionSyntax memberSyntax, ITypeSymbol enumerableType, out TypeInfo typeInfo)
        {
            typeInfo = semanticModel.GetTypeInfo(memberSyntax.Expression);
            if (typeInfo.Type != null && !typeInfo.Type.Equals(enumerableType))
            {
                return true;
            }

            var arguments = invokeSyntax.ArgumentList.Arguments;
            if (arguments.Count > 1)
            {
                typeInfo = semanticModel.GetTypeInfo(arguments[0].Expression);
                return typeInfo.Type != null;
            }

            typeInfo = default(TypeInfo);
            return false;
        }

        private static bool IsPossibleLinqInvocation(InvocationExpressionSyntax invokeSyntax)
        {
            var memberSyntax = invokeSyntax.Expression as MemberAccessExpressionSyntax;
            if (memberSyntax == null || memberSyntax.Name == null)
            {
                return false;
            }

            switch (memberSyntax.Name.Identifier.ValueText)
            {
                case "Last":
                case "LastOrDefault":
                case "First":
                case "FirstOrDefault":
                case "Count":
                    return true;
                default:
                    return false;
            }
        }

        private void AnalyzeCall(SyntaxNodeAnalysisContext context, ITypeSymbol enumerableType, ITypeSymbol readonlyListType, ITypeSymbol listType)
        {
            var invokeSyntax = context.Node as InvocationExpressionSyntax;
            if (invokeSyntax == null || !IsPossibleLinqInvocation(invokeSyntax))
            {
                return;
            }

            ExpressionSyntax thisSyntax;
            IMethodSymbol methodSymbol;
            if (TryNormalizeMethodCall(context, invokeSyntax, out methodSymbol, out thisSyntax) &&
                IsSingleParameterLinqMethod(methodSymbol, enumerableType) &&
                IsTypeWithInefficientLinqMethods(context, thisSyntax, readonlyListType, listType))
            {
                context.ReportDiagnostic(Diagnostic.Create(DoNotCallLastOnIndexableDescriptor, invokeSyntax.GetLocation()));
            }
        }
    }
}

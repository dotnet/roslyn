// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class CompilationUtils
    {
        public static void CheckSymbols<TSymbol>(ImmutableArray<TSymbol> symbols, params string[] descriptions)
            where TSymbol : ISymbol
        {
            Assert.Equal(descriptions.Length, symbols.Length);

            string[] symbolDescriptions = (from s in symbols select s.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)).ToArray();
            Array.Sort(descriptions);
            Array.Sort(symbolDescriptions);

            for (int i = 0; i < descriptions.Length; i++)
            {
                Assert.Equal(symbolDescriptions[i], descriptions[i]);
            }
        }

        public static void CheckSymbolsUnordered<TSymbol>(ImmutableArray<TSymbol> symbols, params string[] descriptions)
            where TSymbol : ISymbol
        {
            Assert.Equal(descriptions.Length, symbols.Length);
            AssertEx.SetEqual(symbols.Select(s => s.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)), descriptions);
        }

        public static void CheckSymbols<TSymbol>(TSymbol[] symbols, params string[] descriptions)
            where TSymbol : ISymbol
        {
            CheckSymbols(symbols.AsImmutableOrNull(), descriptions);
        }

        public static void CheckSymbol(ISymbol symbol, string description)
        {
            Assert.Equal(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), description);
        }

        internal static void CheckConstraints(ITypeParameterSymbol symbol, TypeParameterConstraintKind constraints, params string[] constraintTypes)
        {
            Assert.Equal(constraints, GetTypeParameterConstraints(symbol));
            CheckSymbols(symbol.ConstraintTypes, constraintTypes);
        }

        internal static void CheckReducedExtensionMethod(
            MethodSymbol reducedMethod,
            string reducedMethodDescription,
            string reducedFromDescription,
            string constructedFromDescription,
            string reducedAndConstructedFromDescription)
        {
            var reducedFrom = reducedMethod.ReducedFrom;
            CheckReducedExtensionMethod(reducedMethod, reducedFrom);
            Assert.Equal(reducedMethod.CallsiteReducedFromMethod.Parameters[0].Type, reducedMethod.ReceiverType);

            var constructedFrom = reducedMethod.ConstructedFrom;
            CheckConstructedMethod(reducedMethod, constructedFrom);

            var reducedAndConstructedFrom = constructedFrom.ReducedFrom;
            CheckReducedExtensionMethod(constructedFrom, reducedAndConstructedFrom);
            Assert.Same(reducedFrom, reducedAndConstructedFrom);

            var constructedAndExtendedFrom = reducedFrom.ConstructedFrom;
            CheckConstructedMethod(reducedFrom, constructedAndExtendedFrom);

            CheckSymbol(reducedMethod, reducedMethodDescription);
            CheckSymbol(reducedFrom, reducedFromDescription);
            CheckSymbol(constructedFrom, constructedFromDescription);
            CheckSymbol(reducedAndConstructedFrom, reducedAndConstructedFromDescription);
            CheckSymbol(constructedAndExtendedFrom, reducedAndConstructedFromDescription);
        }

        public static void CheckReducedExtensionMethod(IMethodSymbol reducedMethod, IMethodSymbol reducedFrom)
        {
            Assert.NotNull(reducedFrom);
            Assert.Equal(reducedMethod.ReducedFrom, reducedFrom);
            Assert.Null(reducedFrom.ReducedFrom);

            Assert.True(reducedFrom.IsExtensionMethod);
            Assert.True(reducedMethod.IsExtensionMethod);
            Assert.Equal(reducedMethod.IsImplicitlyDeclared, reducedFrom.IsImplicitlyDeclared);
            Assert.Equal(reducedMethod.CanBeReferencedByName, reducedFrom.CanBeReferencedByName);

            int n = reducedMethod.Parameters.Count();
            Assert.Equal(reducedFrom.Parameters.Count(), n + 1);

            CheckTypeParameters(reducedMethod);
            CheckTypeParameters(reducedFrom);
        }

        public static void CheckConstructedMethod(IMethodSymbol constructedMethod, IMethodSymbol constructedFrom)
        {
            Assert.NotNull(constructedFrom);

            Assert.Same(constructedFrom, constructedMethod.ConstructedFrom);
            Assert.Same(constructedFrom, constructedMethod.OriginalDefinition);

            Assert.Same(constructedFrom, constructedFrom.ConstructedFrom);
            Assert.Same(constructedFrom, constructedFrom.OriginalDefinition);

            CheckTypeParameters(constructedMethod);
            CheckTypeParameters(constructedFrom);
        }

        private static void CheckTypeParameters(IMethodSymbol method)
        {
            var constructedFrom = method.ConstructedFrom;
            Assert.NotNull(constructedFrom);

            foreach (var typeParameter in method.TypeParameters)
            {
                Assert.Equal<ISymbol>(typeParameter.ContainingSymbol, constructedFrom);
            }
        }

        internal static TypeParameterConstraintKind GetTypeParameterConstraints(ITypeParameterSymbol typeParameter)
        {
            var constraints = TypeParameterConstraintKind.None;
            if (typeParameter.HasConstructorConstraint)
            {
                constraints |= TypeParameterConstraintKind.Constructor;
            }
            if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints |= TypeParameterConstraintKind.ReferenceType;
            }
            if (typeParameter.HasValueTypeConstraint)
            {
                constraints |= TypeParameterConstraintKind.ValueType;
            }
            return constraints;
        }

        public class SemanticInfoSummary
        {
            public ISymbol Symbol;
            public CandidateReason CandidateReason;
            public ImmutableArray<ISymbol> CandidateSymbols = ImmutableArray.Create<ISymbol>();
            public ITypeSymbol Type;
            public ITypeSymbol ConvertedType;
            public Conversion ImplicitConversion = default(Conversion);
            public IAliasSymbol Alias;
            public Optional<object> ConstantValue = default(Optional<object>);
            public bool IsCompileTimeConstant { get { return ConstantValue.HasValue; } }
            public ImmutableArray<ISymbol> MemberGroup = ImmutableArray.Create<ISymbol>();

            public ImmutableArray<IMethodSymbol> MethodGroup
            {
                get { return this.MemberGroup.WhereAsArray(s => s.Kind == SymbolKind.Method).SelectAsArray(s => (IMethodSymbol)s); }
            }
        }

        public static SemanticInfoSummary GetSemanticInfoSummary(this SemanticModel semanticModel, SyntaxNode node)
        {
            SemanticInfoSummary summary = new SemanticInfoSummary();

            // The information that is available varies by the type of the syntax node.

            SymbolInfo symbolInfo = SymbolInfo.None;
            if (node is ExpressionSyntax expr)
            {
                symbolInfo = semanticModel.GetSymbolInfo(expr);
                summary.ConstantValue = semanticModel.GetConstantValue(expr);
                var typeInfo = semanticModel.GetTypeInfo(expr);
                summary.Type = (TypeSymbol)typeInfo.Type;
                summary.ConvertedType = (TypeSymbol)typeInfo.ConvertedType;
                summary.ImplicitConversion = semanticModel.GetConversion(expr);
                summary.MemberGroup = semanticModel.GetMemberGroup(expr);
            }
            else if (node is AttributeSyntax attribute)
            {
                symbolInfo = semanticModel.GetSymbolInfo(attribute);
                var typeInfo = semanticModel.GetTypeInfo(attribute);
                summary.Type = (TypeSymbol)typeInfo.Type;
                summary.ConvertedType = (TypeSymbol)typeInfo.ConvertedType;
                summary.ImplicitConversion = semanticModel.GetConversion(attribute);
                summary.MemberGroup = semanticModel.GetMemberGroup(attribute);
            }
            else if (node is OrderingSyntax ordering)
            {
                symbolInfo = semanticModel.GetSymbolInfo(ordering);
            }
            else if (node is SelectOrGroupClauseSyntax selectOrGroupClause)
            {
                symbolInfo = semanticModel.GetSymbolInfo(selectOrGroupClause);
            }
            else if (node is ConstructorInitializerSyntax initializer)
            {
                symbolInfo = semanticModel.GetSymbolInfo(initializer);
                var typeInfo = semanticModel.GetTypeInfo(initializer);
                summary.Type = (TypeSymbol)typeInfo.Type;
                summary.ConvertedType = (TypeSymbol)typeInfo.ConvertedType;
                summary.ImplicitConversion = semanticModel.GetConversion(initializer);
                summary.MemberGroup = semanticModel.GetMemberGroup(initializer);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(node);
            }

            summary.Symbol = (Symbol)symbolInfo.Symbol;
            summary.CandidateReason = symbolInfo.CandidateReason;
            summary.CandidateSymbols = symbolInfo.CandidateSymbols;

            if (node is IdentifierNameSyntax identifier)
            {
                summary.Alias = semanticModel.GetAliasInfo(identifier);
            }

            return summary;
        }

        public static SemanticInfoSummary GetSpeculativeSemanticInfoSummary(this SemanticModel semanticModel, int position, SyntaxNode node, SpeculativeBindingOption bindingOption)
        {
            SemanticInfoSummary summary = new SemanticInfoSummary();

            // The information that is available varies by the type of the syntax node.

            SymbolInfo symbolInfo = new SymbolInfo();
            if (node is ExpressionSyntax)
            {
                ExpressionSyntax expr = (ExpressionSyntax)node;
                symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, expr, bindingOption);
                //summary.ConstantValue = semanticModel.GetSpeculativeConstantValue(expr);
                var typeInfo = semanticModel.GetSpeculativeTypeInfo(position, expr, bindingOption);
                summary.Type = (TypeSymbol)typeInfo.Type;
                summary.ConvertedType = (TypeSymbol)typeInfo.ConvertedType;
                summary.ImplicitConversion = semanticModel.GetSpeculativeConversion(position, expr, bindingOption);
                //summary.MethodGroup = semanticModel.GetSpeculativeMethodGroup(expr);
            }
            else if (node is ConstructorInitializerSyntax)
            {
                var initializer = (ConstructorInitializerSyntax)node;
                symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, initializer);
            }
            else
            {
                throw new NotSupportedException("Type of syntax node is not supported by GetSemanticInfoSummary");
            }

            summary.Symbol = (Symbol)symbolInfo.Symbol;
            summary.CandidateReason = symbolInfo.CandidateReason;
            summary.CandidateSymbols = symbolInfo.CandidateSymbols;

            if (node is IdentifierNameSyntax)
            {
                summary.Alias = semanticModel.GetSpeculativeAliasInfo(position, (IdentifierNameSyntax)node, bindingOption);
            }

            return summary;
        }

        public static List<string> LookupNames(this SemanticModel model, int position, INamespaceOrTypeSymbol container = null, bool namespacesAndTypesOnly = false, bool useBaseReferenceAccessibility = false)
        {
            Assert.True(!useBaseReferenceAccessibility || (object)container == null);
            Assert.True(!useBaseReferenceAccessibility || !namespacesAndTypesOnly);
            var symbols = useBaseReferenceAccessibility
                ? model.LookupBaseMembers(position)
                : namespacesAndTypesOnly
                    ? model.LookupNamespacesAndTypes(position, container)
                    : model.LookupSymbols(position, container);
            return symbols.Select(s => s.Name).Distinct().ToList();
        }

        /// <summary>
        /// Verify the type and nullability inferred by NullabilityWalker of all expressions in the source
        /// that are followed by specific annotations. Annotations are of the form /*T:type*/.
        /// </summary>
        internal static void VerifyTypes(this CSharpCompilation compilation, SyntaxTree tree = null)
        {
            Assert.True(compilation.NullableSemanticAnalysisEnabled);

            if (tree == null)
            {
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    VerifyTypes(compilation, syntaxTree);
                }

                return;
            }

            var root = tree.GetRoot();
            var allAnnotations = getAnnotations();
            if (allAnnotations.IsEmpty)
            {
                return;
            }

            var model = compilation.GetSemanticModel(tree);
            var annotationsByMethod = allAnnotations.GroupBy(annotation => annotation.Expression.Ancestors().OfType<BaseMethodDeclarationSyntax>().First()).ToArray();
            foreach (var annotations in annotationsByMethod)
            {
                var methodSyntax = annotations.Key;
                var method = model.GetDeclaredSymbol(methodSyntax);

                var expectedTypes = annotations.SelectAsArray(annotation => annotation.Text);
                var actualTypes = annotations.SelectAsArray(annotation =>
                    {
                        var typeInfo = model.GetTypeInfo(annotation.Expression);
                        Assert.NotEqual(CodeAnalysis.NullableFlowState.None, typeInfo.Nullability.FlowState);
                        // https://github.com/dotnet/roslyn/issues/35035: After refactoring symboldisplay, we should be able to just call something like typeInfo.Type.ToDisplayString(typeInfo.Nullability.FlowState, TypeWithState.TestDisplayFormat)
                        var type = TypeWithState.Create(
                            (TypeSymbol)(annotation.IsConverted ? typeInfo.ConvertedType : typeInfo.Type),
                            (annotation.IsConverted ? typeInfo.ConvertedNullability : typeInfo.Nullability).FlowState.ToInternalFlowState()).ToTypeWithAnnotations();
                        return type.ToDisplayString(TypeWithAnnotations.TestDisplayFormat);
                    });
                // Consider reporting the correct source with annotations on mismatch.
                AssertEx.Equal(expectedTypes, actualTypes, message: method.ToTestDisplayString());
            }

            ImmutableArray<(ExpressionSyntax Expression, string Text, bool IsConverted)> getAnnotations()
            {
                var builder = ArrayBuilder<(ExpressionSyntax, string, bool)>.GetInstance();
                foreach (var token in root.DescendantTokens())
                {
                    foreach (var trivia in token.TrailingTrivia)
                    {
                        if (trivia.Kind() == SyntaxKind.MultiLineCommentTrivia)
                        {
                            var text = trivia.ToFullString();
                            const string typePrefix = "/*T:";
                            const string convertedPrefix = "/*CT:";
                            const string suffix = "*/";
                            bool startsWithTypePrefix = text.StartsWith(typePrefix);
                            if (text.EndsWith(suffix) && (startsWithTypePrefix || text.StartsWith(convertedPrefix)))
                            {
                                var prefix = startsWithTypePrefix ? typePrefix : convertedPrefix;
                                var expr = getEnclosingExpression(token);
                                Assert.True(expr != null, $"VerifyTypes could not find a matching expression for annotation '{text}'.");

                                var content = text.Substring(prefix.Length, text.Length - prefix.Length - suffix.Length);
                                builder.Add((expr, content, !startsWithTypePrefix));
                            }
                        }
                    }
                }
                return builder.ToImmutableAndFree();
            }

            ExpressionSyntax getEnclosingExpression(SyntaxToken token)
            {
                var node = token.Parent;
                while (true)
                {
                    var expr = asExpression(node);
                    if (expr != null)
                    {
                        return expr;
                    }
                    if (node == root)
                    {
                        break;
                    }
                    node = node.Parent;
                }
                return null;
            }

            ExpressionSyntax asExpression(SyntaxNode node)
            {
                while (true)
                {
                    switch (node)
                    {
                        case null:
                            return null;
                        case ParenthesizedExpressionSyntax paren:
                            return paren.Expression;
                        case IdentifierNameSyntax id when id.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node:
                            node = memberAccess;
                            continue;
                        case ExpressionSyntax expr when expr.Parent is ConditionalAccessExpressionSyntax cond && cond.WhenNotNull == node:
                            node = cond;
                            continue;
                        case ExpressionSyntax expr:
                            return expr;
                        case { Parent: var parent }:
                            node = parent;
                            continue;
                    }
                }
            }
        }
    }
}

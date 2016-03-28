// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

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
            Assert.Equal(reducedMethod.CallsiteReducedFromMethod.Parameters[0].Type.TypeSymbol, reducedMethod.ReceiverType);

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
            if (node is ExpressionSyntax)
            {
                ExpressionSyntax expr = (ExpressionSyntax)node;
                symbolInfo = semanticModel.GetSymbolInfo(expr);
                summary.ConstantValue = semanticModel.GetConstantValue(expr);
                var typeInfo = semanticModel.GetTypeInfo(expr);
                summary.Type = (TypeSymbol)typeInfo.Type;
                summary.ConvertedType = (TypeSymbol)typeInfo.ConvertedType;
                summary.ImplicitConversion = semanticModel.GetConversion(expr);
                summary.MemberGroup = semanticModel.GetMemberGroup(expr);
            }
            else if (node is AttributeSyntax)
            {
                var attribute = (AttributeSyntax)node;
                symbolInfo = semanticModel.GetSymbolInfo(attribute);
                var typeInfo = semanticModel.GetTypeInfo(attribute);
                summary.Type = (TypeSymbol)typeInfo.Type;
                summary.ConvertedType = (TypeSymbol)typeInfo.ConvertedType;
                summary.ImplicitConversion = semanticModel.GetConversion(attribute);
                summary.MemberGroup = semanticModel.GetMemberGroup(attribute);
            }
            else if (node is OrderingSyntax)
            {
                symbolInfo = semanticModel.GetSymbolInfo((OrderingSyntax)node);
            }
            else if (node is SelectOrGroupClauseSyntax)
            {
                symbolInfo = semanticModel.GetSymbolInfo((SelectOrGroupClauseSyntax)node);
            }
            else if (node is ConstructorInitializerSyntax)
            {
                var initializer = (ConstructorInitializerSyntax)node;
                symbolInfo = semanticModel.GetSymbolInfo(initializer);
                var typeInfo = semanticModel.GetTypeInfo(initializer);
                summary.Type = (TypeSymbol)typeInfo.Type;
                summary.ConvertedType = (TypeSymbol)typeInfo.ConvertedType;
                summary.ImplicitConversion = semanticModel.GetConversion(initializer);
                summary.MemberGroup = semanticModel.GetMemberGroup(initializer);
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
                summary.Alias = semanticModel.GetAliasInfo((IdentifierNameSyntax)node);
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

        internal static ImmutableArray<SynthesizedAttributeData> GetSynthesizedAttributes(this ISymbol symbol, bool forReturnType = false)
        {
            var context = new ModuleCompilationState();

            ArrayBuilder<SynthesizedAttributeData> attributes = null;
            if (!forReturnType)
            {
                ((Symbol)symbol).AddSynthesizedAttributes(context, ref attributes);
            }
            else
            {
                Assert.True(symbol.Kind == SymbolKind.Method, "Incorrect usage of GetSynthesizedAttributes");
                ((MethodSymbol)symbol).AddSynthesizedReturnTypeAttributes(ref attributes);
            }

            return attributes != null ? attributes.ToImmutableAndFree() : ImmutableArray.Create<SynthesizedAttributeData>();
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
    }
}

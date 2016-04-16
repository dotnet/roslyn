// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A method that results from the translation of a single lambda expression.
    /// </summary>
    internal sealed class SynthesizedLambdaMethod : SynthesizedMethodBaseSymbol, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol _topLevelMethod;

        internal SynthesizedLambdaMethod(
            NamedTypeSymbol containingType,
            ClosureKind closureKind,
            MethodSymbol topLevelMethod,
            DebugId topLevelMethodId,
            BoundLambda lambdaNode,
            DebugId lambdaId)
            : base(containingType,
                   lambdaNode.Symbol,
                   null,
                   lambdaNode.SyntaxTree.GetReference(lambdaNode.Body.Syntax),
                   lambdaNode.Syntax.GetLocation(),
                   MakeName(topLevelMethod.Name, topLevelMethodId, closureKind, lambdaId),
                   (closureKind == ClosureKind.ThisOnly ? DeclarationModifiers.Private : DeclarationModifiers.Internal)
                       | (lambdaNode.Symbol.IsAsync ? DeclarationModifiers.Async : 0))
        {
            _topLevelMethod = topLevelMethod;

            TypeMap typeMap;
            ImmutableArray<TypeParameterSymbol> typeParameters;
            LambdaFrame lambdaFrame;

            if (!topLevelMethod.IsGenericMethod)
            {
                typeMap = TypeMap.Empty;
                typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else if ((object)(lambdaFrame = this.ContainingType as LambdaFrame) != null)
            {
                typeMap = lambdaFrame.TypeMap;
                typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                typeMap = TypeMap.Empty.WithAlphaRename(topLevelMethod, this, out typeParameters);
            }

            AssignTypeMapAndTypeParameters(typeMap, typeParameters);
        }

        private static string MakeName(string topLevelMethodName, DebugId topLevelMethodId, ClosureKind closureKind, DebugId lambdaId)
        {
            // Lambda method name must contain the declaring method ordinal to be unique unless the method is emitted into a closure class exclusive to the declaring method.
            // Lambdas that only close over "this" are emitted directly into the top-level method containing type.
            // Lambdas that don't close over anything (static) are emitted into a shared closure singleton.
            return GeneratedNames.MakeLambdaMethodName(
                topLevelMethodName,
                (closureKind == ClosureKind.General) ? -1 : topLevelMethodId.Ordinal,
                topLevelMethodId.Generation,
                lambdaId.Ordinal,
                lambdaId.Generation);
        }

        internal override int ParameterCount => this.BaseMethod.ParameterCount;

        // The lambda symbol might have declared no parameters in the case
        //
        // D d = delegate {};
        //
        // but there still might be parameters that need to be generated for the
        // synthetic method. If there are no lambda parameters, try the delegate 
        // parameters instead. 
        // 
        // UNDONE: In the native compiler in this scenario we make up new names for
        // UNDONE: synthetic parameters; in this implementation we use the parameter
        // UNDONE: names from the delegate. Does it really matter?
        protected override ImmutableArray<ParameterSymbol> BaseMethodParameters => this.BaseMethod.Parameters;

        internal override bool GenerateDebugInfo => !this.IsAsync;
        internal override bool IsExpressionBodied => false;
        internal MethodSymbol TopLevelMethod => _topLevelMethod;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            // Syntax offset of a syntax node contained in a lambda body is calculated by the containing top-level method.
            // The offset is thus relative to the top-level method body start.
            return _topLevelMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => _topLevelMethod;

        // The lambda method body needs to be updated when the containing top-level method body is updated.
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;
    }
}

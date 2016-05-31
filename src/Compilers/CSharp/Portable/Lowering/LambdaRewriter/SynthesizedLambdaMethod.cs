// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A method that results from the translation of a single lambda expression.
    /// </summary>
    internal sealed class SynthesizedLambdaMethod : SynthesizedMethodBaseSymbol, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol _topLevelMethod;
        private readonly ImmutableArray<TypeSymbol> _structClosures;

        internal SynthesizedLambdaMethod(
            NamedTypeSymbol containingType,
            ImmutableArray<TypeSymbol> structClosures,
            ClosureKind closureKind,
            MethodSymbol topLevelMethod,
            DebugId topLevelMethodId,
            IBoundLambdaOrFunction lambdaNode,
            DebugId lambdaId)
            : base(containingType,
                   lambdaNode.Symbol,
                   null,
                   lambdaNode.Syntax.SyntaxTree.GetReference(lambdaNode.Body.Syntax),
                   lambdaNode.Syntax.GetLocation(),
                   lambdaNode is BoundLocalFunctionStatement ?
                    MakeName(topLevelMethod.Name, lambdaNode.Symbol.Name, topLevelMethodId, closureKind, lambdaId) :
                    MakeName(topLevelMethod.Name, topLevelMethodId, closureKind, lambdaId),
                   (closureKind == ClosureKind.ThisOnly ? DeclarationModifiers.Private : DeclarationModifiers.Internal)
                       | (closureKind == ClosureKind.Static ? DeclarationModifiers.Static : 0)
                       | (lambdaNode.Symbol.IsAsync ? DeclarationModifiers.Async : 0))
        {
            _topLevelMethod = topLevelMethod;

            TypeMap typeMap;
            ImmutableArray<TypeParameterSymbol> typeParameters;
            ImmutableArray<TypeParameterSymbol> constructedFromTypeParameters;
            LambdaFrame lambdaFrame;

            lambdaFrame = this.ContainingType as LambdaFrame;
            switch (closureKind)
            {
                case ClosureKind.Singleton: // all type parameters on method (except the top level method's)
                case ClosureKind.General: // only lambda's type parameters on method (rest on class)
                    Debug.Assert(lambdaFrame != null);
                    typeMap = lambdaFrame.TypeMap.WithConcatAlphaRename(lambdaNode.Symbol, this, out typeParameters, out constructedFromTypeParameters, lambdaFrame.ContainingMethod);
                    break;
                case ClosureKind.ThisOnly: // all type parameters on method
                case ClosureKind.Static:
                    Debug.Assert(lambdaFrame == null);
                    typeMap = TypeMap.Empty.WithConcatAlphaRename(lambdaNode.Symbol, this, out typeParameters, out constructedFromTypeParameters, null);
                    break;
                default:
                    throw ExceptionUtilities.Unreachable;
            }

            if (!structClosures.IsDefaultOrEmpty && typeParameters.Length != 0)
            {
                var constructedStructClosures = ArrayBuilder<TypeSymbol>.GetInstance();
                foreach (var closure in structClosures)
                {
                    var frame = (LambdaFrame)closure;
                    NamedTypeSymbol constructed;
                    if (frame.Arity == 0)
                    {
                        constructed = frame;
                    }
                    else
                    {
                        var originals = frame.ConstructedFromTypeParameters;
                        var newArgs = typeMap.SubstituteTypeParameters(originals);
                        constructed = frame.Construct(newArgs);
                    }
                    constructedStructClosures.Add(constructed);
                }
                structClosures = constructedStructClosures.ToImmutableAndFree();
            }
            _structClosures = structClosures;

            AssignTypeMapAndTypeParameters(typeMap, typeParameters);
        }

        private static string MakeName(string topLevelMethodName, string localFunctionName, DebugId topLevelMethodId, ClosureKind closureKind, DebugId lambdaId)
        {
            return GeneratedNames.MakeLocalFunctionName(
                topLevelMethodName,
                localFunctionName,
                (closureKind == ClosureKind.General) ? -1 : topLevelMethodId.Ordinal,
                topLevelMethodId.Generation,
                lambdaId.Ordinal,
                lambdaId.Generation);
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

        protected override ImmutableArray<TypeSymbol> ExtraSynthesizedRefParameters => _structClosures;
        internal int ExtraSynthesizedParameterCount => this._structClosures.IsDefault ? 0 : this._structClosures.Length;

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

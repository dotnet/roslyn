﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A method that results from the translation of a single lambda expression.
    /// </summary>
    internal sealed class SynthesizedClosureMethod : SynthesizedMethodBaseSymbol, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly ImmutableArray<NamedTypeSymbol> _structEnvironments;

        internal MethodSymbol TopLevelMethod { get; }
        internal readonly DebugId LambdaId;

        internal SynthesizedClosureMethod(
            NamedTypeSymbol containingType,
            ImmutableArray<SynthesizedClosureEnvironment> structEnvironments,
            ClosureKind closureKind,
            MethodSymbol topLevelMethod,
            DebugId topLevelMethodId,
            MethodSymbol originalMethod,
            SyntaxReference blockSyntax,
            DebugId lambdaId)
            : base(containingType,
                   originalMethod,
                   blockSyntax,
                   originalMethod.DeclaringSyntaxReferences[0].GetLocation(),
                   originalMethod is LocalFunctionSymbol
                    ? MakeName(topLevelMethod.Name, originalMethod.Name, topLevelMethodId, closureKind, lambdaId)
                    : MakeName(topLevelMethod.Name, topLevelMethodId, closureKind, lambdaId),
                   MakeDeclarationModifiers(closureKind, originalMethod))
        {
            TopLevelMethod = topLevelMethod;
            ClosureKind = closureKind;
            LambdaId = lambdaId;

            TypeMap typeMap;
            ImmutableArray<TypeParameterSymbol> typeParameters;

            var lambdaFrame = ContainingType as SynthesizedClosureEnvironment;
            switch (closureKind)
            {
                case ClosureKind.Singleton: // all type parameters on method (except the top level method's)
                case ClosureKind.General: // only lambda's type parameters on method (rest on class)
                    RoslynDebug.Assert(!(lambdaFrame is null));
                    typeMap = lambdaFrame.TypeMap.WithConcatAlphaRename(
                        originalMethod,
                        this,
                        out typeParameters,
                        out _,
                        lambdaFrame.OriginalContainingMethodOpt);
                    break;
                case ClosureKind.ThisOnly: // all type parameters on method
                case ClosureKind.Static:
                    RoslynDebug.Assert(lambdaFrame is null);
                    typeMap = TypeMap.Empty.WithConcatAlphaRename(
                        originalMethod,
                        this,
                        out typeParameters,
                        out _,
                        stopAt: null);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(closureKind);
            }

            if (!structEnvironments.IsDefaultOrEmpty && typeParameters.Length != 0)
            {
                var constructedStructClosures = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                foreach (var env in structEnvironments)
                {
                    NamedTypeSymbol constructed;
                    if (env.Arity == 0)
                    {
                        constructed = env;
                    }
                    else
                    {
                        var originals = env.ConstructedFromTypeParameters;
                        var newArgs = typeMap.SubstituteTypeParameters(originals);
                        constructed = env.Construct(newArgs);
                    }
                    constructedStructClosures.Add(constructed);
                }
                _structEnvironments = constructedStructClosures.ToImmutableAndFree();
            }
            else
            {
                _structEnvironments = ImmutableArray<NamedTypeSymbol>.CastUp(structEnvironments);
            }

            AssignTypeMapAndTypeParameters(typeMap, typeParameters);

            // static local functions should be emitted as static.
            Debug.Assert(!(originalMethod is LocalFunctionSymbol) || !originalMethod.IsStatic || IsStatic);
        }

        private static DeclarationModifiers MakeDeclarationModifiers(ClosureKind closureKind, MethodSymbol originalMethod)
        {
            var mods = closureKind == ClosureKind.ThisOnly ? DeclarationModifiers.Private : DeclarationModifiers.Internal;

            if (closureKind == ClosureKind.Static)
            {
                mods |= DeclarationModifiers.Static;
            }

            if (originalMethod.IsAsync)
            {
                mods |= DeclarationModifiers.Async;
            }

            return mods;
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

        protected override ImmutableArray<TypeSymbol> ExtraSynthesizedRefParameters
            => ImmutableArray<TypeSymbol>.CastUp(_structEnvironments);
        internal int ExtraSynthesizedParameterCount => this._structEnvironments.IsDefault ? 0 : this._structEnvironments.Length;

        internal override bool GenerateDebugInfo => !this.IsAsync;
        internal override bool IsExpressionBodied => false;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            // Syntax offset of a syntax node contained in a lambda body is calculated by the containing top-level method.
            // The offset is thus relative to the top-level method body start.
            return TopLevelMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }

        public sealed override bool AreLocalsZeroed => TopLevelMethod.AreLocalsZeroed;

        IMethodSymbolInternal? ISynthesizedMethodBodyImplementationSymbol.Method => TopLevelMethod;

        // The lambda method body needs to be updated when the containing top-level method body is updated.
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;

        public ClosureKind ClosureKind { get; }
    }
}

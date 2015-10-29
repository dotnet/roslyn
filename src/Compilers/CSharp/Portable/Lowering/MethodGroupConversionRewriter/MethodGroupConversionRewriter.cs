// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodGroupConversionRewriter : BoundTreeRewriterWithStackGuard
    {
        private MethodSymbol CurrentMethod;
        private TypeCompilationState CompilationState;
        private DiagnosticBag Diagnostics;

        private MethodGroupConversionRewriter() { }

        public static BoundStatement Rewrite(
            MethodSymbol currentMethod,
            BoundStatement loweredBody,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(currentMethod != null);
            Debug.Assert(loweredBody?.HasErrors == false);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            if (IsCurrentMethodNotSuitableForRewrite(currentMethod))
            {
                return loweredBody;
            }

            var rewriter = new MethodGroupConversionRewriter
            {
                CurrentMethod = currentMethod,
                CompilationState = compilationState,
                Diagnostics = diagnostics,
            };

            var result = (BoundStatement)rewriter.Visit(loweredBody);
            return result;
        }

        internal static bool IsConversionRewritable(BoundConversion conversion, BoundExpression operand)
        {
            // We only target implicit method group conversion
            if (conversion.ExplicitCastInCode || conversion.ConversionKind != ConversionKind.MethodGroup)
            {
                return false;
            }

            // Static constructors are not suitable to cache
            if (IsCurrentMethodNotSuitableForRewrite(conversion.SymbolOpt))
            {
                return false;
            }

            // Make sure the operand is just an ordinary static method
            var targetMethod = GetTargetMethod(conversion);
            return (targetMethod != null
                && targetMethod.IsStatic
                && targetMethod.MethodKind == MethodKind.Ordinary);
        }

        private static bool IsCurrentMethodNotSuitableForRewrite(MethodSymbol currentMethod)
            => currentMethod?.MethodKind == MethodKind.StaticConstructor;

        private static MethodSymbol GetTargetMethod(BoundConversion conversion)
            => conversion.ExpressionSymbol as MethodSymbol;

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (IsConversionRewritable(node, node.Operand))
            {
                return RewriteConversion(node);
            }

            return base.VisitConversion(node);
        }

        private BoundNode RewriteConversion(BoundConversion conversion)
        {
            var targetMethod = GetTargetMethod(conversion);
            var currentModule = CompilationState.ModuleBuilderOpt;

            Debug.Assert(targetMethod != null);
            Debug.Assert(currentModule != null);

            var F = new SyntheticBoundNodeFactory(CurrentMethod, conversion.Syntax, CompilationState, Diagnostics);

            BoundFieldAccess boundBackingField = CreateBoundBackingField(F, targetMethod, conversion);

            var boundDelegateCreation = new BoundDelegateCreationExpression(
                conversion.Syntax,
                conversion.Operand,
                targetMethod,
                targetMethod.IsExtensionMethod,
                conversion.Type);

            return F.Coalesce(boundBackingField, F.AssignmentExpression(boundBackingField, boundDelegateCreation));
        }

        private BoundFieldAccess CreateBoundBackingField(SyntheticBoundNodeFactory F, MethodSymbol targetMethod, BoundConversion conversion)
        {
            var cacheFrameName = "<>S_" + targetMethod.ContainingType.Name;//GeneratedNames.Make...ClassName();

            MethodGroupConversionCacheFrame cacheFrameSymbol;
            NamedTypeSymbol constructedCacheFrameSymbol;

            ImmutableArray<TypeParameterSymbol> typeParameters;
            ImmutableArray<TypeSymbol> typeArguments;
            var frameContainer = CompilationState.Compilation.Assembly;
            if (TryGetGenericInfo(targetMethod, out typeParameters, out typeArguments))
            {
                var typeMap = new TypeMap(typeParameters, typeArguments);
                cacheFrameSymbol = new MethodGroupConversionCacheFrame(frameContainer, cacheFrameName, targetMethod, typeParameters, typeMap);

                constructedCacheFrameSymbol = cacheFrameSymbol.Construct(typeArguments);
            }
            else
            {
                cacheFrameSymbol = new MethodGroupConversionCacheFrame(frameContainer, cacheFrameName, targetMethod);
                constructedCacheFrameSymbol = cacheFrameSymbol;
            }

            var boundCacheFrame = F.Type(cacheFrameSymbol);

            var backingFieldName = "<>F_" + targetMethod.MetadataName;//GeneratedNames.Make...FieldName();
            var backingFieldType = conversion.Type;
            var backingFieldSymbol = new SynthesizedFieldSymbol(cacheFrameSymbol, backingFieldType, backingFieldName, isPublic: true, isStatic: true);

            return F.Field(null, backingFieldSymbol.AsMember(constructedCacheFrameSymbol));
        }

        private bool TryGetGenericInfo(
            MethodSymbol targetMethod,
            out ImmutableArray<TypeParameterSymbol> typeParameters,
            out ImmutableArray<TypeSymbol> typeArguments)
        {
            var typeParametersBuilder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var typeArgumentsBuilder = ArrayBuilder<TypeSymbol>.GetInstance();

            if (targetMethod.Arity > 0)
            {
                var constructed = (ConstructedMethodSymbol)targetMethod;
                typeParametersBuilder.AddRange(constructed.TypeParameters);
                typeArgumentsBuilder.AddRange(constructed.TypeArguments);
            }

            var containingType = targetMethod.ContainingType;
            while (containingType != null)
            {
                if (containingType.Arity > 0)
                {
                    typeParametersBuilder.AddRange(containingType.TypeParameters);
                    typeArgumentsBuilder.AddRange(containingType.TypeArguments);
                }

                containingType = containingType.ContainingType;
            }

            typeParameters = typeParametersBuilder.ToImmutableAndFree();
            typeArguments = typeArgumentsBuilder.ToImmutableAndFree();

            Debug.Assert(typeParameters.Length == typeArguments.Length);

            return typeArguments.Length > 0;
        }
    }
}

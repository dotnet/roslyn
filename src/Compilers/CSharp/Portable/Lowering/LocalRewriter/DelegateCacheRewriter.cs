// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal class DelegateCacheRewriter
{
    private readonly SyntheticBoundNodeFactory _factory;
    private readonly int _topLevelMethodOrdinal;

    public DelegateCacheRewriter(SyntheticBoundNodeFactory factory, int topLevelMethodOrdinal)
    {
        _factory = factory;
        _topLevelMethodOrdinal = topLevelMethodOrdinal;
    }

    public static bool CanRewrite(SyntheticBoundNodeFactory factory, bool inExpressionLambda, BoundConversion boundConversion, MethodSymbol targetMethod)
        => targetMethod.IsStatic && !boundConversion.IsExtensionMethod
        && !inExpressionLambda // The tree structure / meaning for expression trees should remain untouched.
        && factory.TopLevelMethod is not { MethodKind: MethodKind.StaticConstructor } // Avoid caching twice if people do it manually.
        && factory.Syntax.IsFeatureEnabled(MessageID.IDS_CacheStaticMethodGroupConversions)
        ;

    public BoundExpression Rewrite(int localFunctionOrdinal, SyntaxNode syntax, BoundExpression receiver, MethodSymbol targetMethod, NamedTypeSymbol delegateType)
    {
        Debug.Assert(delegateType.IsDelegateType());

        return new BoundDelegateCreationExpression(syntax, receiver, targetMethod, false, delegateType);
        //var orgSyntax = _factory.Syntax;
        //_factory.Syntax = syntax;

        //var cacheContainer = GetOrAddCacheContainer(delegateType, targetMethod);
        //var cacheField = cacheContainer.GetOrAddCacheField(_factory, delegateType, targetMethod);

        //var boundCacheField = _factory.Field(null, cacheField);
        //var boundDelegateCreation = new BoundDelegateCreationExpression(syntax, receiver, targetMethod, isExtensionMethod: false, type: delegateType)
        //{
        //    WasCompilerGenerated = true
        //};

        //var rewrittenNode = _factory.Coalesce(boundCacheField, _factory.AssignmentExpression(boundCacheField, boundDelegateCreation));

        //_factory.Syntax = orgSyntax;

        //return rewrittenNode;
    }

    private DelegateCacheContainer GetOrAddCacheContainer(NamedTypeSymbol delegateType, MethodSymbol targetMethod)
    {
        Debug.Assert(_factory is { TopLevelMethod: { }, ModuleBuilderOpt: { } });

        if (AConcreteContainerIsEnough(delegateType, targetMethod))
        {
            //return _factory.CompilationState.TypeScopedDelegateCacheContainer;
        }
        else
        {
            //return MethodScopedGenericDelegateCacheContainer;
        }

        throw new NotImplementedException();
    }

    private bool AConcreteContainerIsEnough(NamedTypeSymbol delegateType, MethodSymbol targetMethod)
    {
        // Possible places for type parameters that can act as type arguments to construct the delegateType or targetMethod:
        //   1. containing types
        //   2. current method
        //   3. local functions
        // Our containers are created within the same enclosing type, so we can ignore type parameters from it.

        Debug.Assert(_factory.TopLevelMethod is { });

        // So obviously,
        if (_factory is { CurrentFunction: null, TopLevelMethod.Arity: 0 })
        {
            return true;
        }

        var typeParams = PooledHashSet<TypeParameterSymbol>.GetInstance();
        try
        {
            typeParams.AddAll(_factory.TopLevelMethod.TypeParameters);

            for (Symbol? s = _factory.CurrentFunction; s is MethodSymbol m; s = s.ContainingSymbol)
            {
                typeParams.AddAll(m.TypeParameters);
            }

            if (typeParams.Count == 0)
            {
                return true;
            }

            if (delegateType.ContainsTypeParameters(typeParams) || containsTypeParameters(targetMethod, typeParams))
            {
                return false;
            }
        }
        finally
        {
            typeParams.Free();
        }

        return true;

        static bool containsTypeParameters(MethodSymbol method, PooledHashSet<TypeParameterSymbol> typeParams)
        {
            return method.ContainingType.ContainsTypeParameters(typeParams) ||
                method.TypeArgumentsWithAnnotations.Any(
                    static (typeArg, typeParams) =>
                        typeArg.Type.ContainsTypeParameters(typeParams) ||
                        typeArg.CustomModifiers.Any(static (cm, typeParams) => ((TypeSymbol)cm.Modifier).ContainsTypeParameters(typeParams), typeParams),
                    typeParams);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class DelegateCacheRewriter
{
    private readonly SyntheticBoundNodeFactory _factory;
    private readonly int _topLevelMethodOrdinal;

    private Stack<(int LocalFunctionOrdinal, DelegateCacheContainer CacheContainer)>? _genericCacheContainers;

    internal DelegateCacheRewriter(SyntheticBoundNodeFactory factory, int topLevelMethodOrdinal)
    {
        _factory = factory;
        _topLevelMethodOrdinal = topLevelMethodOrdinal;
    }

    internal static bool CanRewrite(SyntheticBoundNodeFactory factory, bool inExpressionLambda, BoundConversion boundConversion, MethodSymbol targetMethod)
        => targetMethod.IsStatic && !boundConversion.IsExtensionMethod
        && !inExpressionLambda // The tree structure / meaning for expression trees should remain untouched.
        && factory.TopLevelMethod is not { MethodKind: MethodKind.StaticConstructor } // Avoid caching twice if people do it manually.
        && factory.Syntax.IsFeatureEnabled(MessageID.IDS_CacheStaticMethodGroupConversions) // Compatibility reasons.
        ;

    internal BoundExpression Rewrite(int localFunctionOrdinal, SyntaxNode syntax, BoundExpression receiver, MethodSymbol targetMethod, NamedTypeSymbol delegateType)
    {
        Debug.Assert(delegateType.IsDelegateType());

        var oldSyntax = _factory.Syntax;
        _factory.Syntax = syntax;

        var cacheContainer = GetOrAddCacheContainer(localFunctionOrdinal, delegateType, targetMethod);
        var cacheField = cacheContainer.GetOrAddCacheField(_factory, delegateType, targetMethod);

        var boundCacheField = _factory.Field(null, cacheField);
        var boundDelegateCreation = new BoundDelegateCreationExpression(syntax, receiver, targetMethod, isExtensionMethod: false, type: delegateType)
        {
            WasCompilerGenerated = true
        };

        var rewrittenNode = _factory.Coalesce(boundCacheField, _factory.AssignmentExpression(boundCacheField, boundDelegateCreation));

        _factory.Syntax = oldSyntax;

        return rewrittenNode;
    }

    private DelegateCacheContainer GetOrAddCacheContainer(int localFunctionOrdinal, NamedTypeSymbol delegateType, MethodSymbol targetMethod)
    {
        Debug.Assert(_factory.TopLevelMethod is { });
        Debug.Assert(_factory.ModuleBuilderOpt is { });

        var typeCompilationState = _factory.CompilationState;
        var moduleBuilder = _factory.ModuleBuilderOpt;

        DelegateCacheContainer? container;

        if (AConcreteContainerIsEnough(delegateType, targetMethod))
        {
            container = typeCompilationState.ConcreteDelegateCacheContainer;

            if (container is { })
            {
                return container;
            }

            container = new(typeCompilationState.Type, moduleBuilder.CurrentGenerationOrdinal);
            typeCompilationState.ConcreteDelegateCacheContainer = container;
        }
        else
        {
            var containersStack = _genericCacheContainers ??= new();

            while (containersStack.Count > 0)
            {
                if (containersStack.Peek().LocalFunctionOrdinal > localFunctionOrdinal)
                {
                    containersStack.Pop();
                }
            }

            if (containersStack.Count > 0 && containersStack.Peek().LocalFunctionOrdinal == localFunctionOrdinal)
            {
                return containersStack.Peek().CacheContainer;
            }

            container = new(_factory.CurrentFunction ?? _factory.TopLevelMethod, _topLevelMethodOrdinal, localFunctionOrdinal, moduleBuilder.CurrentGenerationOrdinal);
            containersStack.Push((localFunctionOrdinal, container));
        }

        _factory.AddNestedType(container);

        return container;
    }

    private bool AConcreteContainerIsEnough(NamedTypeSymbol delegateType, MethodSymbol targetMethod)
    {
        // Possible places for type parameters that can act as type arguments to construct the delegateType or targetMethod:
        //   1. containing types
        //   2. current method
        //   3. local functions
        // Since our containers are created within the same enclosing type, we can ignore type parameters from it.

        Debug.Assert(_factory.TopLevelMethod is { });

        // So obviously,
        if (_factory is { CurrentFunction: null, TopLevelMethod.Arity: 0 })
        {
            return true;
        }

        var methodTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
        try
        {
            methodTypeParameters.AddAll(_factory.TopLevelMethod.TypeParameters);

            for (Symbol? s = _factory.CurrentFunction; s is MethodSymbol m; s = s.ContainingSymbol)
            {
                methodTypeParameters.AddAll(m.TypeParameters);
            }

            if (methodTypeParameters.Count == 0)
            {
                return true;
            }

            if (delegateType.ContainsTypeParameters(methodTypeParameters) || containsTypeParameters(targetMethod, methodTypeParameters))
            {
                return false;
            }
        }
        finally
        {
            methodTypeParameters.Free();
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

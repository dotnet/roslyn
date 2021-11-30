// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed partial class DelegateCacheRewriter
{
    private readonly SyntheticBoundNodeFactory _factory;
    private readonly MethodSymbol _topLevelMethod;
    private readonly int _topLevelMethodOrdinal;

    private Stack<(int LocalFunctionOrdinal, DelegateCacheContainer CacheContainer)>? _genericCacheContainers;

    internal DelegateCacheRewriter(SyntheticBoundNodeFactory factory, int topLevelMethodOrdinal)
    {
        Debug.Assert(factory.TopLevelMethod is { });

        _factory = factory;
        _topLevelMethod = factory.TopLevelMethod;
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
        var boundDelegateCreation = new BoundDelegateCreationExpression(syntax, receiver, targetMethod, isExtensionMethod: false, type: delegateType);

        var rewrittenNode = _factory.Coalesce(boundCacheField, _factory.AssignmentExpression(boundCacheField, boundDelegateCreation));

        _factory.Syntax = oldSyntax;

        return rewrittenNode;
    }

    private DelegateCacheContainer GetOrAddCacheContainer(int localFunctionOrdinal, NamedTypeSymbol delegateType, MethodSymbol targetMethod)
    {
        Debug.Assert(_factory.ModuleBuilderOpt is { });

        var typeCompilationState = _factory.CompilationState;
        var generation = _factory.ModuleBuilderOpt.CurrentGenerationOrdinal;

        DelegateCacheContainer? container;

        if (AConcreteContainerIsEnough(delegateType, targetMethod))
        {
            container = typeCompilationState.ConcreteDelegateCacheContainer;

            if (container is { })
            {
                return container;
            }

            container = new(typeCompilationState.Type, generation);
            typeCompilationState.ConcreteDelegateCacheContainer = container;
        }
        else
        {
            var containersStack = _genericCacheContainers ??= new();

            while (containersStack.Count > 0 && containersStack.Peek().LocalFunctionOrdinal > localFunctionOrdinal)
            {
                containersStack.Pop();
            }

            if (containersStack.Count > 0 && containersStack.Peek().LocalFunctionOrdinal == localFunctionOrdinal)
            {
                return containersStack.Peek().CacheContainer;
            }

            container = new(_factory.CurrentFunction ?? _topLevelMethod, _topLevelMethodOrdinal, localFunctionOrdinal, generation);
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

        // Obviously,
        if (_factory is { CurrentFunction: null, TopLevelMethod.Arity: 0 })
        {
            return true;
        }

        var methodTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
        try
        {
            methodTypeParameters.AddAll(_topLevelMethod.TypeParameters);

            for (Symbol? s = _factory.CurrentFunction; s is MethodSymbol m; s = s.ContainingSymbol)
            {
                methodTypeParameters.AddAll(m.TypeParameters);
            }

            if (methodTypeParameters.Count == 0)
            {
                return true;
            }

            var checker = TypeParameterUsageChecker.Instance;

            if (checker.Visit(delegateType, methodTypeParameters) || checker.Visit(targetMethod, methodTypeParameters))
            {
                return false;
            }
        }
        finally
        {
            methodTypeParameters.Free();
        }

        return true;
    }
}

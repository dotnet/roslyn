// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// This type helps rewrite the delegate creations that target static method groups to use a cached instance of delegate.
/// </summary>
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

    internal static bool CanRewrite(CSharpCompilation compilation, MethodSymbol topLevelMethod, bool inExpressionLambda, BoundConversion boundConversion, MethodSymbol targetMethod)
        => targetMethod.IsStatic
        && !boundConversion.IsExtensionMethod
        && !inExpressionLambda // The tree structure / meaning for expression trees should remain untouched.
        && topLevelMethod.MethodKind != MethodKind.StaticConstructor // Avoid caching twice if people do it manually.
        && compilation.IsStaticMethodGroupDelegateCacheEnabled
        ;

    internal BoundExpression Rewrite(int localFunctionOrdinal, SyntaxNode syntax, BoundDelegateCreationExpression boundDelegateCreation, MethodSymbol targetMethod, TypeSymbol delegateType)
    {
        Debug.Assert(delegateType.IsDelegateType());

        var oldSyntax = _factory.Syntax;
        _factory.Syntax = syntax;

        var cacheContainer = GetOrAddCacheContainer(localFunctionOrdinal, delegateType, targetMethod);
        var cacheField = cacheContainer.GetOrAddCacheField(_factory, delegateType, targetMethod);

        var boundCacheField = _factory.Field(null, cacheField);
        var rewrittenNode = _factory.Coalesce(boundCacheField, _factory.AssignmentExpression(boundCacheField, boundDelegateCreation));

        _factory.Syntax = oldSyntax;

        return rewrittenNode;
    }

    private DelegateCacheContainer GetOrAddCacheContainer(int localFunctionOrdinal, TypeSymbol delegateType, MethodSymbol targetMethod)
    {
        Debug.Assert(_factory.ModuleBuilderOpt is { });
        Debug.Assert(_factory.CurrentFunction is { });

        var typeCompilationState = _factory.CompilationState;
        var generation = _factory.ModuleBuilderOpt.CurrentGenerationOrdinal;

        DelegateCacheContainer? container;

        if (CanUseTypeScopedConcreteCacheContainer(delegateType, targetMethod))
        {
            container = typeCompilationState.ConcreteDelegateCacheContainer;

            if (container is { })
            {
                return container;
            }

            container = new DelegateCacheContainer(typeCompilationState.Type, generation);
            typeCompilationState.ConcreteDelegateCacheContainer = container;
        }
        else
        {
            var containersStack = _genericCacheContainers ??= new Stack<(int LocalFunctionOrdinal, DelegateCacheContainer CacheContainer)>();

            while (containersStack.Count > 0 && containersStack.Peek().LocalFunctionOrdinal > localFunctionOrdinal)
            {
                containersStack.Pop();
            }

            if (containersStack.Count > 0 && containersStack.Peek().LocalFunctionOrdinal == localFunctionOrdinal)
            {
                return containersStack.Peek().CacheContainer;
            }

            container = new DelegateCacheContainer(_factory.CurrentFunction, _topLevelMethodOrdinal, localFunctionOrdinal, generation);
            containersStack.Push((localFunctionOrdinal, container));
        }

        _factory.AddNestedType(container);

        return container;
    }

    private bool CanUseTypeScopedConcreteCacheContainer(TypeSymbol delegateType, MethodSymbol targetMethod)
    {
        // Possible places for type parameters that can act as type arguments to construct the delegateType or targetMethod:
        //   1. containing types
        //   2. top level method
        //   3. local functions
        // Since our containers are created within the same enclosing types, we can ignore type parameters from them.

        var methodTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
        try
        {
            for (Symbol? enclosingSymbol = _factory.CurrentFunction; enclosingSymbol is MethodSymbol enclosingMethod; enclosingSymbol = enclosingSymbol.ContainingSymbol)
            {
                if (enclosingMethod.Arity > 0)
                {
                    if (targetMethod.MethodKind == MethodKind.LocalFunction)
                    {
                        // Local functions can reference type parameters from their enclosing methods!
                        //
                        // For example:
                        //   void Test<T>()
                        //   {
                        //       var t = Target<int>;
                        //       static object Target<V>() => default(T);
                        //   }
                        //
                        // Therefore, unless no method type parameters for the target local function to use,
                        // we cannot safely use a type scoped concrete cache container without some deep analysis.

                        return false;
                    }

                    methodTypeParameters.AddAll(enclosingMethod.TypeParameters);
                }
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// This type helps rewrite the delegate creations that target static method groups to use a cached instance of delegate.
/// </summary>
internal sealed class DelegateCacheRewriter
{
    private readonly SyntheticBoundNodeFactory _factory;
    private readonly int _topLevelMethodOrdinal;

    private Dictionary<MethodSymbol, DelegateCacheContainer>? _genericCacheContainers;

    internal DelegateCacheRewriter(SyntheticBoundNodeFactory factory, int topLevelMethodOrdinal)
    {
        Debug.Assert(factory.TopLevelMethod is { });

        _factory = factory;
        _topLevelMethodOrdinal = topLevelMethodOrdinal;
    }

    internal static bool CanRewrite(BoundDelegateCreationExpression boundDelegateCreation)
    {
        var targetMethod = boundDelegateCreation.MethodOpt;

        Debug.Assert(targetMethod is { });

        return targetMethod.IsStatic && !boundDelegateCreation.IsExtensionMethod;
    }

    internal BoundExpression Rewrite(BoundDelegateCreationExpression boundDelegateCreation)
    {
        Debug.Assert(boundDelegateCreation.MethodOpt is { });

        var oldSyntax = _factory.Syntax;
        _factory.Syntax = boundDelegateCreation.Syntax;

        var cacheContainer = GetOrAddCacheContainer(boundDelegateCreation);
        var cacheField = cacheContainer.GetOrAddCacheField(_factory, boundDelegateCreation);

        var boundCacheField = _factory.Field(receiver: null, cacheField);
        var rewrittenNode = _factory.Coalesce(boundCacheField, _factory.AssignmentExpression(boundCacheField, boundDelegateCreation));

        _factory.Syntax = oldSyntax;

        return rewrittenNode;
    }

    private DelegateCacheContainer GetOrAddCacheContainer(BoundDelegateCreationExpression boundDelegateCreation)
    {
        Debug.Assert(_factory.ModuleBuilderOpt is { });
        Debug.Assert(_factory.CurrentFunction is { });

        var generation = _factory.ModuleBuilderOpt.CurrentGenerationOrdinal;

        DelegateCacheContainer? container;

        // We don't need to synthesize a container for each and every function.
        //
        // For example:
        //   void LF1<T>()
        //   {
        //       void LF2<G>()
        //       {
        //           void LF3()
        //           {
        //               Func<T> d = SomeMethod<T>;
        //               static void LF4 () { Func<T> d = SomeMethod<T>; }
        //           }
        //
        //           void LF5()
        //           {
        //               Func<T> d = SomeMethod<T>;
        //           }
        //       }
        //   }
        //
        // In the above case, only one cached delegate is necessary, and it could be assigned to the container 'owned' by LF1.

        if (!TryGetOwnerFunction(_factory.CurrentFunction, boundDelegateCreation, out var ownerFunction))
        {
            var typeCompilationState = _factory.CompilationState;
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
            var containers = _genericCacheContainers ??= new Dictionary<MethodSymbol, DelegateCacheContainer>(ReferenceEqualityComparer.Instance);

            if (containers.TryGetValue(ownerFunction, out container))
            {
                return container;
            }

            container = new DelegateCacheContainer(ownerFunction, _topLevelMethodOrdinal, containers.Count, generation);
            containers.Add(ownerFunction, container);
        }

        _factory.AddNestedType(container);

        return container;
    }

    private static bool TryGetOwnerFunction(MethodSymbol currentFunction, BoundDelegateCreationExpression boundDelegateCreation, [NotNullWhen(true)] out MethodSymbol? ownerFunction)
    {
        var targetMethod = boundDelegateCreation.MethodOpt;
        Debug.Assert(targetMethod is { });

        if (targetMethod.MethodKind == MethodKind.LocalFunction)
        {
            // Local functions can use type parameters from their enclosing methods!
            //
            // For example:
            //   void Test<T>()
            //   {
            //       var t = Target<int>;
            //       static object Target<V>() => default(T);
            //   }
            //
            // Therefore, without too much analysis, we select the closest generic enclosing function as the cache container owner.

            for (Symbol? enclosingSymbol = currentFunction; enclosingSymbol is MethodSymbol enclosingMethod; enclosingSymbol = enclosingSymbol.ContainingSymbol)
            {
                if (enclosingMethod.Arity > 0)
                {
                    ownerFunction = enclosingMethod;
                    return true;
                }
            }

            ownerFunction = null;
            return false;
        }

        // @AlekseyTs: It is Ok to create delegates for other method kinds as well.
        // @jcouv: We'd likely want to pay attention to this code if this happens.
        // What we really cared above was,
        // - "Are there any type parameters from the target method that we cannot discover simply from it's signature?"
        // As of C# 10, we only observe local functions could potentially answer yes, so we used that.
        // If this is hit, feel free to change but please also add tests.
        Debug.Assert(targetMethod.MethodKind == MethodKind.Ordinary);

        var usedTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
        try
        {
            if ((targetMethod.IsAbstract || targetMethod.IsVirtual) && boundDelegateCreation.Argument is BoundTypeExpression typeExpression)
            {
                FindTypeParameters(typeExpression.Type, usedTypeParameters);
            }

            var delegateType = boundDelegateCreation.Type;

            FindTypeParameters(delegateType, usedTypeParameters);
            FindTypeParameters(targetMethod, usedTypeParameters);

            for (Symbol? enclosingSymbol = currentFunction; enclosingSymbol is MethodSymbol enclosingMethod; enclosingSymbol = enclosingSymbol.ContainingSymbol)
            {
                if (usedTypeParametersContains(usedTypeParameters, enclosingMethod.TypeParameters))
                {
                    ownerFunction = enclosingMethod;
                    return true;
                }
            }

            ownerFunction = null;
            return false;
        }
        finally
        {
            usedTypeParameters.Free();
        }

        static bool usedTypeParametersContains(HashSet<TypeParameterSymbol> used, ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            foreach (var typeParameter in typeParameters)
            {
                if (used.Contains(typeParameter))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static void FindTypeParameters(TypeSymbol type, HashSet<TypeParameterSymbol> result)
        => type.VisitType(s_typeParameterSymbolCollector, result, visitCustomModifiers: true);

    private static void FindTypeParameters(MethodSymbol method, HashSet<TypeParameterSymbol> result)
    {
        FindTypeParameters(method.ContainingType, result);

        foreach (var typeArgument in method.TypeArgumentsWithAnnotations)
        {
            typeArgument.VisitType(type: null, typeWithAnnotationsPredicate: null, s_typeParameterSymbolCollector, result, visitCustomModifiers: true);
        }
    }

    private static readonly Func<TypeSymbol, HashSet<TypeParameterSymbol>, bool, bool> s_typeParameterSymbolCollector = (typeSymbol, result, _) =>
    {
        if (typeSymbol is TypeParameterSymbol typeParameter)
        {
            result.Add(typeParameter);
        }

        return false;
    };
}

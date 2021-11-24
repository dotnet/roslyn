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

internal class LoweredDelegateCachingFactory
{
    public LoweredDelegateCachingFactory(SyntheticBoundNodeFactory factory, int methodOrdinal)
    {

    }

    public BoundExpression RewriteStaticMethodGroupConversion(SyntaxNode syntax, BoundExpression receiver, MethodSymbol targetMethod, NamedTypeSymbol delegateType)
    {
        Debug.Assert(delegateType.IsDelegateType());
        Debug.Assert((object)targetMethod != null);

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

    //private DelegateCacheContainer GetOrAddCacheContainer(NamedTypeSymbol delegateType, MethodSymbol targetMethod)
    //{
    //    Debug.Assert(_factory is { TopLevelMethod: { }, ModuleBuilderOpt: { } });

    //    switch (ChooseDelegateCacheContainerKind(_factory.TopLevelMethod, delegateType, targetMethod))
    //    {
    //        case DelegateCacheContainerKind.ModuleScopedConcrete:
    //            return _factory.ModuleBuilderOpt.GetOrAddContainer(delegateType);
    //        case DelegateCacheContainerKind.TypeScopedConcrete:
    //            return _factory.CompilationState.TypeScopedDelegateCacheContainer;
    //        case DelegateCacheContainerKind.MethodScopedGeneric:
    //            return MethodScopedGenericDelegateCacheContainer;
    //        default:
    //            throw ExceptionUtilities.Unreachable;
    //    }
    //}

    private static bool AConcreteContainerIsOK(MethodSymbol currentMethod, MethodSymbol currentFunction, NamedTypeSymbol delegateType, MethodSymbol targetMethod)
    {
        // All the possible type parameters that act as type arguments needed to construct the delegateType or targetMethod
        // come from either the current method or local functions.

        // So obviously,
        if (currentMethod.Arity == 0)
        {
            return true;
        }

        var typeParams = PooledHashSet<TypeParameterSymbol>.GetInstance();
        try
        {
            typeParams.AddAll(currentMethod.TypeParameters);

            if (delegateType.ContainsTypeParameters(typeParams) || containsTypeParameters(targetMethod, typeParams))
            {
                return false;
            }
        }
        finally
        {
            typeParams.Free();
        }

        // If not, we can just use the delegateType for the cache field of the container as is.
        return true;

        static bool containsTypeParameter(MethodSymbol method)
        {
            return method.ContainingType.ContainsTypeParameter() ||
                method.TypeArgumentsWithAnnotations.Any(static (typeArg, _) =>
                    typeArg.Type.ContainsTypeParameter() ||
                    typeArg.CustomModifiers.Any(static (cm, unused) => ((TypeSymbol)cm.Modifier).ContainsTypeParameter(), 0), 0);
        }

        static bool containsTypeParameters(MethodSymbol method, System.Collections.Generic.HashSet<TypeParameterSymbol> typeParams)
        {
            return method.ContainingType.ContainsTypeParameters(typeParams) ||
                method.TypeArgumentsWithAnnotations.Any(static (typeArg, typeParams) =>
                    typeArg.Type.ContainsTypeParameters(typeParams) ||
                    typeArg.CustomModifiers.Any(static (cm, typeParams) => ((TypeSymbol)cm.Modifier).ContainsTypeParameters(typeParams), typeParams), typeParams);
        }
    }
}

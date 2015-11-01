// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This class holds all types of delegates that are coverted from a target method,
    /// by defining sub classes represented by <see cref="MethodGroupConversionCacheDelegateFrame"/> that reprensent a type of delegate.
    /// </summary>
    /// <remarks>
    /// Contains <see cref="ArrayBuilder{T}"/> members so need to call Free().
    /// </remarks>
    internal sealed class MethodGroupConversionCacheTargetFrame : SynthesizedContainer
    {
        private readonly Symbol _ContainingSymbol;
        public override Symbol ContainingSymbol => _ContainingSymbol;

        public override TypeKind TypeKind => TypeKind.Class;

        public override bool IsStatic => true;

        private readonly ArrayBuilder<MethodGroupConversionCacheDelegateFrame> DelegateFrames;

        private MethodGroupConversionCacheTargetFrame(
                NamedTypeSymbol containingType,
                string name,
                int typeParametersCount
            )
            : base(name, typeParametersCount, true)
        {
            _ContainingSymbol = containingType;
            DelegateFrames = ArrayBuilder<MethodGroupConversionCacheDelegateFrame>.GetInstance();
        }

        public static MethodGroupConversionCacheTargetFrame Create(NamedTypeSymbol containingType, MethodSymbol targetMethod, int typeParametersCount)
        {
            var frameName = GeneratedNames.MakeMethodGroupConversionCacheTargetFrameName(targetMethod.OriginalDefinition);
            return new MethodGroupConversionCacheTargetFrame(containingType, frameName, typeParametersCount);
        }

        public static ImmutableArray<TypeSymbol> GetTypeArgumentsFromTarget(MethodSymbol targetMethod)
        {
            var typeArgumentsBuilder = ArrayBuilder<TypeSymbol>.GetInstance();

            if (targetMethod.Arity > 0)
            {
                var args = ((ConstructedMethodSymbol)targetMethod).TypeArguments;
                AddTypeArgumentsReversed(typeArgumentsBuilder, ref args);
            }

            var containingType = targetMethod.ContainingType;
            while (containingType != null)
            {
                if ( containingType.Arity > 0 )
                {
                    var args = containingType.TypeArguments;
                    AddTypeArgumentsReversed(typeArgumentsBuilder, ref args);
                }

                containingType = containingType.ContainingType;
            }

            typeArgumentsBuilder.ReverseContents();
            return typeArgumentsBuilder.ToImmutableAndFree();
        }

        public MethodGroupConversionCacheDelegateFrame GetOrAddDelegateFrame(NamedTypeSymbol delegateType, out bool wasAdded)
        {
            var originalDefinition = delegateType.OriginalDefinition;

            for (var i = 0; i < DelegateFrames.Count; i++)
            {
                var frame = DelegateFrames[i];
                if (frame.DelegateField.Type == originalDefinition)
                {
                    wasAdded = false;
                    return frame;
                }
            }

            var delegateFrame = MethodGroupConversionCacheDelegateFrame.Create(this, originalDefinition);
            DelegateFrames.Add(delegateFrame);

            wasAdded = true;
            return delegateFrame;
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            var members = base.GetMembers();
            if (DelegateFrames.Count > 0)
            {
                members = StaticCast<Symbol>.From(DelegateFrames.ToImmutable()).AddRange(members);
            }
            return members;
        }

        private static void AddTypeArgumentsReversed(ArrayBuilder<TypeSymbol> builder, ref ImmutableArray<TypeSymbol> args)
        {
            var i = args.Length;
            while (i-- > 0)
            {
                builder.Add(args[i]);
            }
        }

        public void Free()
        {
            DelegateFrames.Free();
        }
    }
}

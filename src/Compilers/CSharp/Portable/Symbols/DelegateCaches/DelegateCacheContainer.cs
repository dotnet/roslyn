// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a container that holds the delegate caches for method group converisons.
    /// </summary>
    internal abstract class DelegateCacheContainer : SynthesizedContainerBase
    {
        internal readonly DelegateCacheContainerKind ContainerKind;

        protected DelegateCacheContainer(DelegateCacheContainerKind kind)
        {
            ContainerKind = kind;
        }

        public override bool IsStatic => true;

        public override TypeKind TypeKind => TypeKind.Class;

        internal abstract FieldSymbol GetOrAddCacheField(SyntheticBoundNodeFactory factory, NamedTypeSymbol delegateType, MethodSymbol targetMethod);
    }
}

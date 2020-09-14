// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private INamedTypeSymbol? _currentNamedType = null;
        private ISymbol? _currentAccessorParent = null;

        private static BuilderDisposer<T> GetArrayBuilder<T>(out ArrayBuilder<T> builder)
        {
            builder = ArrayBuilder<T>.GetInstance();
            return new BuilderDisposer<T>(builder);
        }

        private ref struct BuilderDisposer<T>
        {
            private readonly ArrayBuilder<T> _builder;

            public BuilderDisposer(ArrayBuilder<T> builder)
            {
                _builder = builder;
            }

            public void Dispose()
            {
                _builder.Free();
            }
        }
    }
}

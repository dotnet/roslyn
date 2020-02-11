﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal abstract partial class AbstractCSharpReducer : AbstractReducer
    {
        protected AbstractCSharpReducer(ObjectPool<IReductionRewriter> pool) : base(pool)
        {
        }
    }
}

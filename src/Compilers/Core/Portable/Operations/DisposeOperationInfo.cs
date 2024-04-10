// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal readonly struct DisposeOperationInfo
    {
        public readonly IMethodSymbol? DisposeMethod;

        public readonly ImmutableArray<IArgumentOperation> DisposeArguments;

        public DisposeOperationInfo(IMethodSymbol? disposeMethod, ImmutableArray<IArgumentOperation> disposeArguments)
        {
            DisposeMethod = disposeMethod;
            DisposeArguments = disposeArguments;
        }
    }
}

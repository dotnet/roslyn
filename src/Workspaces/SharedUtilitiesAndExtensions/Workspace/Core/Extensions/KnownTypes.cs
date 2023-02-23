// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal readonly struct KnownTypes
{
    public readonly INamedTypeSymbol? TaskType;
    public readonly INamedTypeSymbol? TaskOfTType;
    public readonly INamedTypeSymbol? ValueTaskType;
    public readonly INamedTypeSymbol? ValueTaskOfTTypeOpt;

    public readonly INamedTypeSymbol? IEnumerableOfTType;
    public readonly INamedTypeSymbol? IEnumeratorOfTType;

    public readonly INamedTypeSymbol? IAsyncEnumerableOfTTypeOpt;
    public readonly INamedTypeSymbol? IAsyncEnumeratorOfTTypeOpt;

    internal KnownTypes(Compilation compilation)
    {
        TaskType = compilation.TaskType();
        TaskOfTType = compilation.TaskOfTType();
        ValueTaskType = compilation.ValueTaskType();
        ValueTaskOfTTypeOpt = compilation.ValueTaskOfTType();

        IEnumerableOfTType = compilation.IEnumerableOfTType();
        IEnumeratorOfTType = compilation.IEnumeratorOfTType();

        IAsyncEnumerableOfTTypeOpt = compilation.IAsyncEnumerableOfTType();
        IAsyncEnumeratorOfTTypeOpt = compilation.IAsyncEnumeratorOfTType();
    }
}

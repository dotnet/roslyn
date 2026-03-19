// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal readonly struct KnownTaskTypes(Compilation compilation)
{
    public readonly INamedTypeSymbol? TaskType = compilation.TaskType();
    public readonly INamedTypeSymbol? TaskOfTType = compilation.TaskOfTType();
    public readonly INamedTypeSymbol? ValueTaskType = compilation.ValueTaskType();
    public readonly INamedTypeSymbol? ValueTaskOfTType = compilation.ValueTaskOfTType();

    public readonly INamedTypeSymbol? IEnumerableOfTType = compilation.IEnumerableOfTType();
    public readonly INamedTypeSymbol? IEnumeratorOfTType = compilation.IEnumeratorOfTType();

    public readonly INamedTypeSymbol? IAsyncEnumerableOfTType = compilation.IAsyncEnumerableOfTType();
    public readonly INamedTypeSymbol? IAsyncEnumeratorOfTType = compilation.IAsyncEnumeratorOfTType();

    public bool IsTaskLike([NotNullWhen(true)] ITypeSymbol? returnType)
    {
        if (returnType is not null)
        {
            if (returnType.Equals(this.TaskType))
                return true;

            if (returnType.Equals(this.ValueTaskType))
                return true;

            if (returnType.OriginalDefinition.Equals(this.TaskOfTType))
                return true;

            if (returnType.OriginalDefinition.Equals(this.ValueTaskOfTType))
                return true;

            if (returnType.IsErrorType())
                return returnType.Name is "Task" or "ValueTask";
        }

        return false;
    }
}

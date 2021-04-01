// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal readonly struct KnownTaskTypes
    {
        public INamedTypeSymbol? Task { get; }
        public INamedTypeSymbol? TaskOfT { get; }
        public INamedTypeSymbol? ValueTask { get; }
        public INamedTypeSymbol? ValueTaskOfT { get; }

        public INamedTypeSymbol? IEnumerableOfT { get; }
        public INamedTypeSymbol? IEnumeratorOfT { get; }
        public INamedTypeSymbol? IAsyncEnumerableOfT { get; }
        public INamedTypeSymbol? IAsyncEnumeratorOfT { get; }

        public KnownTaskTypes(Compilation compilation)
        {
            Task = compilation.TaskType();
            TaskOfT = compilation.TaskOfTType();
            ValueTask = compilation.ValueTaskType();
            ValueTaskOfT = compilation.ValueTaskOfTType();

            IEnumerableOfT = compilation.IEnumerableOfTType();
            IEnumeratorOfT = compilation.IEnumeratorOfTType();
            IAsyncEnumerableOfT = compilation.IAsyncEnumerableOfTType();
            IAsyncEnumeratorOfT = compilation.IAsyncEnumeratorOfTType();
        }

        public bool IsTaskLikeType(ITypeSymbol type)
        {
            if (type.Equals(Task, SymbolEqualityComparer.Default) ||
                type.Equals(ValueTask, SymbolEqualityComparer.Default) ||
                type.OriginalDefinition.Equals(TaskOfT, SymbolEqualityComparer.Default) ||
                type.OriginalDefinition.Equals(ValueTaskOfT, SymbolEqualityComparer.Default))
            {
                return true;
            }

            if (type.IsErrorType())
            {
                return type.Name.Equals("Task") ||
                       type.Name.Equals("ValueTask");
            }

            return false;
        }

        public bool IsIAsyncEnumerableOrEnumerator(ITypeSymbol returnType)
            => returnType.OriginalDefinition.Equals(IAsyncEnumerableOfT, SymbolEqualityComparer.Default) ||
                returnType.OriginalDefinition.Equals(IAsyncEnumeratorOfT, SymbolEqualityComparer.Default);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal abstract class AbstractMakeMethodAsynchronousService : IMakeMethodAsynchronousService
    {
        public abstract bool IsAsyncReturnType(ITypeSymbol type, KnownTaskTypes knownTaskTypes);

        public bool IsIAsyncEnumerableOrEnumerator(ITypeSymbol returnType, KnownTaskTypes knownTaskTypes)
            => returnType.OriginalDefinition.Equals(knownTaskTypes.IAsyncEnumerableOfT, SymbolEqualityComparer.Default) ||
                returnType.OriginalDefinition.Equals(knownTaskTypes.IAsyncEnumeratorOfT, SymbolEqualityComparer.Default);

        public bool IsTaskLikeType(ITypeSymbol type, KnownTaskTypes knownTaskTypes)
        {
            if (type.Equals(knownTaskTypes.Task, SymbolEqualityComparer.Default) ||
                type.Equals(knownTaskTypes.ValueTask, SymbolEqualityComparer.Default) ||
                type.OriginalDefinition.Equals(knownTaskTypes.TaskOfT, SymbolEqualityComparer.Default) ||
                type.OriginalDefinition.Equals(knownTaskTypes.ValueTaskOfT, SymbolEqualityComparer.Default))
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
    }
}

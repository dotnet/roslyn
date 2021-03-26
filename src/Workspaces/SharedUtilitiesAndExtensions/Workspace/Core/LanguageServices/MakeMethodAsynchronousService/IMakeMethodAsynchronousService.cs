// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal interface IMakeMethodAsynchronousService : ILanguageService
    {
        bool IsIAsyncEnumerableOrEnumerator(ITypeSymbol returnType, KnownTaskTypes knownTaskTypes);

        bool IsTaskLikeType(ITypeSymbol type, KnownTaskTypes knownTaskTypes);

        bool IsAsyncReturnType(ITypeSymbol type, KnownTaskTypes knownTaskTypes);
    }
}

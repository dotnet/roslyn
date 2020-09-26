// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal abstract partial class AbstractMakeMethodAsynchronousCodeFixProvider
    {
        internal readonly struct KnownTypes
        {
            public readonly INamedTypeSymbol _taskType;
            public readonly INamedTypeSymbol _taskOfTType;
            public readonly INamedTypeSymbol _valueTaskType;
            public readonly INamedTypeSymbol _valueTaskOfTTypeOpt;

            public readonly INamedTypeSymbol _iEnumerableOfTType;
            public readonly INamedTypeSymbol _iEnumeratorOfTType;

            public readonly INamedTypeSymbol _iAsyncEnumerableOfTTypeOpt;
            public readonly INamedTypeSymbol _iAsyncEnumeratorOfTTypeOpt;

            internal KnownTypes(Compilation compilation)
            {
                _taskType = compilation.TaskType();
                _taskOfTType = compilation.TaskOfTType();
                _valueTaskType = compilation.ValueTaskType();
                _valueTaskOfTTypeOpt = compilation.ValueTaskOfTType();

                _iEnumerableOfTType = compilation.IEnumerableOfTType();
                _iEnumeratorOfTType = compilation.IEnumeratorOfTType();

                _iAsyncEnumerableOfTTypeOpt = compilation.IAsyncEnumerableOfTType();
                _iAsyncEnumeratorOfTTypeOpt = compilation.IAsyncEnumeratorOfTType();
            }
        }
    }
}

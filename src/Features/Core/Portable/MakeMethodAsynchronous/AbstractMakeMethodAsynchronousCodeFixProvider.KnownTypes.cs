// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal abstract partial class AbstractMakeMethodAsynchronousCodeFixProvider
    {
        internal readonly struct KnownTypes
        {
            public readonly INamedTypeSymbol _taskType;
            public readonly INamedTypeSymbol _taskOfTType;
            public readonly INamedTypeSymbol _valueTaskOfTTypeOpt;

            public readonly INamedTypeSymbol _iEnumerableOfTType;
            public readonly INamedTypeSymbol _iEnumeratorOfTType;

            public readonly INamedTypeSymbol _iAsyncEnumerableOfTType;
            public readonly INamedTypeSymbol _iAsyncEnumeratorOfTType;

            internal KnownTypes(Compilation compilation)
            {
                _taskType = compilation.TaskType();
                _taskOfTType = compilation.TaskOfTType();
                _valueTaskOfTTypeOpt = compilation.ValueTaskOfTType();

                _iEnumerableOfTType = compilation.IEnumerableOfTType();
                _iEnumeratorOfTType = compilation.IEnumeratorOfTType();

                _iAsyncEnumerableOfTType = compilation.IAsyncEnumerableOfTType();
                _iAsyncEnumeratorOfTType = compilation.IAsyncEnumeratorOfTType();
            }
        }
    }
}

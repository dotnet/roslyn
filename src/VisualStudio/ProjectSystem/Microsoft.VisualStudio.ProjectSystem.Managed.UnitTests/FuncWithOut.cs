// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft
{
    public delegate TResult FuncWithOut<TOut, TResult>(out TOut result);
    public delegate TResult FuncWithOut<in T1, TOut, TResult>(T1 arg1, out TOut result);
}

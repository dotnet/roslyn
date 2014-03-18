// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// called when data stored in the cache is evicted.
    /// </summary>
    internal interface IWeakAction<T>
    {
        void Invoke(T value);
    }
}

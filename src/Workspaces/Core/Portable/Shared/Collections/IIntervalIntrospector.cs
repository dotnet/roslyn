﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal interface IIntervalIntrospector<T>
    {
        int GetStart(T value);
        int GetLength(T value);
    }
}

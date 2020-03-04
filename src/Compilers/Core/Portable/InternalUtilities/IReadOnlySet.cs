﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Roslyn.Utilities
{
    internal interface IReadOnlySet<T>
    {
        int Count { get; }
        bool Contains(T item);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal interface IIntervalIntrospector<T>
{
    int GetStart(T value);
    int GetLength(T value);
}

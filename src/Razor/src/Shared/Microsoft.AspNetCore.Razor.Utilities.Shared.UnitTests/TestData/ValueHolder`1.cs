// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.TestData;

public readonly record struct ValueHolder<T>(T Value)
{
    public static implicit operator ValueHolder<T>(T value)
        => new(value);
}

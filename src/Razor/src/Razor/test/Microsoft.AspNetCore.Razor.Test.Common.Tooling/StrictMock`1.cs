// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common;

public class StrictMock<T> : Mock<T>
    where T : class
{
    public StrictMock()
        : base(MockBehavior.Strict)
    {
    }

    public StrictMock(params object?[] args)
        : base(MockBehavior.Strict, args)
    {
    }
}

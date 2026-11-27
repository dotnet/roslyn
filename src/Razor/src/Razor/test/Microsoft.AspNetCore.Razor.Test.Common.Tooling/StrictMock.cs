// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common;

public static class StrictMock
{
    public static T Of<T>()
        where T : class
        => new Mock<T>(MockBehavior.Strict).Object;
    public static T Of<T>(Expression<Func<T, bool>> predicate)
        where T : class
        => new MockRepository(MockBehavior.Strict).OneOf<T>(predicate);
}

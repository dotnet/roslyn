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
        => Mock.Of<T>(MockBehavior.Strict);

    public static T Of<T>(Expression<Func<T, bool>> predicate)
        where T : class
        => Mock.Of<T>(predicate, MockBehavior.Strict);
}

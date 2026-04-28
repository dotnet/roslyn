// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.TestData;

public abstract class EnumerableOrderingTestBase : OrderingTestBase<IEnumerable<int>, IEnumerable<ValueHolder<int>>, OrderingCaseConverters.Enumerable>
{
}

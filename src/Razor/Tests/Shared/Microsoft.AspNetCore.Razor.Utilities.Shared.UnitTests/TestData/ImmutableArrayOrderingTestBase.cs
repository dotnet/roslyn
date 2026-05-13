// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.TestData;

public abstract class ImmutableArrayOrderingTestBase : OrderingTestBase<ImmutableArray<int>, ImmutableArray<ValueHolder<int>>, OrderingCaseConverters.ImmutableArray>
{
}

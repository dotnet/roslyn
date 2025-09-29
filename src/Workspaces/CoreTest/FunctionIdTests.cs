// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class FunctionIdTests
{
    [Fact]
    public void NoDuplicateValues()
    {
        var map = new Dictionary<FunctionId, string>();
        foreach (var name in Enum.GetNames(typeof(FunctionId)))
        {
            var value = (FunctionId)Enum.Parse(typeof(FunctionId), name);
            if (map.TryGetValue(value, out var existingName))
            {
                Assert.True(false, $"'{nameof(FunctionId)}.{name}' cannot have the same value as '{nameof(FunctionId)}.{existingName}'");
            }

            map.Add(value, name);
        }
    }
}

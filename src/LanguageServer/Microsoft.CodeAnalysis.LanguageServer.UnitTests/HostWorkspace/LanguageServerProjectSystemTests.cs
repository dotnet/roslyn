// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.NET.ProjectData;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.HostWorkspace;

public sealed class LanguageServerProjectSystemTests
{
    [Theory]
    [InlineData("false", false)]
    [InlineData("true", true)]
    [InlineData(null, true)]
    public void GetReferenceOutputAssembly_PreservesCachedValue(string? cachedValue, bool expected)
    {
        var metadata = new KeyValueCollection(
            new KeySchema(["ReferenceOutputAssembly"]),
            [cachedValue]);
        var item = new ProjectDataItem("Referenced.csproj", metadata);

        Assert.Equal(expected, LanguageServerProjectSystem.GetReferenceOutputAssembly(item));
    }
}

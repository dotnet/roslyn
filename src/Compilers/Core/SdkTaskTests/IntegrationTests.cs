// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.Sdk.UnitTests;

public sealed class IntegrationTests : IntegrationTestBase
{
    public IntegrationTests(ITestOutputHelper output) : base(output)
    {
        // Our task will search for `../bincore` directory.
        // We cannot populate it during build because our test infrastructure
        // would not rehydrate that (as it's outside the output directory).
        var source = "bincore";
        var target = "../bincore";
        if (!Directory.Exists(target))
        {
            Directory.Move(source, target);
        }
    }
}

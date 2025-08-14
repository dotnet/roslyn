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
        _buildTaskDll = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "../tasks/netcore/binfx/Microsoft.Build.Tasks.CodeAnalysis.Sdk.dll");
    }
}

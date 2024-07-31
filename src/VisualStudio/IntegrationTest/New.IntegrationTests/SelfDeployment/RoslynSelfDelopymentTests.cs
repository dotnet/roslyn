// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.DevLoop;

[IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "TestHive", MaxAttempts = 1)]
public class RoslynSelfBuildTests : AbstractIntegrationTest
{

    [IdeFact]
    public async Task Test()
    {
        var solutionDir = @"D:\Sample\roslyn\roslyn.sln";

    }
}

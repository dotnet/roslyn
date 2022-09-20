// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Host;

internal class WorkspaceTestLogger : IWorkspaceTestLogger
{
    public ITestOutputHelper? OutputHelper { get; set; }

    public void Log(string message)
        => OutputHelper?.WriteLine(message);
}

internal static class WorkspaceTestLoggerExtensions
{
    public static void SetWorkspaceTestOutput(this SolutionServices services, ITestOutputHelper outputHelper)
        => Assert.IsType<WorkspaceTestLogger>(services.GetRequiredService<IWorkspaceTestLogger>()).OutputHelper = outputHelper;
}


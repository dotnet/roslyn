// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceService(typeof(IWorkspaceTestLogger), ServiceLayer.Host), Shared, PartNotDiscoverable]
internal sealed class WorkspaceTestLogger : IWorkspaceTestLogger
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceTestLogger()
    {
    }

    public ITestOutputHelper? OutputHelper { get; set; }

    public void Log(string message)
        => OutputHelper?.WriteLine(message);
}

internal static class WorkspaceTestLoggerExtensions
{
    public static void SetWorkspaceTestOutput(this SolutionServices services, ITestOutputHelper outputHelper)
        => Assert.IsType<WorkspaceTestLogger>(services.GetRequiredService<IWorkspaceTestLogger>()).OutputHelper = outputHelper;
}


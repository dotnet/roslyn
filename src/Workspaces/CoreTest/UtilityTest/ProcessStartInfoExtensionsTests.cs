// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest;

public sealed class ProcessStartInfoExtensionsTests
{
    [Fact]
    public void RemoveInheritedDotNetDiagnosticPorts()
    {
        var processStartInfo = new ProcessStartInfo();
        processStartInfo.Environment["DOTNET_DiagnosticPorts"] = "diagnostic-port";
        processStartInfo.Environment["DOTNET_DefaultDiagnosticPortSuspend"] = "1";
        processStartInfo.Environment["Unrelated"] = "value";

        processStartInfo.RemoveInheritedDotNetDiagnosticPorts();

        Assert.False(processStartInfo.Environment.ContainsKey("DOTNET_DiagnosticPorts"));
        Assert.False(processStartInfo.Environment.ContainsKey("DOTNET_DefaultDiagnosticPortSuspend"));
        Assert.Equal("value", processStartInfo.Environment["Unrelated"]);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Testing;
using Microsoft.Testing.Platform.ServerMode.Client;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Testing;

public sealed class MtpTestRunnerTests
{
    [Fact]
    public void TryGetFullyQualifiedNameUsesVsTestProperty()
    {
        var test = CreateTestNode(("vstest.TestCase.FullyQualifiedName", "Namespace.Type.Test"));

        Assert.Equal("Namespace.Type.Test", MtpTestRunner.TryGetFullyQualifiedName(test));
    }

    [Fact]
    public void TryGetFullyQualifiedNameUsesManagedLocation()
    {
        var test = CreateTestNode(
            ("location.type", "Namespace.Type"),
            ("location.method", "Test(System.Int32)"));

        Assert.Equal("Namespace.Type.Test(System.Int32)", MtpTestRunner.TryGetFullyQualifiedName(test));
    }

    [Fact]
    public void CreateProgressMapsTerminalStates()
    {
        var progress = MtpTestRunner.CreateProgress(
            totalTests: 6,
            ["passed", "skipped", "failed", "timed-out", "error", "canceled"]);

        Assert.Equal(1, progress.TestsPassed);
        Assert.Equal(3, progress.TestsFailed);
        Assert.Equal(1, progress.TestsSkipped);
        Assert.Equal(6, progress.TotalTests);
    }

    [Fact]
    public void TryUpdateTerminalStateIgnoresDuplicatesAndKeepsLatestState()
    {
        var terminalStates = new Dictionary<string, string>();

        Assert.True(MtpTestRunner.TryUpdateTerminalState(
            terminalStates,
            CreateTestNode(("execution-state", "failed"))));
        Assert.False(MtpTestRunner.TryUpdateTerminalState(
            terminalStates,
            CreateTestNode(("execution-state", "failed"))));
        Assert.True(MtpTestRunner.TryUpdateTerminalState(
            terminalStates,
            CreateTestNode(("execution-state", "passed"))));
        Assert.False(MtpTestRunner.TryUpdateTerminalState(
            terminalStates,
            CreateTestNode(("execution-state", "running"))));

        Assert.Equal("passed", terminalStates["test-id"]);
    }

    private static MtpTestNodeUpdate CreateTestNode(params (string Name, object? Value)[] properties)
    {
        var node = new Dictionary<string, object?>
        {
            ["uid"] = "test-id",
            ["display-name"] = "Test",
            ["node-type"] = "action",
        };

        foreach (var (name, value) in properties)
            node.Add(name, value);

        return new MtpTestNodeUpdate(node, parentUid: null);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class ManagedCompilerGlobalCacheTests : TestBase
{
    private const string RoslynCachePathEnvironmentVariable = "ROSLYN_CACHE_PATH";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UseGlobalCacheFeatureFlag_IsIgnoredWhenAbsent(bool visualBasic)
    {
        var messages = ExecuteCompiler(
            visualBasic,
            features: null,
            throwWhen: static message => message.StartsWith("BuildResponseFile =", StringComparison.Ordinal));

        Assert.DoesNotContain(messages, static message => message.Contains("ROSLYN_CACHE_PATH", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UseGlobalCacheFeatureFlag_UsesDefaultPath(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), "roslyn-cache");
        var messages = ExecuteCompiler(
            visualBasic,
            features: "use-global-cache",
            throwWhen: static message => message.Contains("Setting ROSLYN_CACHE_PATH", StringComparison.Ordinal));

        Assert.Contains(messages, message => message.Contains($"Setting ROSLYN_CACHE_PATH to '{expectedPath}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UseGlobalCacheFeatureFlag_UsesExplicitPath(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), "custom-cache-path");
        var messages = ExecuteCompiler(
            visualBasic,
            features: $"use-global-cache={expectedPath}",
            throwWhen: static message => message.Contains("Setting ROSLYN_CACHE_PATH", StringComparison.Ordinal));

        Assert.Contains(messages, message => message.Contains($"Setting ROSLYN_CACHE_PATH to '{expectedPath}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UseGlobalCacheFeatureFlag_DoesNotOverrideExistingEnvironmentVariable(bool visualBasic)
    {
        var existingPath = Path.Combine(Path.GetTempPath(), "existing-cache-path");
        var messages = ExecuteCompiler(
            visualBasic,
            features: $"use-global-cache={Path.Combine(Path.GetTempPath(), "ignored-cache-path")}",
            environmentVariables:
            [
                new KeyValuePair<string, string?>(RoslynCachePathEnvironmentVariable, existingPath),
            ],
            throwWhen: static message => message.Contains("Environment variable ROSLYN_CACHE_PATH is already set.", StringComparison.Ordinal));

        Assert.Contains(messages, static message => message.Contains("Environment variable ROSLYN_CACHE_PATH is already set. Skipping use-global-cache feature flag value.", StringComparison.Ordinal));
        Assert.DoesNotContain(messages, static message => message.Contains("ignored-cache-path", StringComparison.Ordinal));
    }

    private static List<string> ExecuteCompiler(
        bool visualBasic,
        string? features,
        IEnumerable<KeyValuePair<string, string?>>? environmentVariables,
        Func<string, bool> throwWhen)
    {
        var compiler = CreateCompiler(visualBasic);
        compiler.Features = features;

        var logger = new CollectingCompilerServerLogger(throwWhen);
        ApplyEnvironmentVariables(environmentVariables ?? [], () => compiler.ExecuteTool(compiler.PathToBuiltInTool, "", "", logger));
        return logger.Messages;
    }

    private static List<string> ExecuteCompiler(
        bool visualBasic,
        string? features,
        Func<string, bool> throwWhen)
        => ExecuteCompiler(visualBasic, features, environmentVariables: null, throwWhen);

    private static ManagedCompiler CreateCompiler(bool visualBasic)
    {
        ManagedCompiler compiler = visualBasic
            ? new Vbc()
            : new Csc();

        compiler.BuildEngine = new MockEngine();
        compiler.UseSharedCompilation = true;
        compiler.SharedCompilationId = Guid.NewGuid().ToString("N");
        compiler.UseAppHost_TestOnly = true;
        return compiler;
    }

    private sealed class CollectingCompilerServerLogger(Func<string, bool> throwWhen) : ICompilerServerLogger
    {
        private readonly Func<string, bool> _throwWhen = throwWhen;

        public List<string> Messages { get; } = [];

        public bool IsLogging => true;

        public void Log(string message)
        {
            Messages.Add(message);

            if (_throwWhen(message))
            {
                throw new OperationCanceledException();
            }
        }
    }
}
#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public sealed class BuildServerConnectionTests : IDisposable
    {
        internal TempRoot TempRoot { get; } = new TempRoot();
        internal XunitCompilerServerLogger Logger { get; }

        public BuildServerConnectionTests(ITestOutputHelper testOutputHelper)
        {
            Logger = new XunitCompilerServerLogger(testOutputHelper);
        }

        public void Dispose()
        {
            TempRoot.Dispose();
        }

        [Fact]
        public async Task OnlyStartOneServer()
        {
            ServerData? serverData = null;
            try
            {
                var pipeName = ServerUtil.GetPipeName();
                var workingDirectory = TempRoot.CreateDirectory().Path;
                for (var i = 0; i < 5; i++)
                {
                    var response = await BuildServerConnection.RunServerBuildRequestAsync(
                        ProtocolUtil.CreateEmptyCSharp(workingDirectory),
                        pipeName,
                        timeoutOverride: Timeout.Infinite,
                        tryCreateServerFunc: (pipeName, logger) =>
                        {
                            Assert.Null(serverData);
                            serverData = ServerData.Create(logger, pipeName);
                            return true;
                        },
                        Logger,
                        cancellationToken: default);
                    Assert.True(response is CompletedBuildResponse);
                }
            }
            finally
            {
                serverData?.Dispose();
            }
        }

        [Fact]
        public async Task UseExistingServer()
        {
            using var serverData = await ServerUtil.CreateServer(Logger);
            var ran = false;
            var workingDirectory = TempRoot.CreateDirectory().Path;
            for (var i = 0; i < 5; i++)
            {
                var response = await BuildServerConnection.RunServerBuildRequestAsync(
                    ProtocolUtil.CreateEmptyCSharp(workingDirectory),
                    serverData.PipeName,
                    timeoutOverride: Timeout.Infinite,
                    tryCreateServerFunc: (_, _) =>
                    {
                        ran = true;
                        return false;
                    },
                    Logger,
                    cancellationToken: default);
                Assert.True(response is CompletedBuildResponse);
            }

            Assert.False(ran);
        }

        /// <summary>
        /// Simulate the case where the server process crashes or hangs on startup 
        /// and make sure the client properly fails
        /// </summary>
        [Fact]
        public async Task SimulateServerCrashingOnStartup()
        {
            var pipeName = ServerUtil.GetPipeName();
            var ran = false;
            var response = await BuildServerConnection.RunServerBuildRequestAsync(
                ProtocolUtil.CreateEmptyCSharp(TempRoot.CreateDirectory().Path),
                pipeName,
                timeoutOverride: (int)TimeSpan.FromSeconds(2).TotalMilliseconds,
                tryCreateServerFunc: (_, _) =>
                {
                    ran = true;

                    // Correct this is a lie. The server did not start. But it also does a nice
                    // job of simulating a hung or crashed server.
                    return true;
                },
                Logger,
                cancellationToken: default);
            Assert.True(response is CannotConnectResponse);
            Assert.True(ran);
        }

        [Fact]
        public async Task FailedServer()
        {
            var pipeName = ServerUtil.GetPipeName();
            var workingDirectory = TempRoot.CreateDirectory().Path;
            var count = 0;
            for (var i = 0; i < 5; i++)
            {
                var response = await BuildServerConnection.RunServerBuildRequestAsync(
                    ProtocolUtil.CreateEmptyCSharp(workingDirectory),
                    pipeName,
                    timeoutOverride: Timeout.Infinite,
                    tryCreateServerFunc: (_, _) =>
                    {
                        count++;
                        return false;
                    },
                    Logger,
                    cancellationToken: default);
                Assert.True(response is CannotConnectResponse);
            }

            Assert.Equal(5, count);
        }

        [Fact]
        public void GetServerEnvironmentVariables_IncludesDotNetRoot()
        {
            // This test verifies that GetServerEnvironmentVariables properly sets up DOTNET_ROOT
            // without modifying the current process environment
            var currentEnvironment = Environment.GetEnvironmentVariables();
            var originalDotNetRoot = currentEnvironment[RuntimeHostInfo.DotNetRootEnvironmentName];

            var envVars = BuildServerConnection.GetServerEnvironmentVariables(currentEnvironment);

            if (RuntimeHostInfo.GetToolDotNetRoot() is { } dotNetRoot)
            {
                // Should have environment variables including DOTNET_ROOT
                Assert.NotNull(envVars);
                Assert.True(envVars.ContainsKey(RuntimeHostInfo.DotNetRootEnvironmentName));
                Assert.Equal(dotNetRoot, envVars[RuntimeHostInfo.DotNetRootEnvironmentName]);

                // Should include other environment variables from current process
                Assert.True(envVars.Count > 1);

                // Should not have modified the current process environment
                Assert.Equal(originalDotNetRoot, Environment.GetEnvironmentVariable(RuntimeHostInfo.DotNetRootEnvironmentName));
            }
            else
            {
                // If no DOTNET_ROOT is needed, should return null
                Assert.Null(envVars);
            }
        }

        [Fact]
        public void GetServerEnvironmentVariables_ExcludesDotNetRootVariants()
        {
            // This test verifies that DOTNET_ROOT* variables are properly cleared and replaced
            var testEnvVars = new[] { "DOTNET_ROOT_X64", "DOTNET_ROOT_X86", "DOTNET_ROOT_ARM64", "DOTNET_ROOT(x86)" };

            // Create a test environment with DOTNET_ROOT* variants
            var testEnvironment = new System.Collections.Hashtable();
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                testEnvironment[entry.Key] = entry.Value;
            }

            // Add test DOTNET_ROOT* variants
            foreach (var testEnvVar in testEnvVars)
            {
                testEnvironment[testEnvVar] = "test_value";
            }

            var envVars = BuildServerConnection.GetServerEnvironmentVariables(testEnvironment);

            if (envVars != null)
            {

                // Should set DOTNET_ROOT* variants to empty string to prevent inheritance
                foreach (var testEnvVar in testEnvVars)
                {
                    Assert.True(envVars.ContainsKey(testEnvVar), $"Environment variables should contain {testEnvVar}");
                    Assert.Equal(string.Empty, envVars[testEnvVar]);
                }
            }
        }

        [Fact]
        public void TryCreateServer_LogsErrorWhenDotNetHostPathNotFound()
        {
            // This test verifies that TryCreateServer logs an error when DOTNET_HOST_PATH is not found
            // We can't fully test TryCreateServer without a valid server executable, but we can test
            // the logging behavior by creating a mock scenario

            var logs = new List<string>();
            var testLogger = new TestCompilerServerLogger(logs);

            // Create a temporary directory with a fake server executable
            var clientDirectory = TempRoot.CreateDirectory().Path;
            var serverExePath = Path.Combine(clientDirectory, PlatformInformation.IsWindows ? "VBCSCompiler.exe" : "VBCSCompiler");

            // Create an empty file to simulate the server executable
            File.WriteAllText(serverExePath, string.Empty);

            // Save the original environment variables
            var originalDotNetHostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            var originalDotNetExperimentalHostPath = Environment.GetEnvironmentVariable("DOTNET_EXPERIMENTAL_HOST_PATH");

            try
            {
                // Clear both DOTNET_HOST_PATH environment variables to simulate the missing path scenario
                Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", null);
                Environment.SetEnvironmentVariable("DOTNET_EXPERIMENTAL_HOST_PATH", null);

                // Try to create the server
                var pipeName = ServerUtil.GetPipeName();
                var result = BuildServerConnection.TryCreateServer(clientDirectory, pipeName, testLogger, out var processId);

                // Verify that a warning was logged about DOTNET_HOST_PATH not being provided
                Assert.Contains(logs, log => log.Contains("Warning") && log.Contains("DOTNET_HOST_PATH"));
            }
            finally
            {
                // Restore original environment variables
                Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", originalDotNetHostPath);
                Environment.SetEnvironmentVariable("DOTNET_EXPERIMENTAL_HOST_PATH", originalDotNetExperimentalHostPath);
            }
        }

        private sealed class TestCompilerServerLogger : ICompilerServerLogger
        {
            private readonly List<string> _logs;

            public TestCompilerServerLogger(List<string> logs)
            {
                _logs = logs;
            }

            public bool IsLogging => true;

            public void Log(string message)
            {
                _logs.Add(message);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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

        [Fact]
        [WorkItem("https://github.com/dotnet/runtime/issues/134043")]
        public async Task UseExistingServerConcurrently()
        {
            using var serverData = await ServerUtil.CreateServer(Logger);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var workingDirectory = TempRoot.CreateDirectory().Path;
            var serverCreationCount = 0;

            for (var iteration = 0; iteration < 4; iteration++)
            {
                var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var requests = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
                {
                    await start.Task;
                    return await BuildServerConnection.RunServerBuildRequestAsync(
                        ProtocolUtil.CreateEmptyCSharp(workingDirectory),
                        serverData.PipeName,
                        timeoutOverride: (int)TimeSpan.FromSeconds(30).TotalMilliseconds,
                        tryCreateServerFunc: (_, _) =>
                        {
                            Interlocked.Increment(ref serverCreationCount);
                            return false;
                        },
                        Logger,
                        cancellationTokenSource.Token);
                })).ToArray();

                start.SetResult(true);
                var responses = await Task.WhenAll(requests);
                Assert.All(responses, response => Assert.IsType<CompletedBuildResponse>(response));
            }

            Assert.Equal(0, serverCreationCount);
        }

        [ConditionalFact(typeof(NotOnAnyMono))]
        [WorkItem("https://github.com/dotnet/runtime/issues/134043")]
        public void ClientMutexRequiresExplicitAcquisition()
        {
            var mutexName = BuildServerConnection.GetClientMutexName(ServerUtil.GetPipeName());
            using var clientMutex = BuildServerConnection.OpenOrCreateClientMutex(mutexName, out var holdsMutex);
            Assert.False(holdsMutex);

            RunOnOtherThread(() =>
            {
                using var otherMutex = BuildServerConnection.OpenOrCreateClientMutex(mutexName, out var otherHoldsMutex);
                Assert.False(otherHoldsMutex);
                Assert.True(otherMutex.TryLock(timeoutMs: 0));
            });

            Assert.True(clientMutex.TryLock(timeoutMs: 0));
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/runtime/issues/134043")]
        public void ClientMutexTimeoutDoesNotStartServer()
        {
            var pipeName = ServerUtil.GetPipeName();
            var mutexName = BuildServerConnection.GetClientMutexName(pipeName);
            var workingDirectory = TempRoot.CreateDirectory().Path;
            using var owner = BuildServerConnection.OpenOrCreateClientMutex(mutexName, out var holdsMutex);
            if (!holdsMutex)
            {
                Assert.True(owner.TryLock(timeoutMs: 0));
            }

            RunOnOtherThread(() =>
            {
                var startedServer = false;
                var response = BuildServerConnection.RunServerBuildRequestAsync(
                    ProtocolUtil.CreateEmptyCSharp(workingDirectory),
                    pipeName,
                    timeoutOverride: 0,
                    tryCreateServerFunc: (_, _) =>
                    {
                        startedServer = true;
                        return false;
                    },
                    Logger,
                    cancellationToken: default).GetAwaiter().GetResult();

                Assert.IsType<CannotConnectResponse>(response);
                Assert.False(startedServer);
            });
        }

        [ConditionalFact(typeof(NotOnAnyMono))]
        [WorkItem("https://github.com/dotnet/runtime/issues/134043")]
        public void AbandonedClientMutexIsReleased()
        {
            var mutexName = BuildServerConnection.GetClientMutexName(ServerUtil.GetPipeName());
            using var keepAlive = new Mutex(initiallyOwned: false, mutexName);
            using var clientMutex = BuildServerConnection.OpenOrCreateClientMutex(mutexName, out _);
            RunOnOtherThread(() =>
            {
                using var owner = new Mutex(initiallyOwned: false, mutexName);
                Assert.True(owner.WaitOne(TimeSpan.FromSeconds(30)));
                // Abandon ownership by exiting this thread without releasing the mutex.
            });

            Assert.Throws<AbandonedMutexException>(() => clientMutex.TryLock(timeoutMs: 0));
            clientMutex.Dispose();

            RunOnOtherThread(() =>
            {
                using var otherMutex = BuildServerConnection.OpenOrCreateClientMutex(mutexName, out _);
                Assert.True(otherMutex.TryLock(timeoutMs: 0));
            });
        }

        [ConditionalFact(typeof(NotOnAnyMono))]
        [WorkItem("https://github.com/dotnet/runtime/issues/134043")]
        public async Task ClientMutexReleaseFailureIncludesThreadIds()
        {
            var pipeName = ServerUtil.GetPipeName();
            var mutexName = BuildServerConnection.GetClientMutexName(pipeName);
            var threadId = 0;
            var exception = await Assert.ThrowsAsync<Exception>(() => BuildServerConnection.RunServerBuildRequestAsync(
                ProtocolUtil.CreateEmptyCSharp(TempRoot.CreateDirectory().Path),
                pipeName,
                timeoutOverride: 0,
                tryCreateServerFunc: (_, _) =>
                {
                    threadId = Environment.CurrentManagedThreadId;
                    using var mutex = Mutex.OpenExisting(mutexName);
                    mutex.ReleaseMutex();
                    return false;
                },
                Logger,
                cancellationToken: default));

            Assert.Equal($"ReleaseMutex failed. WaitOne Id: {threadId} Release Id: {threadId}", exception.Message);
            Assert.True(exception.InnerException is ApplicationException || exception.InnerException is InvalidOperationException);
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
        [WorkItem("https://github.com/dotnet/roslyn/issues/84072")]
        public void LogException_NonFatal()
        {
            Logger.Messages.Clear();
            Logger.LogException(CreateException(), "Testing");

            Assert.DoesNotContain(Logger.Messages, m => m.Contains("Error:"));
            Assert.Contains(Logger.Messages, m => m.Contains("Exception: 'InvalidOperationException' 'boom' occurred during 'Testing'"));
        }

        private static Exception CreateException()
        {
            try
            {
                throw new InvalidOperationException("boom", new Exception("inner"));
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/msbuild/issues/13844")]
        public async Task WaitForServerProcessExitAsync_CompletesWhenServerMutexIsNotOpen()
        {
            var pipeName = ServerUtil.GetPipeName();
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            using var currentProcess = Process.GetCurrentProcess();
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Assert.False(currentProcess.HasExited);
            Assert.False(BuildServerConnection.WasServerMutexOpen(mutexName));

            var waitTask = BuildServerConnection.WaitForServerProcessExitAsync(pipeName, currentProcess.Id, cancellationTokenSource.Token);
            var completedTask = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token));
            Assert.Same(waitTask, completedTask);
            await waitTask;
        }

        [Fact]
        public void GetServerEnvironmentVariables_IncludesDotNetRoot()
        {
            // This test verifies that GetServerEnvironmentVariables properly sets up DOTNET_ROOT
            // without modifying the current process environment
            var buildEnvironment = StandardBuildEnvironment.Instance;
            var originalDotNetRoot = buildEnvironment.GetEnvironmentVariable(RuntimeHostInfo.DotNetRootEnvironmentName);

            var envVars = BuildServerConnection.GetServerEnvironmentVariables(buildEnvironment);

            if (BuildServerConnection.IsBuiltinToolRunningOnCoreClr && RuntimeHostInfo.GetToolDotNetRoot(buildEnvironment, Logger.Log) is { } dotNetRoot)
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
            else if (envVars != null)
            {
                // No DOTNET_ROOT modification is needed
                var modifiedDotNetRoot = envVars.TryGetValue(RuntimeHostInfo.DotNetRootEnvironmentName, out var value) ? value : null;
                Assert.Equal(originalDotNetRoot, modifiedDotNetRoot);
            }
        }

        [Fact]
        public void GetServerEnvironmentVariables_ExcludesDotNetRootVariants()
        {
            // This test verifies that DOTNET_ROOT* variables are properly cleared and replaced
            var testEnvVars = new[] { "DOTNET_ROOT_X64", "DOTNET_ROOT_X86", "DOTNET_ROOT_ARM64", "DOTNET_ROOT(x86)" };

            // Create a test environment with DOTNET_ROOT* variants
            var testEnvironment = StandardBuildEnvironment.GetEnvironmentVariables();

            // Add test DOTNET_ROOT* variants
            foreach (var testEnvVar in testEnvVars)
            {
                testEnvironment[testEnvVar] = "test_value";
            }

            var buildEnvironment = new TestableBuildEnvironment(Path.GetTempPath(), testEnvironment);
            var envVars = BuildServerConnection.GetServerEnvironmentVariables(buildEnvironment);

            if (BuildServerConnection.IsBuiltinToolRunningOnCoreClr && RuntimeHostInfo.GetToolDotNetRoot(buildEnvironment, Logger.Log) != null)
            {
                Assert.NotNull(envVars);

                // Should set DOTNET_ROOT* variants to empty string to prevent inheritance
                foreach (var testEnvVar in testEnvVars)
                {
                    Assert.True(envVars.ContainsKey(testEnvVar), $"Environment variables should contain {testEnvVar}");
                    Assert.Equal(string.Empty, envVars[testEnvVar]);
                }
            }
        }

        private static void RunOnOtherThread(Action action)
        {
            Exception? exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            })
            {
                IsBackground = true,
            };

            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
            if (exception is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }
    }
}

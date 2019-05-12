// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Specialized;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class VBCSCompilerServerTests
    {
        public class StartupTests : VBCSCompilerServerTests
        {
            [Fact]
            [WorkItem(217709, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/217709")]
            public async Task ShadowCopyAnalyzerAssemblyLoaderMissingDirectory()
            {
                var baseDirectory = Path.Combine(Path.GetTempPath(), TestBase.GetUniqueName());
                var loader = new ShadowCopyAnalyzerAssemblyLoader(baseDirectory);
                var task = loader.DeleteLeftoverDirectoriesTask;
                await task;
                Assert.False(task.IsFaulted);
            }
        }

        public class ShutdownTests : VBCSCompilerServerTests
        {
            private static Task<int> RunShutdownAsync(string pipeName, bool waitForProcess = true, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                var appSettings = new NameValueCollection();
                return new DesktopBuildServerController(appSettings).RunShutdownAsync(pipeName, waitForProcess, timeout, cancellationToken);
            }

            [Fact]
            public async Task Standard()
            {
                using (var serverData = await ServerUtil.CreateServer())
                {
                    // Make sure the server is listening for this particular test. 
                    await serverData.ListenTask;
                    var exitCode = await RunShutdownAsync(serverData.PipeName, waitForProcess: false).ConfigureAwait(false);
                    Assert.Equal(CommonCompiler.Succeeded, exitCode);
                    await serverData.Verify(connections: 1, completed: 1);
                }
            }

            /// <summary>
            /// If there is no server running with the specified pipe name then it's not running and hence
            /// shutdown succeeded.
            /// </summary>
            /// <returns></returns>
            [Fact]
            public async Task NoServerMutex()
            {
                var pipeName = Guid.NewGuid().ToString();
                var exitCode = await RunShutdownAsync(pipeName, waitForProcess: false).ConfigureAwait(false);
                Assert.Equal(CommonCompiler.Succeeded, exitCode);
            }

            [Fact]
            [WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
            public async Task NoServerConnection()
            {
                using (var readyMre = new ManualResetEvent(initialState: false))
                using (var doneMre = new ManualResetEvent(initialState: false))
                {
                    var pipeName = Guid.NewGuid().ToString();
                    var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
                    bool created = false;
                    bool connected = false;

                    var thread = new Thread(() =>
                    {
                        using (var mutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out created))
                        using (var stream = NamedPipeUtil.CreateServer(pipeName))
                        {
                            readyMre.Set();

                            // Get a client connection and then immediately close it.  Don't give any response.
                            stream.WaitForConnection();
                            connected = true;
                            stream.Close();

                            doneMre.WaitOne();
                            mutex.ReleaseMutex();
                        }
                    });

                    // Block until the mutex and named pipe is setup.
                    thread.Start();
                    readyMre.WaitOne();

                    var exitCode = await RunShutdownAsync(pipeName, waitForProcess: false);

                    // Let the fake server exit.
                    doneMre.Set();
                    thread.Join();

                    Assert.Equal(CommonCompiler.Failed, exitCode);
                    Assert.True(connected);
                    Assert.True(created);
                }
            }

            /// <summary>
            /// Here the server doesn't respond to the shutdown request but successfully shuts down before
            /// the client can error out.
            /// </summary>
            /// <returns></returns>
            [Fact]
            [WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
            public async Task ServerShutdownsDuringProcessing()
            {
                using (var readyMre = new ManualResetEvent(initialState: false))
                using (var doneMre = new ManualResetEvent(initialState: false))
                {
                    var pipeName = Guid.NewGuid().ToString();
                    var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
                    bool created = false;
                    bool connected = false;

                    var thread = new Thread(() =>
                    {
                        using (var stream = NamedPipeUtil.CreateServer(pipeName))
                        {
                            var mutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out created);
                            readyMre.Set();

                            stream.WaitForConnection();
                            connected = true;

                            // Client is waiting for a response.  Close the mutex now.  Then close the connection 
                            // so the client gets an error.
                            mutex.ReleaseMutex();
                            mutex.Dispose();
                            stream.Close();

                            doneMre.WaitOne();
                        }
                    });

                    // Block until the mutex and named pipe is setup.
                    thread.Start();
                    readyMre.WaitOne();

                    var exitCode = await RunShutdownAsync(pipeName, waitForProcess: false);

                    // Let the fake server exit.
                    doneMre.Set();
                    thread.Join();

                    Assert.Equal(CommonCompiler.Succeeded, exitCode);
                    Assert.True(connected);
                    Assert.True(created);
                }
            }

            [Fact]
            public async Task RunServerWithLongTempPath()
            {
                string pipeName = BuildServerConnection.GetPipeNameForPathOpt(Guid.NewGuid().ToString());
                string tempPath = new string('a', 100);
                using (var serverData = await ServerUtil.CreateServer(pipeName, tempPath: tempPath))
                {
                    // Make sure the server is listening for this particular test.
                    await serverData.ListenTask;
                    var exitCode = await RunShutdownAsync(serverData.PipeName, waitForProcess: false).ConfigureAwait(false);
                    Assert.Equal(CommonCompiler.Succeeded, exitCode);
                    await serverData.Verify(connections: 1, completed: 1);
                }
            }
        }

        public class ParseCommandLineTests : VBCSCompilerServerTests
        {
            private string _pipeName;
            private bool _shutdown;

            private bool Parse(params string[] args)
            {
                return BuildServerController.ParseCommandLine(args, out _pipeName, out _shutdown);
            }

            [Fact]
            public void Nothing()
            {
                Assert.True(Parse());
                Assert.Null(_pipeName);
                Assert.False(_shutdown);
            }

            [Fact]
            public void PipeOnly()
            {
                Assert.True(Parse("-pipename:test"));
                Assert.Equal("test", _pipeName);
                Assert.False(_shutdown);
            }

            [Fact]
            public void Shutdown()
            {
                Assert.True(Parse("-shutdown"));
                Assert.Null(_pipeName);
                Assert.True(_shutdown);
            }

            [Fact]
            public void PipeAndShutdown()
            {
                Assert.True(Parse("-pipename:test", "-shutdown"));
                Assert.Equal("test", _pipeName);
                Assert.True(_shutdown);
            }

            [Fact]
            public void BadArg()
            {
                Assert.False(Parse("-invalid"));
                Assert.False(Parse("name"));
            }
        }
    }
}

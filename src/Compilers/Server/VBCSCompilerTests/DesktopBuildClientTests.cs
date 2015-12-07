// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias csc;
extern alias vbc;

using System;
using System.IO;
using Microsoft.CodeAnalysis.CommandLine;
using System.Runtime.InteropServices;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class DesktopBuildClientTests : TestBase
    {
        private sealed class ServerInfo
        {
            public readonly string ClientDirectory;
            public readonly string SdkDirectory;
            public readonly List<Task> Servers = new List<Task>();
            public int CreateCount;

            internal ServerInfo()
            {
                ClientDirectory = Path.GetDirectoryName(typeof(DesktopBuildClientTests).Assembly.Location);
                SdkDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            }

            public bool TryCreateServer(string pipeName)
            {
                CreateCount++;
                Servers.Add(CreateServerCore(pipeName));
                return true;
            }

            private Task CreateServerCore(string pipeName)
            {
                Action action = () =>
                {
                    var compilerServerHost = new DesktopCompilerServerHost(ClientDirectory, SdkDirectory);
                    var clientConnectionHost = new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
                    var mutexName = BuildProtocolConstants.GetServerMutexName(pipeName);
                    VBCSCompiler.Run(mutexName, clientConnectionHost, TimeSpan.FromSeconds(3));
                };

                var task = new Task(action, TaskCreationOptions.LongRunning);
                task.Start(TaskScheduler.Default);
                return task;
            }
        }

        private sealed class TestableDesktopBuildClient : DesktopBuildClient
        {
            private readonly string _pipeName;
            private readonly Func<string, bool> _createServerFunc;

            public TestableDesktopBuildClient(
                RequestLanguage langauge,
                CompileFunc compileFunc,
                string pipeName,
                Func<string, bool> createServerFunc) : base(langauge, compileFunc, new Mock<IAnalyzerAssemblyLoader>().Object)
            {
                _pipeName = pipeName;
                _createServerFunc = createServerFunc;
            }

            protected override string GetPipeName(string compilerExeDirectory)
            {
                return _pipeName;
            }

            protected override bool TryCreateServer(string clientDir, string pipeName)
            {
                return _createServerFunc(pipeName);
            }

            protected override int HandleResponse(BuildResponse response, List<string> arguments, BuildPaths buildPaths)
            {
                // Override the base so we don't print the compilation output to Console.Out
                return 0;
            }
        }

        private readonly string _pipeName = Guid.NewGuid().ToString("N");
        private readonly ServerInfo _serverInfo = new ServerInfo();
        private readonly TempDirectory _tempDirectory;
        private readonly BuildPaths _buildPaths;

        public DesktopBuildClientTests()
        {
            _tempDirectory = Temp.CreateDirectory();
            _buildPaths = new BuildPaths(_serverInfo.ClientDirectory, _tempDirectory.Path, _serverInfo.SdkDirectory);
        }

        public override void Dispose()
        {
            foreach (var task in _serverInfo.Servers)
            {
                task.Wait();
            }

            base.Dispose();
        }

        private TestableDesktopBuildClient CreateClient(RequestLanguage? language = null, CompileFunc compileFunc = null)
        {
            language = language ?? RequestLanguage.CSharpCompile;
            compileFunc = compileFunc ?? delegate { return 0; };
            return new TestableDesktopBuildClient(language.Value, compileFunc, _pipeName, _serverInfo.TryCreateServer);
        }

        [Fact]
        public void OnlyStartsOneServer()
        {
            var ranLocal = false;
            var client = CreateClient(compileFunc: delegate
            {
                ranLocal = true;
                throw new Exception();
            });

            for (var i = 0; i < 5; i++)
            {
                client.RunCompilation(new[] { "/shared" }, _buildPaths);
            }

            Assert.Equal(1, _serverInfo.CreateCount);
            Assert.False(ranLocal);
        }
    }
}

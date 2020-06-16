// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

extern alias InteractiveHost;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    public abstract class AbstractInteractiveHostTests : CSharpTestBase, IAsyncLifetime
    {
        private SynchronizedStringWriter _synchronizedOutput = null!;
        private SynchronizedStringWriter _synchronizedErrorOutput = null!;
        private int[] _outputReadPosition = new int[] { 0, 0 };

        internal readonly InteractiveHost Host;

        internal static readonly string HomeDir = FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        protected AbstractInteractiveHostTests()
        {
            Host = new InteractiveHost(typeof(CSharpReplServiceProvider), ".", millisecondsTimeout: -1, joinOutputWritingThreadsOnDisposal: true);

            RedirectOutput();
        }

        internal abstract InteractiveHostPlatform DefaultPlatform { get; }
        internal abstract string[] ReferenceSearchPaths { get; }
        internal abstract bool UseDefaultInitializationFile { get; }

        public async Task InitializeAsync()
        {
            var initializationFileName = UseDefaultInitializationFile ? "CSharpInteractive.rsp" : null;

            await Host.ResetAsync(InteractiveHostOptions.CreateFromDirectory(TestUtils.HostRootPath, initializationFileName, CultureInfo.InvariantCulture, DefaultPlatform));

            await Host.SetPathsAsync(ReferenceSearchPaths, new[] { HomeDir }, HomeDir);

            // assert and remove logo:
            var output = SplitLines(await ReadOutputToEnd());
            var errorOutput = await ReadErrorOutputToEnd();

            Assert.Equal("", errorOutput);

            var expectedOutput = new List<string>();
            expectedOutput.Add(string.Format(CSharpScriptingResources.LogoLine1, CommonCompiler.GetProductVersion(typeof(CSharpReplServiceProvider))));

            if (UseDefaultInitializationFile)
            {
                expectedOutput.Add(string.Format(InteractiveHostResources.Loading_context_from_0, initializationFileName));
            }

            expectedOutput.Add(InteractiveHostResources.Type_Sharphelp_for_more_information);

            AssertEx.Equal(expectedOutput, output);

            // remove logo:
            ClearOutput();
        }

        public async Task DisposeAsync()
        {
            var service = await Host.TryGetServiceAsync();
            Assert.NotNull(service);

            var process = service!.Process;

            Host.Dispose();

            // the process should be terminated
            if (process != null && !process.HasExited)
            {
                process.WaitForExit();
            }
        }

        public void RedirectOutput()
        {
            _synchronizedOutput = new SynchronizedStringWriter();
            _synchronizedErrorOutput = new SynchronizedStringWriter();
            ClearOutput();
            Host.SetOutputs(_synchronizedOutput, _synchronizedErrorOutput);
        }

        public static ImmutableArray<string> SplitLines(string text)
        {
            return ImmutableArray.Create(text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
        }

        public async Task<bool> LoadReference(string reference)
        {
            return await Execute($"#r \"{reference}\"");
        }

        public async Task<bool> Execute(string code)
        {
            var task = await Host.ExecuteAsync(code);
            return task.Success;
        }

        public Task<string> ReadErrorOutputToEnd()
        {
            return ReadOutputToEnd(isError: true);
        }

        public void ClearOutput()
        {
            _outputReadPosition = new int[] { 0, 0 };
            _synchronizedOutput.Clear();
            _synchronizedErrorOutput.Clear();
        }

        public async Task RestartHost()
        {
            ClearOutput();

            await Host.ResetAsync(InteractiveHostOptions.CreateFromDirectory(TestUtils.HostRootPath, initializationFileName: null, CultureInfo.InvariantCulture, InteractiveHostPlatform.Desktop64));
        }

        public async Task<string> ReadOutputToEnd(bool isError = false)
        {
            // writes mark to the STDOUT/STDERR pipe in the remote process:
            var remoteService = await Host.TryGetServiceAsync().ConfigureAwait(false);
            Assert.NotNull(remoteService);

            var writer = isError ? _synchronizedErrorOutput : _synchronizedOutput;
            var markPrefix = '\uFFFF';
            var mark = markPrefix + Guid.NewGuid().ToString();

            await remoteService!.JsonRpc.InvokeAsync<Task>(nameof(InteractiveHost.Service.RemoteConsoleWriteAsync), Encoding.UTF8.GetBytes(mark), isError).ConfigureAwait(false);
            while (true)
            {
                var data = writer.Prefix(mark, ref _outputReadPosition[isError ? 0 : 1]);
                if (data != null)
                {
                    return data;
                }

                await Task.Delay(10);
            }
        }

        public static (string Path, ImmutableArray<byte> Image) CompileLibrary(
            TempDirectory dir, string fileName, string assemblyName, string source, params MetadataReference[] references)
        {
            var file = dir.CreateFile(fileName);
            var compilation = CreateEmptyCompilation(
                new[] { source },
                assemblyName: assemblyName,
                references: references.Concat(new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly) }),
                options: fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? TestOptions.ReleaseExe : TestOptions.ReleaseDll);

            var image = compilation.EmitToArray();
            file.WriteAllBytes(image);

            return (file.Path, image);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [UseExportProvider]
    public class TestNFWThrows
    {
        private XunitTraceListener _logger;
        public TestNFWThrows(ITestOutputHelper output)
        {
            //var converter = new Converter(output);
            //Console.SetOut(converter);
            _logger = new XunitTraceListener(output);
        }

        [Fact]
        public async Task Throws()
        {
            Console.WriteLine("Running test");
            _logger.WriteLine("[Logger] running test");
            var localComposition = EditorTestCompositions.EditorFeatures.WithTestHostParts(Remote.Testing.TestHost.OutOfProcess);
            using var localWorkspace = new TestWorkspace(composition: localComposition);

            var clientProvider = (InProcRemoteHostClientProvider?)localWorkspace.Services.GetService<IRemoteHostClientProvider>();
            Assert.NotNull(clientProvider);

            var client = await InProcRemoteHostClient.GetTestClientAsync(localWorkspace).ConfigureAwait(false);
            var remoteWorkspace = client.TestData.WorkspaceManager.GetWorkspace();
            ServiceBase.Equals(1, 1);
            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Console.WriteLine("fail " + i);
                    _logger.Write("[Logger] Fail " + i);
                    FatalError.ReportAndCatch(new Exception("TestException"));
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}

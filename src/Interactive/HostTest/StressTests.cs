// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    public sealed class StressTests
    {
        [Fact]
        public async Task TestKill()
        {
            for (int sleep = 0; sleep < 20; sleep++)
            {
                await TestKillAfterAsync(sleep).ConfigureAwait(false);
            }
        }

        private async Task TestKillAfterAsync(int milliseconds)
        {
            using var host = new InteractiveHost(typeof(CSharpReplServiceProvider), ".", millisecondsTimeout: 1, joinOutputWritingThreadsOnDisposal: true);
            var options = InteractiveHostOptions.CreateFromDirectory(TestUtils.HostRootPath, initializationFileName: null, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, InteractiveHostPlatform.Desktop64);

            host.InteractiveHostProcessCreated += new Action<Process>(proc =>
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(milliseconds).ConfigureAwait(false);

                    try
                    {
                        proc.Kill();
                    }
                    catch
                    {
                    }
                });
            });

            await host.ResetAsync(options).ConfigureAwait(false);

            for (int j = 0; j < 10; j++)
            {
                await host.ExecuteAsync("1+1").ConfigureAwait(false);
            }
        }
    }
}

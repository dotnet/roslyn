﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

extern alias InteractiveHost;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using System.Threading.Tasks;
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    public sealed class StressTests : AbstractInteractiveHostTests
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

            await host.ResetAsync(new InteractiveHostOptions(GetInteractiveHostDirectory())).ConfigureAwait(false);

            for (int j = 0; j < 10; j++)
            {
                await host.ExecuteAsync("1+1").ConfigureAwait(false);
            }
        }
    }
}

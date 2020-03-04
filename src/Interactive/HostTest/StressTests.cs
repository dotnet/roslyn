﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
extern alias InteractiveHost;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    public sealed class StressTests : AbstractInteractiveHostTests
    {
        private readonly List<InteractiveHost> _processes = new List<InteractiveHost>();
        private readonly List<Thread> _threads = new List<Thread>();

        public override void Dispose()
        {
            try
            {
                foreach (var process in _processes)
                {
                    DisposeInteractiveHostProcess(process);
                }

                foreach (var thread in _threads)
                {
                    thread.Join();
                }
            }
            finally
            {
                base.Dispose();
            }
        }

        private InteractiveHost CreateProcess()
        {
            var p = new InteractiveHost(typeof(CSharpReplServiceProvider), ".", millisecondsTimeout: 1, joinOutputWritingThreadsOnDisposal: true);
            _processes.Add(p);
            return p;
        }

        [Fact]
        public void TestKill()
        {
            for (int sleep = 0; sleep < 20; sleep++)
            {
                TestKillAfter(sleep);
            }
        }

        private void TestKillAfter(int milliseconds)
        {
            var p = CreateProcess();

            p.InteractiveHostProcessCreated += new Action<Process>(proc =>
            {
                var t = new Thread(() =>
                {
                    Thread.Sleep(milliseconds);

                    try
                    {
                        proc.Kill();
                    }
                    catch
                    {
                    }
                });

                t.Name = "Test Thread";
                _threads.Add(t);
                t.Start();
            });

            p.ResetAsync(new InteractiveHostOptions(GetInteractiveHostDirectory())).Wait();

            for (int j = 0; j < 10; j++)
            {
                var rs = p.ExecuteAsync("1+1");
                rs.Wait(CancellationToken.None);
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.CSharp.Interactive;
using Microsoft.CodeAnalysis.Interactive;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    public sealed class StressTests : AbstractInteractiveHostTests, IDisposable
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
            var p = new InteractiveHost(typeof(CSharpReplServiceProvider), GetInteractiveHostPath(), ".", millisecondsTimeout: 1);
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

            p.ResetAsync(new InteractiveHostOptions()).Wait();

            for (int j = 0; j < 10; j++)
            {
                var rs = p.ExecuteAsync("1+1");
                rs.Wait(CancellationToken.None);
            }
        }
    }
}

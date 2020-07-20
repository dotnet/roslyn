// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace IdeBenchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            var config = new DebugInProcessConfig();
#else
            IConfig config = null;
#endif

            new BenchmarkSwitcher(typeof(Program).Assembly).Run(args, config);
        }
    }
}

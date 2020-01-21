// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

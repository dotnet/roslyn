// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace Perf
{
    /// <summary>
    /// This toolchain is designed to take an existing managed application
    /// and run it in an external process.
    /// </summary>
    internal sealed class ExternalProcessToolchain : IToolchain
    {
        public string Name => throw new System.NotImplementedException();

        public IGenerator Generator { get; }

        public IBuilder Builder { get; }

        public IExecutor Executor { get; }

        public ExternalProcessToolchain(string exePath)
        {
            Generator = new ExternalProcessGenerator(exePath);
            Builder = new ExternalProcessBuilder();
            Executor = new ExternalProcessExecutor();
        }

        public bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
            => benchmark is ExternalProcessBenchmark;
    }
}

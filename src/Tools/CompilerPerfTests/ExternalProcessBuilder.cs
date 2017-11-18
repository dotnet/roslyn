// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;

namespace Perf
{
    internal sealed class ExternalProcessBuilder : IBuilder
    {
        public BuildResult Build(
            GenerateResult generateResult,
            ILogger logger,
            Benchmark benchmark,
            IResolver resolver)
        {
            if (!(benchmark is ExternalProcessBenchmark externalProcessBenchmark))
            {
                return BuildResult.Failure(generateResult);
            }

            var exitCode = externalProcessBenchmark.BuildFunc((string)benchmark.Parameters["Commit"]);
            if (exitCode != 0)
            {
                return BuildResult.Failure(generateResult);
            }

            return BuildResult.Success(generateResult);
        }
    }
}

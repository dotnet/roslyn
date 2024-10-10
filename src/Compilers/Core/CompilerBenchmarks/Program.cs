using BenchmarkDotNet.Running;
using CompilerBenchmarks;

BenchmarkSwitcher.FromAssembly(typeof(ErrorFactsStrings).Assembly).Run(args);

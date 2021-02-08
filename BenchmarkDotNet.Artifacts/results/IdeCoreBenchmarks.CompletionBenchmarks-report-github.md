``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-9880H CPU 2.30GHz, 1 CPU, 16 logical and 8 physical cores
  [Host] : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT


```
|                Method | Mean | Error |
|---------------------- |-----:|------:|
|   RunSerialCompletion |   NA |    NA |
| RunParallelCompletion |   NA |    NA |

Benchmarks with issues:
  CompletionBenchmarks.RunSerialCompletion: DefaultJob
  CompletionBenchmarks.RunParallelCompletion: DefaultJob

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.836 (1909/November2018Update/19H2)
Intel Core i9-9880H CPU 2.30GHz, 1 CPU, 16 logical and 8 physical cores
  [Host] : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT


```
|      Method |        Job | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean | Error |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----:|------:|
| RunAnalyzer | Job-GHXGNO |              1 |     Default |   ColdStart |            1 |     Default |   NA |    NA |
| RunAnalyzer |   ShortRun |              3 |           1 |     Default |           16 |           3 |   NA |    NA |

Benchmarks with issues:
  FindReferencesBenchmarks.RunAnalyzer: Job-GHXGNO(IterationCount=1, RunStrategy=ColdStart)
  FindReferencesBenchmarks.RunAnalyzer: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)

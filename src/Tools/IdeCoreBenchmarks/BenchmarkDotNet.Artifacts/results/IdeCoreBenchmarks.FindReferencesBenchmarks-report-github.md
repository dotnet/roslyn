``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1237 (21H1/May2021Update)
Intel Core i7-9700 CPU 3.00GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.100-rc.1.21463.6
  [Host] : .NET 6.0.0 (6.0.21.45113), X64 RyuJIT

Toolchain=InProcessEmitToolchain  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
|            Method | Mean | Error |
|------------------ |-----:|------:|
| RunFindReferences |   NA |    NA |

Benchmarks with issues:
  FindReferencesBenchmarks.RunFindReferences: Job-YPYEAL(Toolchain=InProcessEmitToolchain, IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)

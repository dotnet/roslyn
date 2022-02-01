``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.22000
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host] : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT

Job=QuickJob  InvocationCount=1  IterationCount=0  
LaunchCount=1  UnrollFactor=1  WarmupCount=0  

```
|        Method | Mean | Error |
|-------------- |-----:|------:|
| RunNavigateTo |   NA |    NA |

Benchmarks with issues:
  NavigateToBenchmarks.RunNavigateTo: QuickJob(InvocationCount=1, IterationCount=0, LaunchCount=1, UnrollFactor=1, WarmupCount=0)

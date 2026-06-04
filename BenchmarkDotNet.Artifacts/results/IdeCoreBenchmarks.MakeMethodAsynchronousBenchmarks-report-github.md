```

BenchmarkDotNet v0.15.0, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.108
  [Host] : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2

InvocationCount=1  IterationCount=5  LaunchCount=1  
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=1  

```
| Method                | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|---------------------- |-----:|------:|------:|--------:|------------:|
| &#39;No event assignment&#39; |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  MakeMethodAsynchronousBenchmarks.'No event assignment': Job-IZZRWQ(InvocationCount=1, IterationCount=5, LaunchCount=1, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=1)

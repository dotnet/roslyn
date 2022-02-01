``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.22000
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host] : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT

InvocationCount=1  UnrollFactor=1  

```
|           Method | Mean | Error |
|----------------- |-----:|------:|
| ClassifyDocument |   NA |    NA |

Benchmarks with issues:
  ClassificationBenchmarks.ClassifyDocument: Job-SFGERQ(InvocationCount=1, UnrollFactor=1)

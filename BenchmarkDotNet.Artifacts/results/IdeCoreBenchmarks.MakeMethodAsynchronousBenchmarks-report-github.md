```

BenchmarkDotNet v0.15.0, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.108
  [Host] : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2

Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=5  
UnrollFactor=1  WarmupCount=1  

```
| Method                  | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0      | Gen1      | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|---------:|------:|--------:|----------:|----------:|----------:|------------:|
| &#39;No event assignment&#39;   | 370.8 ms |  95.16 ms | 24.71 ms |  1.00 |    0.08 | 4000.0000 | 3000.0000 |   74.3 MB |        1.00 |
| &#39;With event assignment&#39; | 439.0 ms | 182.97 ms | 47.52 ms |  1.19 |    0.14 | 3000.0000 | 3000.0000 |   57.8 MB |        0.78 |

``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 24.04
AMD EPYC 7763, 1 CPU, 8 logical and 4 physical cores
.NET SDK=10.0.100-rc.1.25451.107
  [Host]   : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT
  ShortRun : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
|                                 Method |      Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |----------:|----------:|----------:|-------:|------:|------:|----------:|
| &#39;Size and Timestamp Check (Fast Path)&#39; |  4.141 μs | 0.1804 μs | 0.0099 μs | 0.0229 |     - |     - |     472 B |
|          &#39;MVID Extraction (Slow Path)&#39; | 35.049 μs | 2.3985 μs | 0.1315 μs | 0.4883 |     - |     - |   8,960 B |
|     &#39;Combined Check (Fast Path First)&#39; |  4.091 μs | 0.1965 μs | 0.0108 μs | 0.0229 |     - |     - |     472 B |

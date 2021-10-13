``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i7-8700 CPU 3.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT
  Job-KWVDKZ : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT

Runtime=.NET Framework 4.7.2  Toolchain=net472  

```
|                                  Method |             Mean |          Error |         StdDev |    Gen 0 |    Gen 1 |    Gen 2 |    Allocated |
|---------------------------------------- |-----------------:|---------------:|---------------:|---------:|---------:|---------:|-------------:|
|           RunLSPSemanticTokensBenchmark |         18.02 ns |       0.157 ns |       0.131 ns |   0.0191 |        - |        - |        120 B |
| RunLSPSemanticTokensBenchmark_RazorImpl | 12,168,209.53 ns | 223,815.727 ns | 553,217.024 ns | 781.2500 | 765.6250 | 765.6250 | 43,093,728 B |

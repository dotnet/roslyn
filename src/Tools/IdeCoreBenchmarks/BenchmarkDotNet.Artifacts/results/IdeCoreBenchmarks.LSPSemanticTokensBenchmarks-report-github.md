``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i7-8700 CPU 3.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT
  Job-KWVDKZ : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT

Runtime=.NET Framework 4.7.2  Toolchain=net472  

```
|                                      Method |       Mean |     Error |    StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|-------------------------------------------- |-----------:|----------:|----------:|-----------:|----------:|----------:|----------:|
|  RunLSPSemanticTokensBenchmark_RazorImpl_10 |   1.574 ms | 0.0230 ms | 0.0264 ms |   357.4219 |  148.4375 |   42.9688 |      2 MB |
|  RunLSPSemanticTokensBenchmark_RazorImpl_20 |   3.816 ms | 0.0502 ms | 0.0470 ms |   679.6875 |  390.6250 |  113.2813 |      5 MB |
| RunLSPSemanticTokensBenchmark_RazorImpl_100 |  18.396 ms | 0.3663 ms | 0.7315 ms |  2968.7500 | 1125.0000 |  500.0000 |     24 MB |
| RunLSPSemanticTokensBenchmark_RazorImpl_250 |  44.935 ms | 0.8936 ms | 1.9987 ms |  6833.3333 | 2500.0000 |  833.3333 |     57 MB |
| RunLSPSemanticTokensBenchmark_RazorImpl_500 | 122.697 ms | 2.4280 ms | 5.1743 ms | 18500.0000 | 6750.0000 | 1500.0000 |    138 MB |

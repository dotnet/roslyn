``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i7-8700 CPU 3.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT
  Job-KWVDKZ : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT

Runtime=.NET Framework 4.7.2  Toolchain=net472  

```
|                                      Method |       Mean |     Error |     StdDev |     Median |       Gen 0 |       Gen 1 |     Gen 2 | Allocated |
|-------------------------------------------- |-----------:|----------:|-----------:|-----------:|------------:|------------:|----------:|----------:|
|       RunLSPSemanticTokensBenchmarkAsync_10 |   7.985 ms | 0.1555 ms |  0.1454 ms |   8.005 ms |   5210.9375 |   1914.0625 |  164.0625 |     32 MB |
|       RunLSPSemanticTokensBenchmarkAsync_20 |  13.878 ms | 0.2695 ms |  0.4427 ms |  13.974 ms |  10093.7500 |   3859.3750 |  250.0000 |     62 MB |
|      RunLSPSemanticTokensBenchmarkAsync_100 |  63.833 ms | 1.2761 ms |  2.5778 ms |  63.223 ms |  49888.8889 |  22000.0000 | 1111.1111 |    310 MB |
|      RunLSPSemanticTokensBenchmarkAsync_250 | 128.247 ms | 2.5580 ms |  7.2148 ms | 126.326 ms | 122500.0000 |  53750.0000 | 2000.0000 |    759 MB |
|      RunLSPSemanticTokensBenchmarkAsync_500 | 297.534 ms | 5.8520 ms | 13.0889 ms | 292.530 ms | 292000.0000 | 131500.0000 | 2500.0000 |  1,785 MB |
|  RunLSPSemanticTokensBenchmark_RazorImpl_10 |   2.225 ms | 0.0441 ms |  0.0471 ms |   2.201 ms |    351.5625 |    136.7188 |   35.1563 |      2 MB |
|  RunLSPSemanticTokensBenchmark_RazorImpl_20 |   4.429 ms | 0.0852 ms |  0.1046 ms |   4.421 ms |    687.5000 |    375.0000 |  109.3750 |      5 MB |
| RunLSPSemanticTokensBenchmark_RazorImpl_100 |  19.487 ms | 0.3163 ms |  0.2804 ms |  19.504 ms |   2968.7500 |   1093.7500 |  437.5000 |     22 MB |
| RunLSPSemanticTokensBenchmark_RazorImpl_250 |  44.846 ms | 0.8845 ms |  1.1186 ms |  44.922 ms |   6727.2727 |   1636.3636 |  545.4545 |     58 MB |
| RunLSPSemanticTokensBenchmark_RazorImpl_500 | 113.969 ms | 2.2534 ms |  2.9301 ms | 114.253 ms |  19200.0000 |   5600.0000 | 1600.0000 |    139 MB |

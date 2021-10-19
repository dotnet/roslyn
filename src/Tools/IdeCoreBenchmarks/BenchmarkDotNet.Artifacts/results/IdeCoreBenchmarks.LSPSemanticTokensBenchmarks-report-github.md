``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i7-8700 CPU 3.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT
  Job-KWVDKZ : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT

Runtime=.NET Framework 4.7.2  Toolchain=net472  

```
|                                  Method |      Mean |     Error |    StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------------------------------- |----------:|----------:|----------:|-----------:|----------:|----------:|----------:|
|       RunLSPSemanticTokensBenchmark_10k |  1.947 ms | 0.0134 ms | 0.0118 ms |   355.4688 |  160.1563 |   56.6406 |      2 MB |
|       RunLSPSemanticTokensBenchmark_20k |  3.954 ms | 0.0844 ms | 0.2474 ms |   656.2500 |  367.1875 |  121.0938 |      5 MB |
|      RunLSPSemanticTokensBenchmark_100k | 16.261 ms | 0.3094 ms | 0.3913 ms |  2750.0000 | 1093.7500 |  468.7500 |     22 MB |
|      RunLSPSemanticTokensBenchmark_250k | 36.942 ms | 0.7235 ms | 0.6767 ms |  6076.9231 | 1692.3077 |  538.4615 |     51 MB |
| RunLSPSemanticTokensBenchmark_AllTokens | 99.239 ms | 1.9669 ms | 2.5576 ms | 17800.0000 | 6000.0000 | 2000.0000 |    139 MB |

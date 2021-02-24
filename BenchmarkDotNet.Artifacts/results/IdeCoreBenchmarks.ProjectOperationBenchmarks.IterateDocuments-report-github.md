``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen Threadripper 3960X, 1 CPU, 48 logical and 24 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4300.0), X64 RyuJIT


```
|                    Method | DocumentCount |            Mean |         Error |        StdDev |          Median |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |----------------:|--------------:|--------------:|----------------:|-------:|-------:|------:|----------:|
|       **Project.DocumentIds** |             **0** |        **23.34 ns** |      **0.494 ns** |      **0.891 ns** |        **22.91 ns** |      **-** |      **-** |     **-** |         **-** |
|         Project.Documents |             0 |        77.22 ns |      0.537 ns |      0.502 ns |        77.31 ns | 0.0772 |      - |     - |     128 B |
| Solution.WithDocumentText |             0 |        14.60 ns |      0.324 ns |      0.690 ns |        14.57 ns |      - |      - |     - |         - |
|       **Project.DocumentIds** |           **100** |     **8,159.41 ns** |     **10.113 ns** |      **8.965 ns** |     **8,157.79 ns** | **0.0305** |      **-** |     **-** |      **72 B** |
|         Project.Documents |           100 |    16,356.60 ns |     97.365 ns |     91.076 ns |    16,382.64 ns | 0.0916 |      - |     - |     201 B |
| Solution.WithDocumentText |           100 |    14,436.90 ns |    355.846 ns |  1,015.248 ns |    14,392.68 ns | 0.8240 | 0.2136 |     - |    5285 B |
|       **Project.DocumentIds** |         **10000** |   **863,988.25 ns** |  **1,297.511 ns** |  **1,213.693 ns** |   **863,625.88 ns** |      **-** |      **-** |     **-** |      **80 B** |
|         Project.Documents |         10000 | 1,861,933.16 ns | 11,538.506 ns | 10,793.125 ns | 1,859,064.65 ns |      - |      - |     - |     208 B |
| Solution.WithDocumentText |         10000 |    14,088.55 ns |    281.041 ns |    453.828 ns |    14,003.49 ns | 0.8698 | 0.2289 |     - |    5539 B |

```

BenchmarkDotNet v0.15.0, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.108
  [Host] : .NET 10.0.9 (10.0.926.27113), X64 RyuJIT AVX2

Toolchain=InProcessEmitToolchain  

```
| Method                  | Scenario   | Mean            | Error         | StdDev         | Median          | Ratio         | RatioSD    | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |----------------:|--------------:|---------------:|----------------:|--------------:|-----------:|-------:|-------:|----------:|------------:|
| **NoDetection**             | **CommonName** |       **0.0014 ns** |     **0.0027 ns** |      **0.0024 ns** |       **0.0000 ns** |             **?** |          **?** |      **-** |      **-** |         **-** |           **?** |
| FarBasedDetection       | CommonName | 345,674.9681 ns | 6,876.3285 ns | 16,475.2451 ns | 343,025.3936 ns |             ? |          ? | 9.7656 |      - |  163405 B |           ? |
| SignatureBasedDetection | CommonName |      53.0268 ns |     0.5400 ns |      0.4510 ns |      53.1092 ns |             ? |          ? | 0.0048 |      - |      80 B |           ? |
|                         |            |                 |               |                |                 |               |            |        |        |           |             |
| **NoDetection**             | **UniqueName** |       **0.0223 ns** |     **0.0008 ns** |      **0.0007 ns** |       **0.0219 ns** |          **1.00** |       **0.05** |      **-** |      **-** |         **-** |          **NA** |
| FarBasedDetection       | UniqueName | 357,225.5225 ns | 6,890.1047 ns |  6,445.0082 ns | 359,513.1511 ns | 16,067,848.53 | 579,718.09 | 9.7656 | 0.4883 |  168033 B |          NA |
| SignatureBasedDetection | UniqueName |      53.0116 ns |     0.1123 ns |      0.0877 ns |      53.0120 ns |      2,384.44 |      75.40 | 0.0048 |      - |      80 B |          NA |

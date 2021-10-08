``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1237 (21H1/May2021Update)
Intel Core i7-9700 CPU 3.00GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.100-rc.1.21463.6
  [Host]    : .NET 6.0.0 (6.0.21.45113), X64 RyuJIT
  RyuJitX64 : .NET 6.0.0 (6.0.21.45113), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|      Method |        Job |              Toolchain | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount |         AnalyzerName |    Mean |    Error |   StdDev |       Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|------------ |----------- |----------------------- |--------------- |------------ |------------ |------------- |------------ |--------------------- |--------:|---------:|---------:|------------:|-----------:|----------:|----------:|
| RunAnalyzer | Job-OPFYMK | InProcessEmitToolchain |              1 |           1 |   ColdStart |            1 |           1 | CShar(...)lyzer [33] | 8.873 s |       NA | 0.0000 s | 213000.0000 | 60000.0000 | 1000.0000 |      1 GB |
| RunAnalyzer |  RyuJitX64 |                Default |        Default |     Default |     Default |           16 |     Default | CShar(...)lyzer [33] | 3.944 s | 0.0514 s | 0.0481 s | 213000.0000 | 60000.0000 | 1000.0000 |      1 GB |

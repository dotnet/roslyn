``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i7-9700 CPU 3.00GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.100-rc.1.21463.6
  [Host] : .NET 6.0.0 (6.0.21.45113), X64 RyuJIT
  Dry    : .NET 6.0.0 (6.0.21.45113), X64 RyuJIT

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
|           Method |    Mean | Error |        Gen 0 |        Gen 1 |     Gen 2 | Allocated |
|----------------- |--------:|------:|-------------:|-------------:|----------:|----------:|
| ClassifyDocument | 270.0 s |    NA | 9682000.0000 | 2649000.0000 | 7000.0000 |     57 GB |

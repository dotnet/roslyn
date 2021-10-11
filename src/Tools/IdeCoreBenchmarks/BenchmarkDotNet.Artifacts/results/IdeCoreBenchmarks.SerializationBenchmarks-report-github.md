``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1237 (21H1/May2021Update)
Intel Core i7-9700 CPU 3.00GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.100-rc.1.21463.6
  [Host] : .NET 6.0.0 (6.0.21.45113), X64 RyuJIT

Toolchain=InProcessEmitToolchain  InvocationCount=1  UnrollFactor=1  

```
|                Method |       Mean |     Error |    StdDev |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------------- |-----------:|----------:|----------:|----------:|----------:|----------:|----------:|
|   SerializeSyntaxNode | 217.111 ms | 1.6006 ms | 1.4972 ms | 6000.0000 | 3000.0000 | 1000.0000 | 68,723 KB |
| DeserializeSyntaxNode |   1.568 ms | 0.0037 ms | 0.0029 ms |         - |         - |         - |    833 KB |

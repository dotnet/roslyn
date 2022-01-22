The C# compiler has a number of different benchmarks to measure regressions and test performance changes.

The tests are stored in the https://github.com/dotnet/performance repo. Roslyn compiler tests are https://github.com/dotnet/performance/tree/main/src/benchmarks/real-world/Roslyn.

To run the tests, you need an installation of the `dotnet` CLI, potentially with preview SDK, which are available at https://github.com/dotnet/core-sdk.

## Running the tests

To run the benchmarks,

1. Clone the dotnet/performance repo 
2. Navigate to the Roslyn directory
3. Run `dotnet run -c Release -f netcoreapp3.0` (replace `netcoreapp3.0` with whichever target framework is interesting).

You'll see results that look like the following:

```
BenchmarkDotNet=v0.11.5.1159-nightly, OS=Windows 10.0.18362
AMD Ryzen 7 1800X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.100-alpha1-014713
  [Host]     : .NET Core 3.0.0-rc1-19456-20 (CoreCLR 4.700.19.45506, CoreFX 4.700.19.45604), X64 RyuJIT
  Job-OPYEPF : .NET Core 3.0.0-rc1-19456-20 (CoreCLR 4.700.19.45506, CoreFX 4.700.19.45604), X64 RyuJIT
  Job-NVEWKL : .NET Core 3.0.0-rc1-19456-20 (CoreCLR 4.700.19.45506, CoreFX 4.700.19.45604), X64 RyuJIT

MaxRelativeError=0.01  InvocationCount=1

|                      Method | UnrollFactor |        Mean |    Error |   StdDev |      Median |         Min |         Max |       Gen 0 |      Gen 1 |     Gen 2 |  Allocated |
|---------------------------- |------------- |------------:|---------:|---------:|------------:|------------:|------------:|------------:|-----------:|----------:|-----------:|
|       CompileMethodsAndEmit |           16 |  6,374.0 ms | 29.41 ms | 26.07 ms |  6,379.8 ms |  6,317.4 ms |  6,404.8 ms |  82000.0000 | 22000.0000 |         - |  498.15 MB |
|           SerializeMetadata |           16 |    422.5 ms |  3.77 ms |  3.34 ms |    422.6 ms |    417.2 ms |    429.5 ms |   3000.0000 |  1000.0000 |         - |   33.39 MB |
|              GetDiagnostics |            1 |  4,807.7 ms | 16.78 ms | 15.70 ms |  4,802.0 ms |  4,784.7 ms |  4,838.0 ms |  78000.0000 | 20000.0000 |         - |  381.44 MB |
| GetDiagnosticsWithAnalyzers |            1 | 17,985.0 ms | 78.74 ms | 73.65 ms | 17,979.5 ms | 17,892.6 ms | 18,122.8 ms | 232000.0000 | 63000.0000 | 2000.0000 | 1397.13 MB |
```

The above shows a summary of the performance metrics for all the benchmarks in the `StageBenchmarks` class. The tests in this class are meant to measure the performance of various compiler stages, like binding, lowering, and serializing metadata. Each of the tests is run with parallelism disabled to get more reliable results, so the results are not necessarily indicative of the relative wall-clock performance between the test cases, but they are useful for seeing the performance of each of the stages relative to themselves over time.

For instance, if you have some change that you think will improve or harm binding performance, looking at the results from GetDiagnostics should tell you some of the absolute difference in instructions that will be executed.

## Measuring Roslyn changes

Once you get numbers, you may want to see if they make a difference. By default, the benchmarks in dotnet/performance always run against published NuGet builds of Roslyn. If you want to measure against local changes, you'll need to modify the benchmark slightly.

1. Make sure you have a copy of both dotnet/performance and dotnet/roslyn cloned to your machine

2. Comment out/remove the NuGet reference from the CompilerBenchmarks.csproj file: https://github.com/dotnet/performance/blob/117423c2109c1022c7b51ba7943832b5907926fc/src/benchmarks/real-world/Roslyn/CompilerBenchmarks.csproj#L18

3. Add a reference to your local copy of the CSharpCodeAnalysis project as follows: `<ProjectReference Include="<path-to-Roslyn>/src/Compilers/CSharp/Portable/Microsoft.CodeAnalysis.CSharp.csproj" />`

4. Run tests

You'll now have results specific to the Roslyn changes on your local machine. By changing branches or using `git stash` you should now be able to run the benchmark twice, with and without your changes. Determining whether or not your changes are significant is out of scope of this document, but here are some rules of thumb:

Take the mean of your benchmark and add/subtract the "Error" field. This is your 99.9% confidence interval, meaning that there is a 99.9% chance that the *real* mean of the benchmark is within this interval. Now do the same for the second benchmark. If those intervals do not intersect, you can say with 99.9% confidence that the means of those two benchmarks are different. Note, even if they *do* intersect, the measurements may *still* be significantly different, but that would require a more complicated test.

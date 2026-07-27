// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Roslyn.LanguageServer.Protocol;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ParsedUriBenchmarks
{
    private readonly string _uriString = "file:///C:/Users/username/source/repos/roslyn/artifacts/Generated%20Files/Program.cs?line=100#fragment";
    private readonly string _filePath = @"C:\Users\username\source\repos\roslyn\artifacts\Generated Files\Program.cs";
    private readonly Uri _systemUri;
    private readonly ParsedUri _parsedUri;
    private Uri? _systemUriResult;
    private ParsedUri _parsedUriResult;

    public ParsedUriBenchmarks()
    {
        _systemUri = new Uri(_uriString);
        _parsedUri = ParsedUri.Parse(_uriString);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parse")]
    public void CreateSystemUriFromUri()
        => _systemUriResult = new(_uriString);

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public void CreateParsedUriFromParse()
        => _parsedUriResult = ParsedUri.Parse(_uriString);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("File")]
    public void CreateSystemUriFromFile()
        => _systemUriResult = new(_filePath);

    [Benchmark]
    [BenchmarkCategory("File")]
    public void CreateParsedUriFromFile()
        => _parsedUriResult = ParsedUri.File(_filePath);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Format")]
    public string FormatSystemUri()
        => _systemUri.ToString();

    [Benchmark]
    [BenchmarkCategory("Format")]
    public string FormatParsedUri()
        => _parsedUri.ToString();
}

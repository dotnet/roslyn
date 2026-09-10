// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.LanguageServer;
using Roslyn.LanguageServer.Protocol;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ParsedUriBenchmarks
{
    private readonly string _uriString = "file:///C:/Users/username/source/repos/roslyn/artifacts/Generated%20Files/Program.cs?line=100#fragment";
    private readonly string _filePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? @"C:\Users\username\source\repos\roslyn\artifacts\Generated Files\Program.cs"
        : "/Users/username/source/repos/roslyn/artifacts/Generated Files/Program.cs";
    private readonly Uri _systemUri;
    private readonly ParsedUri _parsedUri;
    private Uri? _systemUriResult;
    private ParsedUri? _parsedUriResult;
    private DocumentUri? _documentUriResult;

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
    [BenchmarkCategory("FileAndFormat")]
    public string CreateAndFormatSystemUriFromFile()
        => new Uri(_filePath).ToString();

    [Benchmark]
    [BenchmarkCategory("FileAndFormat")]
    public string CreateAndFormatParsedUriFromFile()
        => ParsedUri.File(_filePath).ToString();

#pragma warning disable CS0618 // Benchmarking the previous System.Uri-backed implementation.
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocumentUri")]
    public void CreateSystemDocumentUriFromFile()
        => _documentUriResult = new DocumentUri(new Uri(_filePath));
#pragma warning restore CS0618

    [Benchmark]
    [BenchmarkCategory("DocumentUri")]
    public void CreateParsedDocumentUriFromFile()
        => _documentUriResult = ProtocolConversions.CreateAbsoluteDocumentUri(_filePath);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Format")]
    public string FormatSystemUri()
        => _systemUri.ToString();

    [Benchmark]
    [BenchmarkCategory("Format")]
    public string FormatParsedUri()
        => _parsedUri.ToString();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ParseAndFormat")]
    public string CreateAndFormatSystemUriFromUri()
        => new Uri(_uriString).ToString();

    [Benchmark]
    [BenchmarkCategory("ParseAndFormat")]
    public string CreateAndFormatParsedUriFromParse()
        => ParsedUri.Parse(_uriString).ToString();
}

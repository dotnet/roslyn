// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class GlobalQualifiedTypeNameRewriterBenchmark
{
    private readonly GlobalQualifiedTypeNameRewriter _rewriter = new(["TItem"]);

    [Params(
        "TItem",
        "System.String",
        "System.Collections.Generic.Dictionary<System.String, TItem>",
        "global::System.Collections.Generic.Dictionary<System.String, System.Collections.Generic.List<TItem>>")]
    public string TypeName { get; set; } = null!;

    [Benchmark]
    public string Rewrite() => _rewriter.Rewrite(TypeName);
}

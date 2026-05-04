// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

internal static class ParseOptionsExtensions
{
    public static bool UseRoslynTokenizer(this ParseOptions parseOptions)
        => parseOptions.Features.TryGetValue("use-roslyn-tokenizer", out var useRoslynTokenizerValue) &&
           string.Equals(useRoslynTokenizerValue, "true", StringComparison.OrdinalIgnoreCase);
}

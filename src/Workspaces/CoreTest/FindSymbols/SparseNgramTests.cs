// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FindSymbols;

public sealed class SparseNgramTests
{
    private static HashSet<string> GetAllNgrams(string text)
    {
        using var results = TemporaryArray<(int start, int length)>.Empty;
        SparseNgramGenerator.BuildAllNgrams(text, ref results.AsRef());

        var set = new HashSet<string>();
        foreach (var (start, length) in results)
            set.Add(text.Substring(start, length));

        return set;
    }

    private static HashSet<string> GetCoveringNgrams(string text)
    {
        using var results = TemporaryArray<(int start, int length)>.Empty;
        SparseNgramGenerator.BuildCoveringNgrams(text, ref results.AsRef());

        var set = new HashSet<string>();
        foreach (var (start, length) in results)
            set.Add(text.Substring(start, length));

        return set;
    }

    #region BuildAllNgrams — matches reference implementation

    [Fact]
    public void BuildAll_TwoChars_NoNgrams()
    {
        var ngrams = GetAllNgrams("he");
        Assert.Empty(ngrams);
    }

    [Fact]
    public void BuildAll_ThreeChars_SingleTrigram()
    {
        var ngrams = GetAllNgrams("hel");
        Assert.Single(ngrams);
        Assert.Contains("hel", ngrams);
    }

    [Fact]
    public void BuildAll_FourChars()
    {
        // Reference: BuildAllNgrams("hell") -> {"hel", "ell"}
        var ngrams = GetAllNgrams("hell");
        Assert.Contains("hel", ngrams);
        Assert.Contains("ell", ngrams);
    }

    [Fact]
    public void BuildAll_HelloWorld_MatchesReference()
    {
        // Reference: BuildAllNgrams("hello world") produces:
        //   {"hel", "ell", "llo", "lo ", "o w", "lo w", " wo", "lo wo", "wor", "orl", "worl", "rld"}
        var ngrams = GetAllNgrams("hello world");
        Assert.Contains("hel", ngrams);
        Assert.Contains("ell", ngrams);
        Assert.Contains("llo", ngrams);
        Assert.Contains("lo ", ngrams);
        Assert.Contains("o w", ngrams);
        Assert.Contains("lo w", ngrams);
        Assert.Contains(" wo", ngrams);
        Assert.Contains("lo wo", ngrams);
        Assert.Contains("wor", ngrams);
        Assert.Contains("orl", ngrams);
        Assert.Contains("worl", ngrams);
        Assert.Contains("rld", ngrams);
    }

    [Fact]
    public void BuildAll_Chester_MatchesReference()
    {
        // Reference: BuildAllNgrams("chester ") produces:
        //   {"che", "hes", "ches", "est", "chest", "ste", "ter", "ster", "er "}
        var ngrams = GetAllNgrams("chester ");
        Assert.Contains("che", ngrams);
        Assert.Contains("hes", ngrams);
        Assert.Contains("ches", ngrams);
        Assert.Contains("est", ngrams);
        Assert.Contains("chest", ngrams);
        Assert.Contains("ste", ngrams);
        Assert.Contains("ter", ngrams);
        Assert.Contains("ster", ngrams);
        Assert.Contains("er ", ngrams);
    }

    [Fact]
    public void BuildAll_ForLoop_MatchesReference()
    {
        // Reference: BuildAllNgrams("for(int i=42") produces:
        //   {"for", "or(", "for(", "r(i", "for(i", "(in", "int", "(int", "nt ",
        //    "t i", " i=", "t i=", "i=4", "t i=4", "nt i=4", "(int i=4", "=42"}
        var ngrams = GetAllNgrams("for(int i=42");
        Assert.Contains("for", ngrams);
        Assert.Contains("or(", ngrams);
        Assert.Contains("for(", ngrams);
        Assert.Contains("r(i", ngrams);
        Assert.Contains("for(i", ngrams);
        Assert.Contains("(in", ngrams);
        Assert.Contains("int", ngrams);
        Assert.Contains("(int", ngrams);
        Assert.Contains("nt ", ngrams);
        Assert.Contains("t i", ngrams);
        Assert.Contains(" i=", ngrams);
        Assert.Contains("t i=", ngrams);
        Assert.Contains("i=4", ngrams);
        Assert.Contains("t i=4", ngrams);
        Assert.Contains("nt i=4", ngrams);
        Assert.Contains("(int i=4", ngrams);
        Assert.Contains("=42", ngrams);
    }

    [Fact]
    public void BuildAll_BoundedSize()
    {
        // The algorithm produces at most 2n-2 n-grams for a string of length n.
        var text = "abcdefghijklmnop";
        using var results = TemporaryArray<(int start, int length)>.Empty;
        SparseNgramGenerator.BuildAllNgrams(text, ref results.AsRef());
        Assert.True(results.Count <= 2 * text.Length - 2,
            $"Expected at most {2 * text.Length - 2} n-grams, got {results.Count}");
    }

    [Fact]
    public void BuildAll_AllNgramsAtLeastThreeChars()
    {
        var ngrams = GetAllNgrams("abcdefghij");
        Assert.DoesNotContain(ngrams, g => g.Length < SparseNgramGenerator.MinNgramLength);
    }

    #endregion

    #region BuildAllNgrams — negative

    [Fact]
    public void BuildAll_EmptyString_NoNgrams()
    {
        Assert.Empty(GetAllNgrams(""));
    }

    [Fact]
    public void BuildAll_OneChar_NoNgrams()
    {
        Assert.Empty(GetAllNgrams("a"));
    }

    [Fact]
    public void BuildAll_TwoChars_NoNgrams2()
    {
        Assert.Empty(GetAllNgrams("ab"));
    }

    #endregion

    #region BuildCoveringNgrams — matches reference implementation

    [Fact]
    public void BuildCovering_TwoChars_NoNgrams()
    {
        Assert.Empty(GetCoveringNgrams("he"));
    }

    [Fact]
    public void BuildCovering_ThreeChars_SingleTrigram()
    {
        var ngrams = GetCoveringNgrams("hel");
        Assert.Single(ngrams);
        Assert.Contains("hel", ngrams);
    }

    [Fact]
    public void BuildCovering_FourChars()
    {
        // Reference: BuildCoveringNgrams("hell") -> {"hel", "ell"}
        var ngrams = GetCoveringNgrams("hell");
        Assert.Contains("hel", ngrams);
        Assert.Contains("ell", ngrams);
    }

    [Fact]
    public void BuildCovering_HelloWorld_MatchesReference()
    {
        // Reference: BuildCoveringNgrams("hello world") produces:
        //   {"hel", "ell", "llo", "rld", "worl", "lo wo"}
        var ngrams = GetCoveringNgrams("hello world");
        Assert.Contains("hel", ngrams);
        Assert.Contains("ell", ngrams);
        Assert.Contains("llo", ngrams);
        Assert.Contains("rld", ngrams);
        Assert.Contains("worl", ngrams);
        Assert.Contains("lo wo", ngrams);
    }

    [Fact]
    public void BuildCovering_ChesterSpace_MatchesReference()
    {
        // Reference: BuildCoveringNgrams("chester ") -> {"chest", "ster", "er "}
        var ngrams = GetCoveringNgrams("chester ");
        Assert.Contains("chest", ngrams);
        Assert.Contains("ster", ngrams);
        Assert.Contains("er ", ngrams);
    }

    [Fact]
    public void BuildCovering_Chester_MatchesReference()
    {
        // Reference: BuildCoveringNgrams("chester") -> {"chest", "ster"}
        var ngrams = GetCoveringNgrams("chester");
        Assert.Contains("chest", ngrams);
        Assert.Contains("ster", ngrams);
    }

    [Fact]
    public void BuildCovering_ForLoop_MatchesReference()
    {
        // Reference: BuildCoveringNgrams("for(int i=42") -> {"for(i", "(int i=4", "=42"}
        var ngrams = GetCoveringNgrams("for(int i=42");
        Assert.Contains("for(i", ngrams);
        Assert.Contains("(int i=4", ngrams);
        Assert.Contains("=42", ngrams);
    }

    [Fact]
    public void BuildCovering_IsSubsetOfBuildAll()
    {
        var text = "hello world";
        var all = GetAllNgrams(text);
        var covering = GetCoveringNgrams(text);
        Assert.True(covering.IsSubsetOf(all),
            $"Covering n-grams should be a subset of all n-grams. Extra: {string.Join(", ", covering.Except(all))}");
    }

    [Fact]
    public void BuildCovering_BoundedSize()
    {
        // The covering set produces at most n-2 n-grams.
        var text = "abcdefghijklmnop";
        using var results = TemporaryArray<(int start, int length)>.Empty;
        SparseNgramGenerator.BuildCoveringNgrams(text, ref results.AsRef());
        Assert.True(results.Count <= text.Length - 2,
            $"Expected at most {text.Length - 2} covering n-grams, got {results.Count}");
    }

    #endregion

    #region CoveringNgramsProbablyContained

    [Fact]
    public void CoveringCheck_AllPresent_ReturnsTrue()
    {
        var filter = BuildFilterForText("readline");
        Assert.True(SparseNgramGenerator.CoveringNgramsProbablyContained("readline", filter));
    }

    [Fact]
    public void CoveringCheck_SubstringPresent_ReturnsTrue()
    {
        var filter = BuildFilterForText("readline");
        Assert.True(SparseNgramGenerator.CoveringNgramsProbablyContained("read", filter));
    }

    [Fact]
    public void CoveringCheck_DifferentText_ReturnsFalse()
    {
        var filter = BuildFilterForText("readline");
        Assert.False(SparseNgramGenerator.CoveringNgramsProbablyContained("xyzwvq", filter));
    }

    [Fact]
    public void CoveringCheck_ShortText_BelowMinLength_ReturnsTrue()
    {
        var filter = BuildFilterForText("readline");
        Assert.True(SparseNgramGenerator.CoveringNgramsProbablyContained("re", filter));
    }

    [Fact]
    public void CoveringCheck_PartialOverlap_ReturnsFalse()
    {
        var filter = BuildFilterForText("goobar");
        Assert.False(SparseNgramGenerator.CoveringNgramsProbablyContained("goobaz", filter));
    }

    [Fact]
    public void CoveringCheck_CaseSensitive_DifferentCase_ReturnsFalse()
    {
        var filter = BuildFilterForText("readline");
        Assert.False(SparseNgramGenerator.CoveringNgramsProbablyContained("READLINE", filter));
    }

    #endregion

    private static BloomFilter BuildFilterForText(string text)
    {
        using var ngrams = TemporaryArray<(int start, int length)>.Empty;
        SparseNgramGenerator.BuildAllNgrams(text, ref ngrams.AsRef());

        var strings = new HashSet<string>();
        foreach (var (start, length) in ngrams)
            strings.Add(text.Substring(start, length));

        return new BloomFilter(0.01, isCaseSensitive: true, strings);
    }
}

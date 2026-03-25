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

    #region BuildAllNgrams — structural properties

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
    public void BuildAll_FourChars_ProducesOverlap()
    {
        // Four chars always produces exactly 2 trigrams (the only two possible).
        var ngrams = GetAllNgrams("hell");
        Assert.Contains("hel", ngrams);
        Assert.Contains("ell", ngrams);
    }

    [Fact]
    public void BuildAll_HelloWorld_ProducesNonEmptySet()
    {
        var ngrams = GetAllNgrams("hello world");
        Assert.NotEmpty(ngrams);
        Assert.DoesNotContain(ngrams, g => g.Length < SparseNgramGenerator.MinNgramLength);
    }

    [Fact]
    public void BuildAll_Chester_ProducesNonEmptySet()
    {
        var ngrams = GetAllNgrams("chester ");
        Assert.NotEmpty(ngrams);
        Assert.DoesNotContain(ngrams, g => g.Length < SparseNgramGenerator.MinNgramLength);
    }

    [Fact]
    public void BuildAll_AllNgramsAreSubstringsOfInput()
    {
        var text = "cancellationtoken";
        var ngrams = GetAllNgrams(text);
        foreach (var ngram in ngrams)
            Assert.Contains(ngram, text);
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

    [Fact]
    public void BuildAll_Deterministic()
    {
        // Same input always produces the same output.
        var ngrams1 = GetAllNgrams("stringbuilder");
        var ngrams2 = GetAllNgrams("stringbuilder");
        Assert.True(ngrams1.SetEquals(ngrams2));
    }

    [Fact]
    public void BuildAll_RealIdentifiers_ProduceReasonableNgramCounts()
    {
        // Verify the algorithm produces non-trivial n-gram sets for real identifiers.
        var identifiers = new[] { "getvalue", "tostring", "readline", "stringbuilder",
            "cancellationtoken", "iasyncenumerable" };

        foreach (var id in identifiers)
        {
            var ngrams = GetAllNgrams(id);
            Assert.NotEmpty(ngrams);
            Assert.True(ngrams.Count >= 2,
                $"Identifier '{id}' should produce at least 2 n-grams, got {ngrams.Count}");
        }
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

    #region BuildCoveringNgrams — structural properties

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
    public void BuildCovering_FourChars_ProducesOverlap()
    {
        var ngrams = GetCoveringNgrams("hell");
        Assert.Contains("hel", ngrams);
        Assert.Contains("ell", ngrams);
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
    public void BuildCovering_IsSubsetOfBuildAll_RealIdentifiers()
    {
        var identifiers = new[] { "getvalue", "tostring", "readline", "stringbuilder",
            "cancellationtoken", "iasyncenumerable" };

        foreach (var id in identifiers)
        {
            var all = GetAllNgrams(id);
            var covering = GetCoveringNgrams(id);
            Assert.True(covering.IsSubsetOf(all),
                $"Covering of '{id}' should be subset of all. Extra: {string.Join(", ", covering.Except(all))}");
        }
    }

    [Fact]
    public void BuildCovering_FewerOrEqualToBuildAll()
    {
        var identifiers = new[] { "getvalue", "stringbuilder", "cancellationtoken" };
        foreach (var id in identifiers)
        {
            var all = GetAllNgrams(id);
            var covering = GetCoveringNgrams(id);
            Assert.True(covering.Count <= all.Count,
                $"Covering of '{id}' ({covering.Count}) should be <= all ({all.Count})");
        }
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

    [Fact]
    public void BuildCovering_ProducesShorterSetThanAll_ForLongIdentifiers()
    {
        // For long identifiers, covering should be strictly smaller than all.
        var id = "cancellationtoken";
        var all = GetAllNgrams(id);
        var covering = GetCoveringNgrams(id);
        Assert.True(covering.Count < all.Count,
            $"Covering of '{id}' ({covering.Count}) should be < all ({all.Count})");
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

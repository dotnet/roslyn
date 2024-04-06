// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Roslyn.Utilities;

internal struct WordSimilarityChecker : IDisposable
{
    private readonly struct CacheResult(string candidate, bool areSimilar, double similarityWeight)
    {
        public readonly string CandidateText = candidate;
        public readonly bool AreSimilar = areSimilar;
        public readonly double SimilarityWeight = similarityWeight;
    }

    // Cache the result of the last call to AreSimilar.  We'll often be called with the same
    // value multiple times in a row, so we can avoid expensive computation by returning the
    // same value immediately.
    private CacheResult _lastAreSimilarResult;

    private readonly string _source;
    private readonly EditDistance _editDistance;
    private readonly int _threshold;

    /// <summary>
    /// Whether or words should be considered similar if one is contained within the other
    /// (regardless of edit distance).  For example if is true then IService would be considered
    /// similar to IServiceFactory despite the edit distance being quite high at 7.
    /// </summary>
    private readonly bool _substringsAreSimilar;

    public readonly bool IsDefault => _source is null;

    public WordSimilarityChecker(string text, bool substringsAreSimilar)
    {
        _source = text ?? throw new ArgumentNullException(nameof(text));
        _threshold = GetThreshold(_source);
        _editDistance = new EditDistance(text);
        _substringsAreSimilar = substringsAreSimilar;
    }

    public readonly void Dispose()
    {
        if (this.IsDefault)
            return;

        _editDistance.Dispose();
    }

    public static bool AreSimilar(string originalText, string candidateText)
        => AreSimilar(originalText, candidateText, substringsAreSimilar: false);

    public static bool AreSimilar(string originalText, string candidateText, bool substringsAreSimilar)
        => AreSimilar(originalText, candidateText, substringsAreSimilar, out _);

    public static bool AreSimilar(string originalText, string candidateText, out double similarityWeight)
    {
        return AreSimilar(
            originalText, candidateText,
            substringsAreSimilar: false, similarityWeight: out similarityWeight);
    }

    /// <summary>
    /// Returns true if 'originalText' and 'candidateText' are likely a misspelling of each other.
    /// Returns false otherwise.  If it is a likely misspelling a similarityWeight is provided
    /// to help rank the match.  Lower costs mean it was a better match.
    /// </summary>
    public static bool AreSimilar(string originalText, string candidateText, bool substringsAreSimilar, out double similarityWeight)
    {
        using var checker = new WordSimilarityChecker(originalText, substringsAreSimilar);
        var result = checker.AreSimilar(candidateText, out similarityWeight);

        return result;
    }

    internal static int GetThreshold(string value)
        => value.Length <= 4 ? 1 : 2;

    public bool AreSimilar(string candidateText)
        => AreSimilar(candidateText, out _);

    public bool AreSimilar(string candidateText, out double similarityWeight)
    {
        if (_source.Length < 3)
        {
            // If we're comparing strings that are too short, we'll find 
            // far too many spurious hits.  Don't even bother in this case.
            similarityWeight = double.MaxValue;
            return false;
        }

        if (_lastAreSimilarResult.CandidateText == candidateText)
        {
            similarityWeight = _lastAreSimilarResult.SimilarityWeight;
            return _lastAreSimilarResult.AreSimilar;
        }

        var result = AreSimilarWorker(candidateText, out similarityWeight);
        _lastAreSimilarResult = new CacheResult(candidateText, result, similarityWeight);
        return result;
    }

    private bool AreSimilarWorker(string candidateText, out double similarityWeight)
    {
        similarityWeight = double.MaxValue;

        // If the two strings differ by more characters than the cost threshold, then there's 
        // no point in even computing the edit distance as it would necessarily take at least
        // that many additions/deletions.
        if (Math.Abs(_source.Length - candidateText.Length) <= _threshold)
        {
            similarityWeight = _editDistance.GetEditDistance(candidateText, _threshold);
        }

        if (similarityWeight > _threshold)
        {
            // it had a high cost.  However, the string the user typed was contained
            // in the string we're currently looking at.  That's enough to consider it
            // although we place it just at the threshold (i.e. it's worse than all
            // other matches).
            if (_substringsAreSimilar && candidateText.IndexOf(_source, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                similarityWeight = _threshold;
            }
            else
            {
                return false;
            }
        }

        Debug.Assert(similarityWeight <= _threshold);

        similarityWeight += Penalty(candidateText, _source);
        return true;
    }

    private static double Penalty(string candidateText, string originalText)
    {
        var lengthDifference = Math.Abs(originalText.Length - candidateText.Length);
        if (lengthDifference != 0)
        {
            // For all items of the same edit cost, we penalize those that are 
            // much longer than the original text versus those that are only 
            // a little longer.
            //
            // Note: even with this penalty, all matches of cost 'X' will all still
            // cost less than matches of cost 'X + 1'.  i.e. the penalty is in the 
            // range [0, 1) and only serves to order matches of the same cost.
            //
            // Here's the relation of the first few values of length diff and penalty:
            // LengthDiff   -> Penalty
            // 1            -> .5
            // 2            -> .66
            // 3            -> .75
            // 4            -> .8
            // And so on and so forth.
            var penalty = 1.0 - (1.0 / (lengthDifference + 1));
            return penalty;
        }

        return 0;
    }

    public readonly bool LastCacheResultIs(bool areSimilar, string candidateText)
        => _lastAreSimilarResult.AreSimilar == areSimilar && _lastAreSimilarResult.CandidateText == candidateText;
}

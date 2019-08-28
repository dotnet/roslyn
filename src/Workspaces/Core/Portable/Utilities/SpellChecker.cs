// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Utilities;

namespace Roslyn.Utilities
{
    internal class SpellChecker : IObjectWritable, IChecksummedObject
    {
        private const string SerializationFormat = "3";

        public Checksum Checksum { get; }

        private readonly BKTree _bkTree;

        public SpellChecker(Checksum checksum, BKTree bKTree)
        {
            Checksum = checksum;
            _bkTree = bKTree;
        }

        public SpellChecker(Checksum checksum, IEnumerable<StringSlice> corpus)
            : this(checksum, BKTree.Create(corpus))
        {
        }

        public IList<string> FindSimilarWords(string value)
            => FindSimilarWords(value, substringsAreSimilar: false);

        public IList<string> FindSimilarWords(string value, bool substringsAreSimilar)
        {
            var result = _bkTree.Find(value, threshold: null);

            var checker = WordSimilarityChecker.Allocate(value, substringsAreSimilar);
            var array = result.Where(checker.AreSimilar).ToArray();
            checker.Free();

            return array;
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            Checksum.WriteTo(writer);
            _bkTree.WriteTo(writer);
        }

        internal static SpellChecker TryReadFrom(ObjectReader reader)
        {
            try
            {
                var formatVersion = reader.ReadString();
                if (string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
                {
                    var checksum = Checksum.ReadFrom(reader);
                    var bkTree = BKTree.ReadFrom(reader);
                    if (bkTree != null)
                    {
                        return new SpellChecker(checksum, bkTree);
                    }
                }
            }
            catch
            {
                Logger.Log(FunctionId.SpellChecker_ExceptionInCacheRead);
            }

            return null;
        }
    }

    internal class WordSimilarityChecker
    {
        private struct CacheResult
        {
            public readonly string CandidateText;
            public readonly bool AreSimilar;
            public readonly double SimilarityWeight;

            public CacheResult(string candidate, bool areSimilar, double similarityWeight)
            {
                CandidateText = candidate;
                AreSimilar = areSimilar;
                SimilarityWeight = similarityWeight;
            }
        }

        // Cache the result of the last call to AreSimilar.  We'll often be called with the same
        // value multiple times in a row, so we can avoid expensive computation by returning the
        // same value immediately.
        private CacheResult _lastAreSimilarResult;

        private string _source;
        private EditDistance _editDistance;
        private int _threshold;

        /// <summary>
        /// Whether or words should be considered similar if one is contained within the other
        /// (regardless of edit distance).  For example if is true then IService would be considered
        /// similar to IServiceFactory despite the edit distance being quite high at 7.
        /// </summary>
        private bool _substringsAreSimilar;

        private static readonly object s_poolGate = new object();
        private static readonly Stack<WordSimilarityChecker> s_pool = new Stack<WordSimilarityChecker>();

        public static WordSimilarityChecker Allocate(string text, bool substringsAreSimilar)
        {
            WordSimilarityChecker checker;
            lock (s_poolGate)
            {
                checker = s_pool.Count > 0
                    ? s_pool.Pop()
                    : new WordSimilarityChecker();
            }

            checker.Initialize(text, substringsAreSimilar);
            return checker;
        }

        private WordSimilarityChecker()
        {
        }

        private void Initialize(string text, bool substringsAreSimilar)
        {
            _source = text ?? throw new ArgumentNullException(nameof(text));
            _threshold = GetThreshold(_source);
            _editDistance = new EditDistance(text);
            _substringsAreSimilar = substringsAreSimilar;
        }

        public void Free()
        {
            _editDistance?.Dispose();
            _source = null;
            _editDistance = null;
            _lastAreSimilarResult = default;
            lock (s_poolGate)
            {
                s_pool.Push(this);
            }
        }

        public static bool AreSimilar(string originalText, string candidateText)
            => AreSimilar(originalText, candidateText, substringsAreSimilar: false);

        public static bool AreSimilar(string originalText, string candidateText, bool substringsAreSimilar)
            => AreSimilar(originalText, candidateText, substringsAreSimilar, out var unused);

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
            var checker = Allocate(originalText, substringsAreSimilar);
            var result = checker.AreSimilar(candidateText, out similarityWeight);
            checker.Free();

            return result;
        }

        internal static int GetThreshold(string value)
            => value.Length <= 4 ? 1 : 2;

        public bool AreSimilar(string candidateText)
            => AreSimilar(candidateText, out var similarityWeight);

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
    }
}

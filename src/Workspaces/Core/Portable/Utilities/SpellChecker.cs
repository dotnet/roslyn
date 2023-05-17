// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Roslyn.Utilities
{
    internal readonly struct SpellChecker : IObjectWritable, IChecksummedObject
    {
        private const string SerializationFormat = "3";

        public Checksum Checksum { get; }

        private readonly BKTree _bkTree;

        public SpellChecker(Checksum checksum, BKTree bKTree)
        {
            Checksum = checksum;
            _bkTree = bKTree;
        }

        public SpellChecker(Checksum checksum, IEnumerable<string> corpus)
            : this(checksum, BKTree.Create(corpus))
        {
        }

        public void FindSimilarWords(ref TemporaryArray<string> similarWords, string value, bool substringsAreSimilar)
        {
            using var result = TemporaryArray<string>.Empty;
            using var checker = new WordSimilarityChecker(value, substringsAreSimilar);

            _bkTree.Find(ref result.AsRef(), value, threshold: null);

            foreach (var current in result)
            {
                if (checker.AreSimilar(current))
                    similarWords.Add(current);
            }
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            Checksum.WriteTo(writer);
            _bkTree.WriteTo(writer);
        }

        internal static SpellChecker? TryReadFrom(ObjectReader reader)
        {
            try
            {
                var formatVersion = reader.ReadString();
                if (string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
                {
                    var checksum = Checksum.ReadFrom(reader);
                    var bkTree = BKTree.ReadFrom(reader);
                    if (bkTree != null)
                        return new SpellChecker(checksum, bkTree.Value);
                }
            }
            catch
            {
                Logger.Log(FunctionId.SpellChecker_ExceptionInCacheRead);
            }

            return null;
        }
    }
}

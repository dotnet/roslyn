// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Roslyn.Utilities;

/// <summary>
/// Explicitly a reference type so that the consumer of this in <see cref="BKTree"/> can safely operate on an
/// instance without having to lock to ensure it sees the entirety of the value written out.
/// </summary>>
internal sealed class SpellChecker(BKTree bKTree)
{
    private const string SerializationFormat = "4";

    public SpellChecker(IEnumerable<string> corpus)
        : this(BKTree.Create(corpus))
    {
    }

    public void FindSimilarWords(ref TemporaryArray<string> similarWords, string value, bool substringsAreSimilar)
    {
        using var result = TemporaryArray<string>.Empty;
        using var checker = new WordSimilarityChecker(value, substringsAreSimilar);

        bKTree.Find(ref result.AsRef(), value, threshold: null);

        foreach (var current in result)
        {
            if (checker.AreSimilar(current))
                similarWords.Add(current);
        }
    }

    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(SerializationFormat);
        bKTree.WriteTo(writer);
    }

    internal static SpellChecker? TryReadFrom(ObjectReader reader)
    {
        try
        {
            var formatVersion = reader.ReadString();
            if (string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
            {
                var bkTree = BKTree.ReadFrom(reader);
                if (bkTree != null)
                    return new SpellChecker(bkTree.Value);
            }
        }
        catch
        {
            Logger.Log(FunctionId.SpellChecker_ExceptionInCacheRead);
        }

        return null;
    }
}

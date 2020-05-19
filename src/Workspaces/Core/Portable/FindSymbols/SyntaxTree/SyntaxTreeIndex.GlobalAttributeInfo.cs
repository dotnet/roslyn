// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private readonly struct GlobalAttributeInfo
        {
            private static readonly GlobalAttributeInfo s_empty = new GlobalAttributeInfo(ImmutableDictionary<string, ImmutableArray<int>>.Empty);

            /// <summary>
            /// Map from string literals inside global attributes to their positions inside the syntax tree.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<int>> _docCommentIdStringLiteralsToPositionsInTreeMap { get; }

            public bool IsEmpty => _docCommentIdStringLiteralsToPositionsInTreeMap.IsEmpty;

            /// <summary>
            /// Returns true if we have one or more instances of the given symbol documentation comment id string literal
            /// within global attributes in this tree.
            /// If true, it returns the <paramref name="positionsOfLiteralsInTree"/> for these references.
            /// </summary>
            public bool HasDocCommentIdStringLiterals(string docCommentIdStringLiteral, out ImmutableArray<int> positionsOfLiteralsInTree)
                => _docCommentIdStringLiteralsToPositionsInTreeMap.TryGetValue(docCommentIdStringLiteral, out positionsOfLiteralsInTree);

            public GlobalAttributeInfo(ImmutableDictionary<string, ImmutableArray<int>> stringLiteralsToPositionsMap)
            {
                _docCommentIdStringLiteralsToPositionsInTreeMap = stringLiteralsToPositionsMap;
            }

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(_docCommentIdStringLiteralsToPositionsInTreeMap.Count);

                foreach (var kvp in _docCommentIdStringLiteralsToPositionsInTreeMap)
                {
                    writer.WriteString(kvp.Key);
                    writer.WriteInt32(kvp.Value.Length);

                    foreach (var spanStart in kvp.Value)
                    {
                        writer.WriteInt32(spanStart);
                    }
                }
            }

            public static GlobalAttributeInfo? TryReadFrom(ObjectReader reader)
            {
                try
                {
                    var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<int>>();
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; ++i)
                    {
                        var key = reader.ReadString();
                        var arrayLength = reader.ReadInt32();
                        var valueBuilder = ArrayBuilder<int>.GetInstance(arrayLength);

                        for (var j = 0; j < arrayLength; ++j)
                        {
                            var value = reader.ReadInt32();
                            valueBuilder.Add(value);
                        }

                        builder[key] = valueBuilder.ToImmutableAndFree();
                    }

                    return new GlobalAttributeInfo(builder.ToImmutable());
                }
                catch (Exception)
                {
                }

                return s_empty;
            }
        }
    }
}

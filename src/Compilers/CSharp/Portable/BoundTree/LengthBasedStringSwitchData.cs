// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // The general idea is to stratify cases by
    // first bucketing on Length
    // then bucketing on a character position selected by heuristic
    // and finally switching to exact string (this is a simple string comparison when only one possibility remains).
    //
    // The benefit of this approach is that it much reduces the need for computing
    // the input string's hashcode.
    //
    // We emit something like:
    //
    // // null case:
    // if (key is null)
    //   goto labelNull; OR goto labelDefault;
    //
    // switch (key.Length)
    // {
    //   // empty string doesn't need a char or string test
    //   case 0: goto labelEmpty;
    //
    //   // strings of length 1 don't need any further validation once we've checked one char
    //   case 1:
    //     switch (key[posM])
    //     {
    //        case '1': goto label1;
    //        case '2': goto label2;
    //        ...
    //        default: goto labelDefault;
    //     }
    //   ...
    //   // when a given length is sufficient to narrow down to one case we skip the char test:
    //   case N: if (key == "caseN") { goto labelN; } else { goto labelDefault; }
    //   ...
    //   case M:
    //     switch (key[posM])
    //     {
    //        // when a single character check narrows down to one possibility:
    //        case '1': if (key == "caseM1") { goto labelM1; } else { goto labelDefault; }
    //
    //        // when a single character check leaves a few possibilities remaining (worst case scenario):
    //        case '2':
    //          switch (key)
    //          {
    //            case "caseM1_A": goto labelM1_A;
    //            case "caseM1_B": goto labelM1_B;
    //            ...
    //            default: goto labelDefault;
    //          }
    //        ...
    //        default: goto labelDefault;
    //      }
    //    ...
    //    default: goto labelDefault;
    // }

    internal sealed class LengthBasedStringSwitchData
    {
        internal readonly LengthJumpTable LengthBasedJumpTable;
        internal readonly ImmutableArray<CharJumpTable> CharBasedJumpTables;
        internal readonly ImmutableArray<StringJumpTable> StringBasedJumpTables;

        internal LengthBasedStringSwitchData(LengthJumpTable lengthJumpTable,
            ImmutableArray<CharJumpTable> charJumpTables, ImmutableArray<StringJumpTable> stringJumpTables)
        {
            LengthBasedJumpTable = lengthJumpTable;
            CharBasedJumpTables = charJumpTables;
            StringBasedJumpTables = stringJumpTables;
        }

        internal struct LengthJumpTable
        {
            public readonly LabelSymbol? NullCaseLabel;
            public readonly ImmutableArray<(int value, LabelSymbol label)> LengthCaseLabels;

            public LengthJumpTable(LabelSymbol? nullCaseLabel, ImmutableArray<(int value, LabelSymbol label)> lengthCaseLabels)
            {
                Debug.Assert(lengthCaseLabels.Length > 0);

                this.NullCaseLabel = nullCaseLabel;
                this.LengthCaseLabels = lengthCaseLabels;
            }
        }

        internal struct CharJumpTable
        {
            public readonly LabelSymbol Label;
            public readonly int SelectedCharPosition;
            public readonly ImmutableArray<(char value, LabelSymbol label)> CharCaseLabels;

            internal CharJumpTable(LabelSymbol label, int selectedCharPosition, ImmutableArray<(char value, LabelSymbol label)> charCaseLabels)
            {
                Debug.Assert(charCaseLabels.Length > 0);

                this.Label = label;
                this.SelectedCharPosition = selectedCharPosition;
                this.CharCaseLabels = charCaseLabels;
            }
        }

        internal struct StringJumpTable
        {
            public readonly LabelSymbol Label;
            public readonly ImmutableArray<(string value, LabelSymbol label)> StringCaseLabels;

            internal StringJumpTable(LabelSymbol label, ImmutableArray<(string value, LabelSymbol label)> stringCaseLabels)
            {
                Debug.Assert(stringCaseLabels.Length > 0);

                this.Label = label;
                this.StringCaseLabels = stringCaseLabels;
            }
        }

        // Based on benchmarks, the previous hashcode-based approach arguably performs better
        // when buckets have 6 candidates or more.
        internal bool ShouldGenerateLengthBasedSwitch(int labelsCount)
        {
            return SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(labelsCount) &&
                StringBasedJumpTables.All(t => t.StringCaseLabels.Length <= 5);
        }

        internal static LengthBasedStringSwitchData Create(ImmutableArray<(ConstantValue value, LabelSymbol label)> inputCases)
        {
            Debug.Assert(inputCases.All(c => c.value.IsString && c.label is not null));

            LabelSymbol? nullCaseLabel = null;
            foreach (var inputCase in inputCases)
            {
                if (inputCase.value.IsNull)
                {
                    Debug.Assert(nullCaseLabel is null, "At most one null case per string dispatch");
                    nullCaseLabel = inputCase.label;
                }
            }

            var lengthCaseLabels = ArrayBuilder<(int value, LabelSymbol label)>.GetInstance();
            var charJumpTables = ArrayBuilder<CharJumpTable>.GetInstance();
            var stringJumpTables = ArrayBuilder<StringJumpTable>.GetInstance();
            foreach (var group in inputCases.Where(c => !c.value.IsNull).GroupBy(c => c.value.StringValue!.Length))
            {
                int stringLength = group.Key;
                var labelForLength = CreateAndRegisterCharJumpTables(stringLength, group.SelectAsArray(c => (c.value.StringValue!, c.label)), charJumpTables, stringJumpTables);
                lengthCaseLabels.Add((stringLength, labelForLength));
            }

            var lengthJumpTable = new LengthJumpTable(nullCaseLabel, lengthCaseLabels.ToImmutableAndFree());
            return new LengthBasedStringSwitchData(lengthJumpTable, charJumpTables.ToImmutableAndFree(), stringJumpTables.ToImmutableAndFree());
        }

        private static LabelSymbol CreateAndRegisterCharJumpTables(int stringLength, ImmutableArray<(string value, LabelSymbol label)> casesWithGivenLength,
            ArrayBuilder<CharJumpTable> charJumpTables, ArrayBuilder<StringJumpTable> stringJumpTables)
        {
            Debug.Assert(stringLength >= 0);
            Debug.Assert(casesWithGivenLength.All(c => c.value.Length == stringLength));
            Debug.Assert(casesWithGivenLength.Length > 0);

            if (stringLength == 0)
            {
                // Only the empty string has zero Length, no need for further testing
                return casesWithGivenLength.Single().label;
            }

            if (casesWithGivenLength.Length == 1)
            {
                // We only have one case for the given string length, we don't need to do a char test
                // Instead we'll jump straight to the final string test
                return CreateAndRegisterStringJumpTable(casesWithGivenLength, stringJumpTables);
            }

            var bestCharacterPosition = selectBestCharacterIndex(stringLength, casesWithGivenLength);
            var charCaseLabels = ArrayBuilder<(char value, LabelSymbol label)>.GetInstance();
            foreach (var group in casesWithGivenLength.GroupBy(c => c.value[bestCharacterPosition]))
            {
                // When dealing with a stringLength==1 bucket, a character check gives us the final answer,
                // no need to follow with a string check.
                LabelSymbol label = (stringLength == 1)
                    ? group.Single().label
                    : CreateAndRegisterStringJumpTable(group.ToImmutableArray(), stringJumpTables);
                char character = group.Key;
                charCaseLabels.Add((character, label));
            }

            var charJumpTable = new CharJumpTable(label: new GeneratedLabelSymbol("char-dispatch"), bestCharacterPosition, charCaseLabels.ToImmutableAndFree());
            charJumpTables.Add(charJumpTable);
            return charJumpTable.Label;

            static int selectBestCharacterIndex(int stringLength, ImmutableArray<(string value, LabelSymbol label)> caseLabels)
            {
                // We pick the position that maximizes number of buckets with a single entry.
                // We break ties by preferring lower max bucket size.
                Debug.Assert(stringLength > 0);
                Debug.Assert(caseLabels.Length > 0);
                int bestIndex = -1;
                int bestIndexSingleEntryCount = -1;
                int bestIndexLargestBucket = int.MaxValue;
                for (int currentPosition = 0; currentPosition < stringLength; currentPosition++)
                {
                    (int singleEntryCount, int largestBucket) = positionScore(currentPosition, caseLabels);

                    if (singleEntryCount > bestIndexSingleEntryCount ||
                        (singleEntryCount == bestIndexSingleEntryCount && largestBucket < bestIndexLargestBucket))
                    {
                        bestIndexSingleEntryCount = singleEntryCount;
                        bestIndexLargestBucket = largestBucket;
                        bestIndex = currentPosition;
                    }
                }

                return bestIndex;
            }

            // Given a position and a set of string cases of matching lengths, inspect the buckets created by inspecting
            // those strings at that position. Return the count how many buckets have a single entry and the size of the largest bucket.
            static (int singleEntryCount, int largestBucket) positionScore(int position, ImmutableArray<(string value, LabelSymbol label)> caseLabels)
            {
                var countPerChar = PooledDictionary<char, int>.GetInstance();
                foreach (var caseLabel in caseLabels)
                {
                    Debug.Assert(caseLabel.value is not null);
                    var currentChar = caseLabel.value[position];
                    if (countPerChar.TryGetValue(currentChar, out var currentCount))
                    {
                        countPerChar[currentChar] = currentCount + 1;
                    }
                    else
                    {
                        countPerChar[currentChar] = 1;
                    }
                }

                var singleEntryCount = countPerChar.Values.Count(c => c == 1);
                var largestBucket = countPerChar.Values.Max();
                countPerChar.Free();
                return (singleEntryCount, largestBucket);
            }
        }

        private static LabelSymbol CreateAndRegisterStringJumpTable(ImmutableArray<(string value, LabelSymbol label)> cases, ArrayBuilder<StringJumpTable> stringJumpTables)
        {
            Debug.Assert(cases.Length > 0 && cases.All(c => c.value is not null));
            var stringJumpTable = new StringJumpTable(label: new GeneratedLabelSymbol("string-dispatch"), cases.SelectAsArray(c => (c.value, c.label)));
            stringJumpTables.Add(stringJumpTable);
            return stringJumpTable.Label;
        }

#if DEBUG
        public string Dump()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Length dispatch:");
            builder.AppendLine($"Buckets: {string.Join(", ", StringBasedJumpTables.Select(t => t.StringCaseLabels.Length))}");
            builder.AppendLine($"  case null: {readable(LengthBasedJumpTable.NullCaseLabel)}");
            dump(LengthBasedJumpTable.LengthCaseLabels);
            builder.AppendLine();

            builder.AppendLine("Char dispatches:");
            foreach (var charJumpTable in CharBasedJumpTables)
            {
                builder.AppendLine($"Label {readable(charJumpTable.Label)}:");
                builder.AppendLine($"  Selected char position: {charJumpTable.SelectedCharPosition}:");
                dump(charJumpTable.CharCaseLabels);
            }
            builder.AppendLine();

            builder.AppendLine("String dispatches:");
            foreach (var stringJumpTable in StringBasedJumpTables)
            {
                builder.AppendLine($"Label {readable(stringJumpTable.Label)}:");
                dump(stringJumpTable.StringCaseLabels);
            }
            builder.AppendLine();

            return builder.ToString();

            void dump<T>(ImmutableArray<(T value, LabelSymbol label)> cases)
            {
                foreach (var (constant, label) in cases)
                {
                    builder.AppendLine($"  case {constant}: {readable(label)}");
                }
            }

            string readable(LabelSymbol? label)
            {
                if (label is null)
                {
                    return "<null>";
                }

                return label.ToString();
            }
        }
#endif
    }
}

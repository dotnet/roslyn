// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal class SemanticTokensEditsDiffer : TextDiffer
    {
        private SemanticTokensEditsDiffer(IReadOnlyList<int> oldArray, int[] newArray)
        {
            if (oldArray is null)
            {
                throw new ArgumentNullException(nameof(oldArray));
            }

            OldArray = oldArray;
            NewArray = newArray;
        }

        private IReadOnlyList<int> OldArray { get; }
        private int[] NewArray { get; }

        protected override int OldTextLength => OldArray.Count;
        protected override int NewTextLength => NewArray.Length;

        protected override bool ContentEquals(int oldTextIndex, int newTextIndex)
        {
            return OldArray[oldTextIndex] == NewArray[newTextIndex];
        }

        public static IReadOnlyList<DiffEdit> ComputeSemanticTokensEdits(
            int[] oldTokens,
            int[] newTokens)
        {
            var differ = new SemanticTokensEditsDiffer(oldTokens, newTokens);
            var diffs = differ.ComputeDiff();

            return diffs;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax
{
    internal static class CommonSyntaxNodeRemover
    {
        public static void GetSeparatorInfo(
            SyntaxNodeOrTokenList nodesAndSeparators, int nodeIndex, int endOfLineKind,
            out bool nextTokenIsSeparator, out bool nextSeparatorBelongsToNode)
        {
            // remove preceding separator if any, except for the case where
            // the following separator immediately touches the item in the list
            // and is followed by a newline.
            // 
            // In that case, we consider the next token to be more closely
            // associated with the item, and it should be removed.
            //
            // For example, if you have:
            //
            //      Goo(a, // a stuff
            //          b, // b stuff
            //          c);
            //
            // If we're removing 'b', we should remove the comma after it.
            //
            // If there is no next comma, or the next comma is not on the 
            // same line, then just remove the preceding comma if there is 
            // one.  If there is no next or previous comma there's nothing
            // in the list that needs to be fixed up.

            var node = nodesAndSeparators[nodeIndex].AsNode();

            nextTokenIsSeparator =
                nodeIndex + 1 < nodesAndSeparators.Count &&
                nodesAndSeparators[nodeIndex + 1].IsToken;

            nextSeparatorBelongsToNode =
                nextTokenIsSeparator && nodesAndSeparators[nodeIndex + 1].AsToken() is { HasLeadingTrivia: false } nextSeparator &&
ContainsEndOfLine(node.GetTrailingTrivia(), endOfLineKind) is false &&
                ContainsEndOfLine(nextSeparator.TrailingTrivia, endOfLineKind);
        }

        private static bool ContainsEndOfLine(SyntaxTriviaList triviaList, int endOfLineKind)
            => triviaList.IndexOf(endOfLineKind) >= 0;
    }
}

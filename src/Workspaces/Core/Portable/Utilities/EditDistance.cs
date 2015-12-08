// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using static System.Math;

namespace Roslyn.Utilities
{
    internal class EditDistance : IDisposable
    {
        private string originalText;
        private char[] originalTextArray;
        private int threshold;

        // Cache the result of the last call to IsCloseMatch.  We'll often be called with the same
        // value multiple times in a row, so we can avoid expensive computation by returning the
        // same value immediately.
        private ValueTuple<string, bool, double> lastIsCloseMatchResult;

        public EditDistance(string text, int? threshold = null)
        {
            originalText = text;
            originalTextArray = ConvertToLowercaseArray(text);

            // We only allow fairly close matches (in order to prevent too many
            // spurious hits).  A reasonable heauristic for this is the Log_2(length) (rounded 
            // down).  
            //
            // Strings length 1-3 : 1 edit allowed.
            //         length 4-7 : 2 edits allowed.
            //         length 8-15: 3 edits allowed.
            //
            // and so forth.
            this.threshold = threshold ?? Max(1, (int)Log(text.Length, 2));
        }

        private static char[] ConvertToLowercaseArray(string text)
        {
            var array = Pool<char>.GetArray(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                array[i] = char.ToLower(text[i]);
            }

            return array;
        }

        public void Dispose()
        {
            Pool<char>.ReleaseArray(originalTextArray);
            originalText = null;
            originalTextArray = null;
        }

        public static int GetEditDistance(string s, string t, int? threshold = null)
        {
            using (var editDistance = new EditDistance(s, threshold))
            {
                return editDistance.GetEditDistance(t);
            }
        }

        public int GetEditDistance(string other)
        {
            var otherCharacterArray = ConvertToLowercaseArray(other);

            try
            {
                // Swap the strings so the first is always the shortest.  This helps ensure some
                // nice invariants in the code that walks both strings below.
                return originalText.Length <= other.Length
                    ? GetEditDistance(originalTextArray, otherCharacterArray, originalText.Length, other.Length, threshold)
                    : GetEditDistance(otherCharacterArray, originalTextArray, other.Length, originalText.Length, threshold);
            }
            finally
            {
                Pool<char>.ReleaseArray(otherCharacterArray);
            }
        }

        private static int GetEditDistance(
            char[] shortString, char[] longString,
            int shortLength, int longLength,
            int costThreshold)
        {
            // Note: shortLength and longLength values will mutate and represent the lengths 
            // of the portions of the arrays we want to compare.
            //
            // Also note: shortLength will always be smaller or equal to longLength.
            Debug.Assert(shortLength <= longLength);

            // Optimized version of the restricted string edit distance algorithm:
            // see https://en.wikipedia.org/wiki/Edit_distance.
            //
            // The optimized version uses insights from Ukonnen to greatly diminish the amount
            // of work that needs to be done. http://www.cs.helsinki.fi/u/ukkonen/InfCont85.PDF
            //
            // First, because we only need the distance, and don't need the path, we can do the 
            // standard approach of not requiring a full matrix to store all our edit distances.
            // 
            // Second, because a threshold is provided, we only need to search the values in
            // matrix that are within threshold distance away from the diagonal.  Any paths
            // outside that diagonal would necessarily have an edit distance that was greater
            // than the threshold and thus can be avoided.  i.e. if we have a matrix like
            //
#if false
            -------------
            | abcdefghij|
            |m          |
            |n          |
            |o          |
            -------------

            Then the two relevant diagonals are

            -------------
            | abcdefghij|
            |m\      \  |
            |n \      \ |
            |o  \      \|
            -------------

            And we can cap the search past the diagonals based on the threshold.  For larger strings
            With a small threshold, this can be very valuable.  For example, if we had 10 char strings
            with a a threshold of 2, we'd only have to search

            -------------
            | abcdefghij|
            |m\**       |
            |n*\**      |
            |o**\**     |
            |p **\**    |
            |q  **\**   |
            |r   **\**  |
            |s    **\** |
            |t     **\**|
            |u      **\*|
            |v       **\|
            -------------

            Which greatly decreases the search space.  Any items outside of this range will necessarily
            have a higher cost than our threshold and thus don't even need to be considered.

#endif
            //
            // Third, instead of taking the swapping approach often found in algorithms that 
            // go a row at a time through the matrix, we can take avantage of the fact that
            // we know we're always sweeping the matrix from top to bottom and from left to
            // right.  In that case, as we're producing new row values, we can store it in 
            // the existing row.  However, we make sure to just keep around the values we're
            // about to overwrite so we can use them as we continue to populate the row.
            // This optimization is explained in more detail below.
            //
            // Note: because we do look for characater twiddling, we do need to keep around
            // a single previous row as well.  But this means keeping around two rows instead
            // of three.
            //
            // Fourth, we do fast scans to see if our strings share any common prefix/suffix
            // portions.  These portions of the string correspond to a 0 edit cost.  We then
            // only have to do the string edit distance on the portion of the strings that
            // don't match.
            //
            // Fifth, to avoid lots of overhead indexing into strings and converting their
            // characters to lowercase, we do a prepass over the string and convert it to
            // a lowercase char array.  We also pool these arrays to avoid the allocations.
            // By doing this, we only need to convert characters once, and we can then 
            // access the characters extremely quickly from the array.
            //
            // Sixth, we optimize for the case where we will be comparing a single string
            // against many potential match strings.  We cache the information about the 
            // single string (such as it's lower-case char array) so that we don't have to
            // recompute it.
            //
            // With all these changes we see an improvement of about 20x when computing the
            // edit distance for strings.  In practical terms, what was 4 seconds of searching
            // for results becomes around 0.2 seconds.

            // First:
            // Determine the common prefix/suffix portions of the strings.
            while (shortLength > 0 && shortString[shortLength - 1] == longString[longLength - 1])
            {
                shortLength--;
                longLength--;
            }

            var startIndex = 0;
            while (startIndex < shortLength && shortString[startIndex] == longString[startIndex])
            {
                startIndex++;
                shortLength--;
                longLength--;
            }

            // 'shortLength' and 'longLength' are now the lengths of the substrings of our strings that we
            // want to compare. 'startIndex' is the starting point of the substrings in both array.

            // If we've matched all of the 'short' string in the prefix and suffix of 'longString'. then the edit
            // distance is just whatever operations we have to create the remaining longString substring.
            if (shortLength == 0)
            {
                return longLength;
            }

#if false
            // Note: this check is not necessary. Recall that shortLength is always less than 
            // longLength.  So if longLength was 0 then shortLength would also be zero, and the
            // above case would hit.
            //
            // I'm keeping this in just to help clarify this in case someone wonders why that
            // check is missing.
            if (longLength == 0)
            {
                return shortLength;
            }
#endif

            // The is the minimum number of edits we'd have to make.  i.e. if  'shortString' and 
            // 'longString' are the same length, then we might not need to make any edits.  However,
            // if longString has length 10 and shortString has length 7, then we're going to have to
            // make at least 3 edits.

            var minimumEditCount = longLength - shortLength;
            Debug.Assert(minimumEditCount >= 0);

            // If our threshold is greater than the number of edits it would take to just produce 'longString'
            // then just cap the threshold.  In the code below 'threshold' acts as the diagonal
            // we don't have to look outside of.  By capping the threshold here we make the math
            // easy below.
            if (costThreshold > longLength)
            {
                costThreshold = longLength;
            }

            // If the number of edits we'd have to perform is greater than our threshold, then
            // there's no point in even continuing.
            if (minimumEditCount > costThreshold)
            {
                return int.MaxValue;
            }

            //  The matrix we are virtually simulating here is indexed in the following fashion:
            //
            //      i<- 0 1 2 3 4 5 6 7
            //    j     X l m R e a d e
            //    ^  
            //    0 X
            //    1 m
            //    2 l
            //    3 R
            //    4 e
            //    5 a
            //    6 d
            //    7 e
            //    8 r
            //
            //          It will be virtually initialized with the following values.  These represent the
            //          initial 'left' and 'above' values for the algorithm below
            //
            //      i<-|0 1 2 3 4 5 6 7
            //    j    |X l m R e a d e
            //    ^   0|1 2 3 4 5 6 7 8
            //    -----+---------------
            //    0 X 1|
            //    1 m 2|
            //    2 l 3|
            //    3 R 4|
            //    4 e 5|
            //    5 a 6|
            //    6 d 7|
            //    7 e 8|
            //    8 r 9|
            //
            //          We'll then proceed a column at a time from left to right producing the new column values.
            //          In the absense of twiddles we can note something useful:  The value for any given (i,j) we
            //          are computing is only dependent on (i - 1, j), (i, j-1), and (i - 1, j - 1).  These correspond,
            //          respectively, to the costs to delete, insert, or change/keep a character.  In traditional 
            //          implementations we could accomplish this by having an array for the column we are generating
            //          and having an array for the previous column as well.  
            //          
            //          However, we can be a bit smarter and use a single array.  How?  Well, instead of writing 
            //          the new values into a new column, we can write it right back into the column we're currently
            //          traversing.  After all, if we're writing into (i, j), then (i-1, j) is just the current value
            //          in the cell.  (i, j-1) is the value in the cell right above us that we just computed before 
            //          this cell.  And (i-1, j-1) is the value in the cell right above *before* we computed the 
            //          new value for it.  The only trickiness is we have to store the value of the cell above us
            //          in a temporary before we overwrite it.  Now we have access to all three values we need to
            //          compute the new (i,j) value.
            //
            //          Note, we do something similar for twiddling. However, because a twiddle cost is "1 + (i-2, j-2)"
            //          we need to keep around our i-2 values.  We can't do that if we're overwriting all the values
            //          in the column.  So we do keep around one additional column for the i-2 generation.
            //
            //          With our heuristic of Log_2(length) changes allowed this will end up producing the following
            //          set of values:
            //
            //      i<- 0 1 2 3 4 5 6 7
            //    j    |X l m R e a d e
            //    ^   0|1 2 3 4 5 6 7 8
            //    -----+---------------
            //    0 X 1|0 1 2
            //    1 m 2|1 1 1 2 
            //    2 l 3|2 1 1 2 3 
            //    3 R 4|3 2 2 1 2 3 
            //    4 e 5|  3 3 2 1 2 3 
            //    5 a 6|    4 3 2 1 2 3
            //    6 d 7|      4 3 2 1 2
            //    7 e 8|        4 3 2 1
            //    8 r 9|          4 3 2
            //
            //          Note that we've avoid examining 30 elements in the matrix (out of 8*9=72), or roughly
            //          40%. 

            var costArray = Pool<int>.GetArray(longLength);
            var previousCostArray = Pool<int>.GetArray(longLength);

            try
            {
                // here we are setting the initial 'left' array:

                //      i<-|
                //    j    |
                //    ^   0|
                //    -----+ 
                //    0 X 1|
                //    1 m 2|
                //    2 l 3|
                //    3 R 4|
                //    4 e 5|
                //    5 a 6|
                //    6 d 7|
                //    7 e 8|
                //    8 r 9|
                //
                // This is the cost to go from an empty string to the longString, and is essentially
                // the cost of inserting all the characters.  By doing this, we have access to 'left'
                // values as we actualy walk through each column.
                for (var i = 0; i < longLength; i++)
                {
                    costArray[i] = i + 1;
                }

                // We only need to bother even checking the threshold if it's lower than the 
                // cost of just creating all of t.
                var checkThreshold = costThreshold < longLength;

                // Some complicated indices here.  We effectively only want to walk the portion
                // of the matrix that is 'offset' around the diagonal.  So as we're walking
                // we check if we're getting past the offset, and if so, we bump our indices
                // to skip the values we don't need to check.

                var offset = costThreshold - minimumEditCount;

                var jFrom = 0;
                var jTo = costThreshold;

                var currentShortCharacter = shortString[startIndex];
                var editDistance = 0;

                // Walk the matrix from left to right, one column at a time.
                for (int i = 0; i < shortLength; i++)
                {
                    // Keep track of the previous character in 'shortString'.  We'll need it for
                    // the twiddle check.
                    var previousShortCharacter = currentShortCharacter;
                    currentShortCharacter = shortString[startIndex + i];

                    var currentLongCharacter = longString[startIndex];

                    // as we start going past the offset increase where we start looking within 
                    // 'longString'.
                    if (i > offset)
                    {
                        jFrom++;
                    }

                    // Keep incrementing jTo (so the length of our window stays the same) as long
                    // as it wouldn't go past the length of the string we want to check.
                    if (jTo < longLength)
                    {
                        jTo++;
                    }

                    // Note: we're just setting 'editDistance' here so that it is the initial
                    // 'above' value when we start processing this column.  The above values
                    // are very simple if we're processing the entire column:
                    // 
                    //       i<- 0 1 2 3 4 5 6 7
                    //     j    |X l m R e a d e
                    //     ^   0|1 2 3 4 5 6 7 8    <-- above values
                    //
                    // i.e. they're just equal to i+1.  However, if we're only processing
                    // a part of a column, then the 'above' value doesn't exist.  In this case 
                    //
                    //      3 R 4|3 2 2 1 2 3 4 
                    //      4 e 5|4 3 3 2 1 2 3 ?    <-- No value above when we're starting at ?
                    //
                    // This is because we can't actually have an edit that comes in through
                    // the top of the column.  This edit would necessarily be more costly 
                    // than our threshold.  To handle this, we simply set the edit distance
                    // to int.Max.  This will make the 'above' value int.Max, and it means
                    // we'll never pick it as our path.
                    editDistance = jFrom == 0 ? i + 1 : int.MaxValue;

                    // 'aboveLeft' is computed in a similar fashion, but doesn't have this same
                    // problem.  In the case where we're at the top aboveLeft is simply i.  And
                    // in a case where we're starting in the middle of the column, by construction,
                    // we'll always have the aboveLeft value in location in the column right above
                    // where we're starting at.
                    var aboveLeftEditDistance = jFrom == 0 ? i : costArray[jFrom - 1];

                    var nextTwiddleCost = 0;
                    for (var j = jFrom; j < jTo; j++)
                    {
                        // Note: any acceses into costArray *before* we write into it represent values of 
                        // (i-1,*).  Once we write into it that value will represent (i, *).

                        // As we move down the column our previously written edit distance becomes the 
                        // 'above' value for the next computation.  i.e. the value we previously wrote 
                        // into (i, j) is now at (i, j-1).
                        var aboveEditDistance = editDistance;

                        // Initialize the editDistance for (i,j) to be the edit distance for
                        // (i-1, j-1).  If they match on characters, we'll keep this edit distance.
                        // If they differ, then we'll check what gives us the cheapest edit distance.
                        editDistance = aboveLeftEditDistance;

                        // before we overwrite the current cost at (i,j), keep track of the value
                        // of (i-1, j).  At this point:
                        //
                        //  1) editDistance corresponds to      (i-1, j-1) 
                        //  2) aboveEditDistance corresponds to (i, j-1)
                        //  3) leftEditDistance corresponds to   (i-1, j)
                        var leftEditDistance = costArray[j];

                        var currentTwiddleCost = nextTwiddleCost;
                        nextTwiddleCost = previousCostArray[j];
                        previousCostArray[j] = editDistance;

                        var previousLongCharacter = currentLongCharacter;
                        currentLongCharacter = longString[startIndex + j];

                        if (currentShortCharacter != currentLongCharacter)
                        {
                            // Because the characters didn't match, our edit distance is the min of:
                            // "(i-1,j-1) + 1"   "(i-1,j) + 1"   "(i, j-1) + 1"
                            //
                            // No matter what we have to add one, so we always do that below.
                            // So now we just need to compare (i-1,j-1)   (i-1,j)   and    (i, j-1)
                            //
                            // editDistance was already set to (i-1,j-1) above.  So all we need to do
                            // is compare that to (i-1,j) and (i, j-1)  and pick the smallest.

                            if (leftEditDistance < editDistance)
                            {
                                // (i-1,j) < (i-1,j-1)
                                editDistance = leftEditDistance;
                            }

                            if (aboveEditDistance < editDistance)
                            {
                                // (i,j-1) < (i-1,j-1) && (i-1, j)
                                editDistance = aboveEditDistance;
                            }

                            // We're always one greater than the min edit distance for inserting/deleting/changing.
                            editDistance++;

                            // Check for twiddles if we're past the first row and column.
                            if (i != 0 && j != 0 && currentShortCharacter == previousLongCharacter && previousShortCharacter == currentLongCharacter)
                            {
                                currentTwiddleCost++;
                                if (currentTwiddleCost < editDistance)
                                {
                                    editDistance = currentTwiddleCost;
                                }
                            }
                        }

                        costArray[j] = editDistance;

                        // Keep track of what is in (i-1, j) now.  The next time through this loop
                        // it will be (i-1, j-1) (hence 'aboveLeft').
                        aboveLeftEditDistance = leftEditDistance;
                    }

                    // Recall that minimumEditCount is simply the difference in length of our two
                    // strings.  So costArray[i] is the cost for the upper-left diagonal of the
                    // matrix.  costArray[i+minimumEditCount] is the cost for the lower right diagonal.
                    // Here we are simply getting the lowest cost edit of hese two substrings so far.
                    // If this lowest cost edit is greater than our threshold, then there is no need 
                    // to proceed.
                    if (checkThreshold && (costArray[i + minimumEditCount] > costThreshold))
                    {
                        return int.MaxValue;
                    }
                }

                return editDistance;
            }
            finally
            {
                Pool<int>.ReleaseArray(costArray);
                Pool<int>.ReleaseArray(previousCostArray);
            }
        }

        public static bool IsCloseMatch(string originalText, string candidateText)
        {
            double dummy;
            return IsCloseMatch(originalText, candidateText, out dummy);
        }

        /// <summary>
        /// Returns true if 'value1' and 'value2' are likely a misspelling of each other.
        /// Returns false otherwise.  If it is a likely misspelling a matchCost is provided
        /// to help rank the match.  Lower costs mean it was a better match.
        /// </summary>
        public static bool IsCloseMatch(string originalText, string candidateText, out double matchCost)
        {
            using (var editDistance = new EditDistance(originalText))
            {
                return editDistance.IsCloseMatch(candidateText, out matchCost);
            }
        }

        public bool IsCloseMatch(string candidateText, out double matchCost)
        {
            if (this.originalText.Length < 3)
            {
                // If we're comparing strings that are too short, we'll find 
                // far too many spurious hits.  Don't even both in this case.
                matchCost = double.MaxValue;
                return false;
            }

            if (lastIsCloseMatchResult.Item1 == candidateText)
            {
                matchCost = lastIsCloseMatchResult.Item3;
                return lastIsCloseMatchResult.Item2;
            }

            var result = IsCloseMatchWorker(candidateText, out matchCost);
            lastIsCloseMatchResult = ValueTuple.Create(candidateText, result, matchCost);
            return result;
        }

        private bool IsCloseMatchWorker(string candidateText, out double matchCost)
        {
            matchCost = double.MaxValue;

            // If the two strings differ by more characters than the cost threshold, then there's 
            // no point in even computing the edit distance as it would necessarily take at least
            // that many additions/deletions.
            if (Math.Abs(originalText.Length - candidateText.Length) <= threshold)
            {
                matchCost = GetEditDistance(candidateText);
            }

            if (matchCost > threshold)
            {
                // it had a high cost.  However, the string the user typed was contained
                // in the string we're currently looking at.  That's enough to consider it
                // although we place it just at the threshold (i.e. it's worse than all
                // other matches).
                if (candidateText.IndexOf(originalText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchCost = threshold;
                }
            }

            if (matchCost > threshold)
            {
                return false;
            }

            matchCost += Penalty(candidateText, this.originalText);
            return true;
        }

        private static double Penalty(string candidateText, string originalText)
        {
            int lengthDifference = Math.Abs(originalText.Length - candidateText.Length);
            if (lengthDifference != 0)
            {
                // For all items of the same edit cost, we penalize those that are 
                // much longer than the original text versus those that are only 
                // a little longer.
                //
                // Note: even with this penalty, all matches of cost 'X' will all still
                // cost less than matches of cost 'X + 1'.  i.e. the penalty is in the 
                // range [0, 1) and only serves to order matches of the same cost.
                double penalty = 1.0 - (1.0 / lengthDifference);
                return penalty;
            }

            return 0;
        }

        internal static class Pool<T>
        {
            private const int MaxPooledArraySize = 256;

            // Keep around a few arrays of size 256 that we can use for operations without
            // causing lots of garbage to be created.  If we do compare items larger than
            // that, then we will just allocate and release those arrays on demand.
            private static ObjectPool<T[]> s_pool = new ObjectPool<T[]>(() => new T[MaxPooledArraySize]);

            public static T[] GetArray(int size)
            {
                if (size <= MaxPooledArraySize)
                {
                    var array = s_pool.Allocate();
                    Array.Clear(array, 0, array.Length);
                    return array;
                }

                return new T[size];
            }

            public static void ReleaseArray(T[] array)
            {
                if (array.Length <= MaxPooledArraySize)
                {
                    s_pool.Free(array);
                }
            }
        }
    }
}
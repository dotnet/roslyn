// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IBraceMatchingServiceExtensions
    {
        /// <summary>
        /// Given code like   ()^()  (where ^ is the caret position), returns the two pairs of
        /// matching braces on the left and the right of the position.  Note: a brace matching
        /// pair is only returned if the position is on the left-side of hte start brace, or the
        /// right side of end brace.  So, for example, if you have (^()), then only the inner 
        /// braces are returned as the position is not on the right-side of the outer braces.
        /// 
        /// This function also works for multi-character braces i.e.  ([  ])   In this case,
        /// the rule is that the position has to be on the left side of the start brace, or 
        /// inside the start brace (but not at the end).  So,    ^([   ])  will return this
        /// as a brace match, as will  (^[    ]).  But   ([^   ])  will not.
        /// 
        /// The same goes for the braces on the the left of the caret.  i.e.:   ([   ])^
        /// will return the braces on the left, as will   ([   ]^).  But   ([   ^]) will not.
        /// </summary>
        public static async Task<(BraceMatchingResult? leftOfPosition, BraceMatchingResult? rightOfPosition)> GetAllMatchingBracesAsync(
            this IBraceMatchingService service,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            // These are the matching spans when checking the token to the right of the position.
            var rightOfPosition = await service.GetMatchingBracesAsync(document, position, cancellationToken).ConfigureAwait(false);

            // The braces to the right of the position should only be added if the position is 
            // actually within the span of the start brace.  Note that this is what we want for
            // single character braces as well as multi char braces.  i.e. if the user has:
            //
            //      ^{ }    // then { and } are matching braces.
            //      {^ }    // then { and } are not matching braces.
            //
            //      ^<@ @>  // then <@ and @> are matching braces.
            //      <^@ @>  // then <@ and @> are matching braces.
            //      <@^ @>  // then <@ and @> are not matching braces.
            if (rightOfPosition.HasValue &&
                !rightOfPosition.Value.LeftSpan.Contains(position))
            {
                // Not a valid match.  
                rightOfPosition = null;
            }

            if (position == 0)
            {
                // We're at the start of the document, can't find braces to the left of the position.
                return (leftOfPosition: null, rightOfPosition);
            }

            // See if we're touching the end of some construct.  i.e.:
            //
            //      { }^
            //      <@ @>^
            //      <@ @^>
            //
            // But not
            //
            //      { ^}
            //      <@ ^@>

            var leftOfPosition = await service.GetMatchingBracesAsync(document, position - 1, cancellationToken).ConfigureAwait(false);

            if (leftOfPosition.HasValue &&
                position <= leftOfPosition.Value.RightSpan.End &&
                position > leftOfPosition.Value.RightSpan.Start)
            {
                // Found a valid pair on the left of us.
                return (leftOfPosition, rightOfPosition);
            }
            
            // No valid pair of braces on the left of us.
            return (leftOfPosition: null, rightOfPosition);
        }

        public static async Task<TextSpan?> FindMatchingSpanAsync(
            this IBraceMatchingService service,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var (bracesLeftOfPosition, bracesRightOfPosition) = await service.GetAllMatchingBracesAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            // Favor matches where the position is on the outside boundary of the braces. i.e. if we
            // have:  {}^()
            //
            // then this would return the  ()  not the  {}
            return bracesRightOfPosition?.RightSpan ?? bracesLeftOfPosition?.LeftSpan;
        }
    }
}
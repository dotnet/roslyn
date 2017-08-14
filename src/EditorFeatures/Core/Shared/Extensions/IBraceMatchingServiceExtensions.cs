// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IBraceMatchingServiceExtensions
    {
        public static async Task<TextSpan?> FindMatchingSpanAsync(
            this IBraceMatchingService service,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            // These are the matching spans when checking the token to the right of the position.
            var braces1 = await service.GetMatchingBracesAsync(document, position, cancellationToken).ConfigureAwait(false);

            // These are the matching spans when checking the token to the left of the position.
            BraceMatchingResult? braces2 = null;

            // Ensure caret is valid at left of position.
            if (position > 0)
            {
                braces2 = await service.GetMatchingBracesAsync(document, position - 1, cancellationToken).ConfigureAwait(false);
            }

            // Favor matches where the position is on the outside boundary of the braces. i.e. if we
            // have:  {^()}  
            //
            // then this would return the  ()  not the  {}
            if (braces1.HasValue && position >= braces1.Value.LeftSpan.Start && position < braces1.Value.LeftSpan.End)
            {
                // ^{ } -- return right span
                return braces1.Value.RightSpan;
            }
            else if (braces2.HasValue && position > braces2.Value.RightSpan.Start && position <= braces2.Value.RightSpan.End)
            {
                // { }^ -- return left span
                return braces2.Value.LeftSpan;
            }
            else if (braces2.HasValue && position > braces2.Value.LeftSpan.Start && position <= braces2.Value.LeftSpan.End)
            {
                // {^ } -- return right span
                return braces2.Value.RightSpan;
            }
            else if (braces1.HasValue && position >= braces1.Value.RightSpan.Start && position < braces1.Value.RightSpan.End)
            {
                // { ^} - return left span
                return braces1.Value.LeftSpan;
            }

            return null;
        }
    }
}

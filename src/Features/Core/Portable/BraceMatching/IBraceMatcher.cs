// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.BraceMatching;

internal interface IBraceMatcher
{
    /// <summary>
    /// Given a <paramref name="document"/> and a <paramref name="position"/> within that document, gets the <see
    /// cref="BraceMatchingResult"/> if the position is at the start or end character of a matching pair of braces.
    /// Importantly, the <paramref name="position"/> is the position of the actual character to examine.  For
    /// example, given: <c>Goo()$$[1, 2, 3]</c> (where <c>$$</c> is the position), this would only be considering
    /// the <c>[</c> brace, not the <c>)</c> brace that precedes it.  Similarly, for <c>Goo()[1, 2, 3$$]</c> this
    /// would be considering the <c>]</c> brace.  If <c>Goo()[1, 2, 3]$$</c> were passed, no braces should be
    /// reported, despite the position being at the end of a brace.
    /// <para>
    /// It is the job of the calling feature ("Brace Matching") to actually make multiple calls into these matchers
    /// to then determine what to do.  For example with <c>Goo(true)$$[1, 2, 3]</c> (where $$ is now the caret
    /// position of the user), the feature will make two calls in, one for <c>Goo(true$$)[1, 2, 3]</c> and one for
    /// <c>Goo(true)$$[1, 2, 3]</c>.  This will allow it to see that the caret is between two complete brace pairs,
    /// and it can highlight both.  The <see cref="IBraceMatcher"/> does not have to consider this, or try to pick
    /// which set of braces to return.
    /// </para>
    /// </summary>
    Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken);
}

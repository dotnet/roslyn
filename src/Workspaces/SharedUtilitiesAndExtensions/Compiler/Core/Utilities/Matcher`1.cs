// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

/// <summary>
/// Helper class to allow one to do simple regular expressions over a sequence of objects (as
/// opposed to a sequence of characters).
/// </summary>
internal abstract partial class Matcher<T>
{
    // Tries to match this matcher against the provided sequence at the given index.  If the
    // match succeeds, 'true' is returned, and 'index' points to the location after the match
    // ends.  If the match fails, then false it returned and index remains the same.  Note: the
    // matcher does not need to consume to the end of the sequence to succeed.
    public abstract bool TryMatch(IList<T> sequence, ref int index);

    internal static Matcher<T> Repeat(Matcher<T> matcher)
        => new RepeatMatcher(matcher);

    internal static Matcher<T> OneOrMore(Matcher<T> matcher)
    {
        // m+ is the same as (m m*)
        return Sequence(matcher, Repeat(matcher));
    }

    internal static Matcher<T> Choice(params Matcher<T>[] matchers)
        => new ChoiceMatcher(matchers);

    internal static Matcher<T> Sequence(params Matcher<T>[] matchers)
        => new SequenceMatcher(matchers);

    internal static Matcher<T> Single(Func<T, bool> predicate, string description)
        => new SingleMatcher(predicate, description);
}

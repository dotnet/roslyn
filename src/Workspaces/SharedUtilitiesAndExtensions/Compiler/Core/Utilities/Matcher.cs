// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class Matcher
    {
        /// <summary>
        /// Matcher equivalent to (m*)
        /// </summary>
        public static Matcher<T> Repeat<T>(Matcher<T> matcher)
            => Matcher<T>.Repeat(matcher);

        /// <summary>
        /// Matcher equivalent to (m+)
        /// </summary>
        public static Matcher<T> OneOrMore<T>(Matcher<T> matcher)
            => Matcher<T>.OneOrMore(matcher);

        /// <summary>
        /// Matcher equivalent to (m_1|m_2|...|m_n)
        /// </summary>
        public static Matcher<T> Choice<T>(params Matcher<T>[] matchers)
            => Matcher<T>.Choice(matchers);

        /// <summary>
        /// Matcher equivalent to (m_1 ... m_n)
        /// </summary>
        public static Matcher<T> Sequence<T>(params Matcher<T>[] matchers)
            => Matcher<T>.Sequence(matchers);

        /// <summary>
        /// Matcher that matches an element if the provide predicate returns true.
        /// </summary>
        public static Matcher<T> Single<T>(Func<T, bool> predicate, string description)
            => Matcher<T>.Single(predicate, description);
    }
}

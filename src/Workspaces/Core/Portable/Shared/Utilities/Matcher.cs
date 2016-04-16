// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class Matcher
    {
        /// <summary>
        /// Matcher equivalent to (m*)
        /// </summary>
        public static Matcher<T> Repeat<T>(Matcher<T> matcher)
        {
            return Matcher<T>.Repeat(matcher);
        }

        /// <summary>
        /// Matcher equivalent to (m+)
        /// </summary>
        public static Matcher<T> OneOrMore<T>(Matcher<T> matcher)
        {
            return Matcher<T>.OneOrMore(matcher);
        }

        /// <summary>
        /// Matcher equivalent to (m_1|m_2|...|m_n)
        /// </summary>
        public static Matcher<T> Choice<T>(params Matcher<T>[] matchers)
        {
            return Matcher<T>.Choice(matchers);
        }

        /// <summary>
        /// Matcher equivalent to (m_1 ... m_n)
        /// </summary>
        public static Matcher<T> Sequence<T>(params Matcher<T>[] matchers)
        {
            return Matcher<T>.Sequence(matchers);
        }

        /// <summary>
        /// Matcher that matches an element if the provide predicate returns true.
        /// </summary>
        public static Matcher<T> Single<T>(Func<T, bool> predicate, string description)
        {
            return Matcher<T>.Single(predicate, description);
        }
    }
}

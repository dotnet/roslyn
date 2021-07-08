// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Roslyn.Test.Utilities
{
    public static class EqualityUtil
    {
        public static void RunAll<T>(
            Func<T, T, bool> compEqualsOperator,
            Func<T, T, bool> compNotEqualsOperator,
            params EqualityUnit<T>[] values)
        {
            var util = new EqualityUtil<T>(values, compEqualsOperator, compNotEqualsOperator);
            util.RunAll();
        }

        public static void RunAll<T>(EqualityUnit<T> unit, bool checkIEquatable = true)
        {
            RunAll(checkIEquatable, new[] { unit });
        }

        public static void RunAll<T>(params EqualityUnit<T>[] values)
        {
            RunAll(checkIEquatable: true, values: values);
        }

        public static void RunAll<T>(bool checkIEquatable, params EqualityUnit<T>[] values)
        {
            var util = new EqualityUtil<T>(values);
            util.RunAll(checkIEquatable);
        }
    }
}

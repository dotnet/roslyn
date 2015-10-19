// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}

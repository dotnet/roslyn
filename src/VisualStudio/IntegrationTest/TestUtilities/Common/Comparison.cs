// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    internal static class Comparison
    {
        public static bool AreStringValuesEqual(string str1, string str2)
            => string.IsNullOrEmpty(str1) == string.IsNullOrEmpty(str2)
            || str1 == str2;
    }
}

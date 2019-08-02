// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace CSharpSyntaxGenerator
{
    internal static class Utils
    {
        public static bool IsTrue(string val)
            => val != null && string.Compare(val, "true", true) == 0;
    }
}

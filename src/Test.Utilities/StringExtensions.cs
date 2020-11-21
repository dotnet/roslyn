// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Test.Utilities
{
    public static class StringExtensions
    {
        public static string NormalizeLineEndings(this string text)
        {
            return text.Replace(Environment.NewLine, "\r\n");
        }
    }
}

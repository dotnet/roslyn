// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal static class Util
    {
        internal static byte[] ToUtf8WithNullTerminator(string text)
        {
            if (text == null)
            {
                return null;
            }

            // SQLite expects null terminated UTF8, so append an extra null terminator
            return Encoding.UTF8.GetBytes(text + "\0");
        }
    }
}

﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class StringTextTest_BigEndianUnicode : StringTextTest_Default
    {
        protected override SourceText Create(string source)
        {
            byte[] buffer = GetBytes(Encoding.BigEndianUnicode, source);
            using (var stream = new MemoryStream(buffer, 0, buffer.Length, writable: false, publiclyVisible: true))
            {
                return EncodedStringText.Create(stream);
            }
        }
    }
}

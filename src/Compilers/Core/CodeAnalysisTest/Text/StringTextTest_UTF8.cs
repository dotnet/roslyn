// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class StringTextTest_UTF8 : StringTextTest_Default
    {
        protected override SourceText Create(string source)
        {
            byte[] buffer = GetBytes(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), source);
            using (var stream = new MemoryStream(buffer, 0, buffer.Length, writable: false, publiclyVisible: true))
            {
                return EncodedStringText.Create(stream);
            }
        }
    }
}

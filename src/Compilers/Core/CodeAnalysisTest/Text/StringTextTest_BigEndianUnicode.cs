﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Xunit;

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

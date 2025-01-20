// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public static class EncodingTestHelpers
{
    public static IEnumerable<Encoding?> GetEncodings()
    {
#if NET
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        yield return null;
        yield return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        yield return new UTF32Encoding(bigEndian: true, byteOrderMark: false);
        yield return new UTF32Encoding(bigEndian: true, byteOrderMark: true);
        yield return new UTF32Encoding(bigEndian: false, byteOrderMark: false);
        yield return new UnicodeEncoding(bigEndian: true, byteOrderMark: false);
        yield return new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
#if NET
        foreach (var info in CodePagesEncodingProvider.Instance.GetEncodings())
        {
            yield return info.GetEncoding();
        }
#else
            yield return Encoding.ASCII;
            yield return Encoding.GetEncoding("SJIS");
            yield return Encoding.GetEncoding(1250);
#endif
    }

    public static IEnumerable<object?[]> GetEncodingTestCases()
        => GetEncodings().Select(e => new object?[] { e });

    public static void AssertEncodingsEqual(Encoding? expected, Encoding? actual)
    {
        if (expected == null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.NotNull(actual);

            Assert.Equal(expected.CodePage, actual!.CodePage);
            Assert.Equal(expected.WebName, actual.WebName);
            Assert.Equal(expected.GetPreamble(), actual.GetPreamble());
            Assert.Equal(expected, actual);
        }
    }
}

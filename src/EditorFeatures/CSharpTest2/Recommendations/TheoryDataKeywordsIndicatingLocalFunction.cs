// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    internal class TheoryDataKeywordsIndicatingLocalFunction : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "extern" };
            yield return new object[] { "static extern" };
            yield return new object[] { "extern static" };
            yield return new object[] { "async" };
            yield return new object[] { "static async" };
            yield return new object[] { "async static" };
            yield return new object[] { "unsafe" };
            yield return new object[] { "static unsafe" };
            yield return new object[] { "unsafe static" };
            yield return new object[] { "async unsafe" };
            yield return new object[] { "unsafe async" };
            yield return new object[] { "unsafe extern" };
            yield return new object[] { "extern unsafe" };
            yield return new object[] { "extern unsafe async static" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class TheoryDataKeywordsIndicatingLocalFunctionWithoutAsync : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "extern" };
            yield return new object[] { "static extern" };
            yield return new object[] { "extern static" };
            yield return new object[] { "unsafe" };
            yield return new object[] { "static unsafe" };
            yield return new object[] { "unsafe static" };
            yield return new object[] { "unsafe extern" };
            yield return new object[] { "extern unsafe" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class TheoryDataKeywordsIndicatingLocalFunctionWithAsync : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "async" };
            yield return new object[] { "static async" };
            yield return new object[] { "async static" };
            yield return new object[] { "async unsafe" };
            yield return new object[] { "unsafe async" };
            yield return new object[] { "extern unsafe async static" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SpecialTypeTests
    {
        [Fact]
        public void ExtendedSpecialType_ToString()
        {
            AssertEx.Equal("0", ((ExtendedSpecialType)SpecialType.None).ToString());
            AssertEx.Equal("System_Object", ((ExtendedSpecialType)1).ToString());
            AssertEx.Equal("System_Runtime_CompilerServices_InlineArrayAttribute", ((ExtendedSpecialType)SpecialType.Count).ToString());
            AssertEx.Equal("System_ReadOnlySpan_T", ((ExtendedSpecialType)InternalSpecialType.First).ToString());
            AssertEx.Equal("System_ReadOnlySpan_T", ((ExtendedSpecialType)InternalSpecialType.System_ReadOnlySpan_T).ToString());
            AssertEx.Equal("System_Runtime_InteropServices_ExtendedLayoutKind", ((ExtendedSpecialType)(InternalSpecialType.NextAvailable - 1)).ToString());
            AssertEx.Equal("60", ((ExtendedSpecialType)InternalSpecialType.NextAvailable).ToString());
        }
    }
}

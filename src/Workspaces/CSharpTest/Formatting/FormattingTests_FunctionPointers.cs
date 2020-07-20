// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    [Trait(Traits.Feature, Traits.Features.Formatting)]
    public class FormattingTests_FunctionPointers : CSharpFormattingTestBase
    {
        [Fact]
        public async Task FormatFunctionPointer()
        {
            // TODO(https://github.com/dotnet/roslyn/issues/44312): add a space after the "int"s in the baseline and make this test still pass
            var content = @"
unsafe class C
{
    delegate * < int,  int> functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate*<int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task FormatFunctionPointerWithCallingConvention()
        {
            // TODO(https://github.com/dotnet/roslyn/issues/44312): add a space after the "int"s in the baseline and make this test still pass
            var content = @"
unsafe class C
{
    delegate *cdecl < int,  int> functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate* cdecl<int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }
    }
}

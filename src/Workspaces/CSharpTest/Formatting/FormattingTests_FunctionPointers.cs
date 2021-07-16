// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            var content = @"
unsafe class C
{
    delegate * < int ,  int > functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate*<int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task FormatFunctionPointerWithManagedCallingConvention()
        {
            var content = @"
unsafe class C
{
    delegate *managed < int ,  int > functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate* managed<int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task FormatFunctionPointerWithUnmanagedCallingConvention()
        {
            var content = @"
unsafe class C
{
    delegate *unmanaged < int ,  int > functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate* unmanaged<int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task FormatFunctionPointerWithUnmanagedCallingConventionAndSpecifiers()
        {
            var content = @"
unsafe class C
{
    delegate *unmanaged [ Cdecl ,  Thiscall ] < int ,  int > functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate* unmanaged[Cdecl, Thiscall]<int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task FormatFunctionPointerWithUnrecognizedCallingConvention()
        {
            var content = @"
unsafe class C
{
    delegate *invalid < int ,  int > functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate*invalid <int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact]
        public async Task FormatFunctionPointerWithInvalidCallingConventionAndSpecifiers()
        {
            var content = @"
unsafe class C
{
    delegate *invalid [ Cdecl ,  Thiscall ] < int ,  int > functionPointer;
}";

            var expected = @"
unsafe class C
{
    delegate*invalid [Cdecl, Thiscall]<int, int> functionPointer;
}";

            await AssertFormatAsync(expected, content);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingTests_FunctionPointers : CSharpFormattingTestBase
{
    [Fact]
    public async Task FormatFunctionPointer()
    {
        await AssertFormatAsync(@"
unsafe class C
{
    delegate*<int, int> functionPointer;
}", @"
unsafe class C
{
    delegate * < int ,  int > functionPointer;
}");
    }

    [Fact]
    public async Task FormatFunctionPointerWithManagedCallingConvention()
    {
        await AssertFormatAsync(@"
unsafe class C
{
    delegate* managed<int, int> functionPointer;
}", @"
unsafe class C
{
    delegate *managed < int ,  int > functionPointer;
}");
    }

    [Fact]
    public async Task FormatFunctionPointerWithUnmanagedCallingConvention()
    {
        await AssertFormatAsync(@"
unsafe class C
{
    delegate* unmanaged<int, int> functionPointer;
}", @"
unsafe class C
{
    delegate *unmanaged < int ,  int > functionPointer;
}");
    }

    [Fact]
    public async Task FormatFunctionPointerWithUnmanagedCallingConventionAndSpecifiers()
    {
        await AssertFormatAsync(@"
unsafe class C
{
    delegate* unmanaged[Cdecl, Thiscall]<int, int> functionPointer;
}", @"
unsafe class C
{
    delegate *unmanaged [ Cdecl ,  Thiscall ] < int ,  int > functionPointer;
}");
    }

    [Fact]
    public async Task FormatFunctionPointerWithUnrecognizedCallingConvention()
    {
        await AssertFormatAsync(@"
unsafe class C
{
    delegate*invalid <int, int> functionPointer;
}", @"
unsafe class C
{
    delegate *invalid < int ,  int > functionPointer;
}");
    }

    [Fact]
    public async Task FormatFunctionPointerWithInvalidCallingConventionAndSpecifiers()
    {
        await AssertFormatAsync(@"
unsafe class C
{
    delegate*invalid [Cdecl, Thiscall]<int, int> functionPointer;
}", @"
unsafe class C
{
    delegate *invalid [ Cdecl ,  Thiscall ] < int ,  int > functionPointer;
}");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public class RawStringLiteralParsingTests : ParsingTests
    {
        public RawStringLiteralParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestInFieldInitializer()
        {
            CreateCompilation(
@"class C
{
    string s = """""" """"""; 
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestInConstantFieldInitializer1()
        {
            CreateCompilation(
@"class C
{
    const string s = """""" """"""; 
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestInConstantFieldInitializer2()
        {
            CreateCompilation(
@"class C
{
    const string s = """""" """""" + 'a'; 
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestInConstantFieldInitializer3()
        {
            CreateCompilation(
@"class C
{
    const string s = 'a' + """""" """"""; 
}").VerifyDiagnostics();
        }
    }
}

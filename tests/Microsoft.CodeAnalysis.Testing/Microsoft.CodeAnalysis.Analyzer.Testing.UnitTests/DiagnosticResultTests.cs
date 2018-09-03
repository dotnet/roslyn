// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DiagnosticResultTests
    {
        private static DiagnosticResult CompilerError
            => DiagnosticResult.CompilerError("CS1002").WithLocation(1, 1).WithMessage("; expected");

        [Fact]
        public void TestToString()
        {
            Assert.Equal(
                "?(1,1): error CS1002: ; expected",
                CompilerError.ToString());
        }

        [Fact]
        public void TestToStringWithSpan()
        {
            Assert.Equal(
                "?(1,3,2,4): error CS1002",
                DiagnosticResult.CompilerError("CS1002").WithSpan(1, 3, 2, 4).ToString());
        }

        [Fact]
        public void TestToStringSeverity()
        {
            Assert.Equal(
                "?(1,1): error CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Error).ToString());
            Assert.Equal(
                "?(1,1): warning CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Warning).ToString());
            Assert.Equal(
                "?(1,1): info CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Info).ToString());
            Assert.Equal(
                "?(1,1): hidden CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Hidden).ToString());
        }

        [Fact]
        public void TestToStringWithoutLocation()
        {
            Assert.Equal(
                "error CS1002: ; expected",
                DiagnosticResult.CompilerError("CS1002").WithMessage("; expected").ToString());
        }

        [Fact]
        public void TestToStringWithoutMessage()
        {
            Assert.Equal(
                "?(1,1): error CS1002",
                DiagnosticResult.CompilerError("CS1002").WithLocation(1, 1).ToString());
        }

        [Fact]
        public void TestToStringWithoutLocationOrMessage()
        {
            Assert.Equal(
                "error CS1002",
                DiagnosticResult.CompilerError("CS1002").ToString());
        }

        [Fact]
        public void TestToStringWithMessageFormat()
        {
            Assert.Equal(
                "error CS1002: {0} expected",
                DiagnosticResult.CompilerError("CS1002").WithMessageFormat("{0} expected").ToString());
            Assert.Equal(
                "error CS1002: ; expected",
                DiagnosticResult.CompilerError("CS1002").WithMessageFormat("{0} expected").WithArguments(";").ToString());
        }
    }
}

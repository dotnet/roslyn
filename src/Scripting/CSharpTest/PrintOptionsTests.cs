// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests
{
    public class PrintOptionsTests : ObjectFormatterTestBase
    {
        private static readonly ObjectFormatter s_formatter = new TestCSharpObjectFormatter();

        [Fact]
        public void NullOptions()
        {
            Assert.Throws<ArgumentNullException>(() => s_formatter.FormatObject("hello", options: null));
        }

        [Fact]
        public void InvalidNumberRadix()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrintOptions().NumberRadix = 3);
        }

        [Fact]
        public void InvalidMemberDisplayFormat()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrintOptions().MemberDisplayFormat = (MemberDisplayFormat)(-1));
        }

        [Fact]
        public void InvalidMaximumOutputLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrintOptions().MaximumOutputLength = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrintOptions().MaximumOutputLength = 0);
        }

        [Fact]
        public void ValidNumberRadix()
        {
            var formatter = new TestCSharpObjectFormatter(includeCodePoints: true);
            var options = new PrintOptions();

            options.NumberRadix = 10;
            Assert.Equal("10", formatter.FormatObject(10, options));
            Assert.Equal("int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }", formatter.FormatObject(new int[10], options));
            Assert.Equal(@"16 '\u0010'", formatter.FormatObject('\u0010', options));

            options.NumberRadix = 16;
            Assert.Equal("0x0000000a", formatter.FormatObject(10, options));
            Assert.Equal("int[0x0000000a] { 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000 }", formatter.FormatObject(new int[10], options));
            Assert.Equal(@"0x0010 '\u0010'", formatter.FormatObject('\u0010', options));
        }

        [Fact]
        public void ValidMemberDisplayFormat()
        {
            var options = new PrintOptions();

            options.MemberDisplayFormat = MemberDisplayFormat.Hidden;
            Assert.Equal("PrintOptions", s_formatter.FormatObject(options, options));

            options.MemberDisplayFormat = MemberDisplayFormat.SingleLine;
            Assert.Equal("PrintOptions { Ellipsis=\"...\", EscapeNonPrintableCharacters=true, MaximumOutputLength=1024, MemberDisplayFormat=SingleLine, NumberRadix=10 }", s_formatter.FormatObject(options, options));

            options.MemberDisplayFormat = MemberDisplayFormat.SeparateLines;
            Assert.Equal(@"PrintOptions {
  Ellipsis: ""..."",
  EscapeNonPrintableCharacters: true,
  MaximumOutputLength: 1024,
  MemberDisplayFormat: SeparateLines,
  NumberRadix: 10,
  _maximumOutputLength: 1024,
  _memberDisplayFormat: SeparateLines,
  _numberRadix: 10
}
", s_formatter.FormatObject(options, options));
        }

        [Fact]
        public void ValidEscapeNonPrintableCharacters()
        {
            var options = new PrintOptions();

            options.EscapeNonPrintableCharacters = true;
            Assert.Equal(@"""\t""", s_formatter.FormatObject("\t", options));
            Assert.Equal(@"'\t'", s_formatter.FormatObject('\t', options));

            options.EscapeNonPrintableCharacters = false;
            Assert.Equal("\"\t\"", s_formatter.FormatObject("\t", options));
            Assert.Equal("'\t'", s_formatter.FormatObject('\t', options));
        }

        [Fact]
        public void ValidMaximumOutputLength()
        {
            var options = new PrintOptions();

            options.MaximumOutputLength = 1;
            Assert.Equal("1...", s_formatter.FormatObject(123456, options));

            options.MaximumOutputLength = 2;
            Assert.Equal("12...", s_formatter.FormatObject(123456, options));

            options.MaximumOutputLength = 3;
            Assert.Equal("123...", s_formatter.FormatObject(123456, options));

            options.MaximumOutputLength = 4;
            Assert.Equal("1234...", s_formatter.FormatObject(123456, options));

            options.MaximumOutputLength = 5;
            Assert.Equal("12345...", s_formatter.FormatObject(123456, options));

            options.MaximumOutputLength = 6;
            Assert.Equal("123456", s_formatter.FormatObject(123456, options));

            options.MaximumOutputLength = 7;
            Assert.Equal("123456", s_formatter.FormatObject(123456, options));
        }

        [Fact]
        public void ValidEllipsis()
        {
            var options = new PrintOptions();
            options.MaximumOutputLength = 1;

            options.Ellipsis = ".";
            Assert.Equal("1.", s_formatter.FormatObject(123456, options));

            options.Ellipsis = "..";
            Assert.Equal("1..", s_formatter.FormatObject(123456, options));

            options.Ellipsis = "";
            Assert.Equal("1", s_formatter.FormatObject(123456, options));

            options.Ellipsis = null;
            Assert.Equal("1", s_formatter.FormatObject(123456, options));
        }
    }
}
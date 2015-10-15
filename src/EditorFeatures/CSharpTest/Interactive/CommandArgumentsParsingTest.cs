// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.Interactive
{
    public class CommandArgumentsParsingTest
    {
        [WpfFact]
        public void Path()
        {
            TestPath("", expected: null, validArg: true, readToEnd: true);
            TestPath("blah", expected: null, validArg: true, readToEnd: false);
            TestPath(@"""blah""", expected: "blah", validArg: true, readToEnd: true);
            TestPath(@"""b\l\a\h""", expected: @"b\l\a\h", validArg: true, readToEnd: true);
            TestPath(@"""b\l\a\h"" // comment", expected: @"b\l\a\h", validArg: true, readToEnd: true);

            TestPath("\t   \"x\tx\"  \t  \r\n  \t  ", expected: "x\tx", validArg: true, readToEnd: true);
            TestPath(@"""b\l\a\h", expected: @"b\l\a\h", validArg: false);
            TestPath(@"""blah" + "\r\n" + @"""", expected: "blah", validArg: false);
            TestPath(@"""blah""" + "\r\n" + @"""", expected: "blah", validArg: true, readToEnd: false);

            TestPath(@"""blah//foo", expected: "blah//foo", validArg: false);
            TestPath(@"""blah//foo""", expected: "blah//foo", validArg: true);
        }

        private void TestPath(string args, string expected, bool validArg, bool? readToEnd = null)
        {
            int i = 0;
            string actual;

            Assert.Equal(validArg, CommandArgumentsParser.ParsePath(args, ref i, out actual));
            Assert.Equal(expected, actual);

            if (readToEnd != null)
            {
                Assert.Equal(readToEnd, CommandArgumentsParser.ParseTrailingTrivia(args, ref i));
            }
        }
    }
}

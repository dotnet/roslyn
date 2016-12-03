// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    public abstract class ObjectFormatterTestBase
    {
        protected static PrintOptions SingleLineOptions => new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SingleLine };
        protected static PrintOptions SeparateLinesOptions => new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SeparateLines, MaximumOutputLength = int.MaxValue };
        protected static PrintOptions HiddenOptions => new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.Hidden };

        public void AssertMembers(string str, params string[] expected)
        {
            int i = 0;
            foreach (var line in str.Split(new[] { "\r\n  " }, StringSplitOptions.None))
            {
                if (i == 0)
                {
                    Assert.Equal(expected[i] + " {", line);
                }
                else if (i == expected.Length - 1)
                {
                    Assert.Equal(expected[i] + "\r\n}\r\n", line);
                }
                else
                {
                    Assert.Equal(expected[i] + ",", line);
                }

                i++;
            }

            Assert.Equal(expected.Length, i);
        }

        public string FilterDisplayString(string str)
        {
            str = Regex.Replace(str, @"Id = \d+", "Id = *");
            str = Regex.Replace(str, @"Id=\d+", "Id=*");
            str = Regex.Replace(str, @"Id: \d+", "Id: *");

            return str;
        }
    }
}

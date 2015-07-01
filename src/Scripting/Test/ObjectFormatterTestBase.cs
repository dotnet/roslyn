// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.UnitTests
{
    public abstract class ObjectFormatterTestBase
    {
        protected static readonly ObjectFormattingOptions s_hexa = new ObjectFormattingOptions(useHexadecimalNumbers: true);
        protected static readonly ObjectFormattingOptions s_memberList = new ObjectFormattingOptions(memberFormat: MemberDisplayFormat.List);
        protected static readonly ObjectFormattingOptions s_inline = new ObjectFormattingOptions(memberFormat: MemberDisplayFormat.Inline);

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
            str = System.Text.RegularExpressions.Regex.Replace(str, @"Id = \d+", "Id = *");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"Id=\d+", "Id=*");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"Id: \d+", "Id: *");

            return str;
        }
    }
}

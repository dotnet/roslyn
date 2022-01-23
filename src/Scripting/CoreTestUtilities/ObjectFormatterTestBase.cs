// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            foreach (var line in str.Split(new[] { Environment.NewLine + "  " }, StringSplitOptions.None))
            {
                if (i == 0)
                {
                    Assert.Equal(expected[i] + " {", line);
                }
                else if (i == expected.Length - 1)
                {
                    Assert.Equal(expected[i] + Environment.NewLine + "}" + Environment.NewLine, line);
                }
                else
                {
                    Assert.Equal(expected[i] + ",", line);
                }

                i++;
            }

            Assert.Equal(expected.Length, i);
        }
    }
}

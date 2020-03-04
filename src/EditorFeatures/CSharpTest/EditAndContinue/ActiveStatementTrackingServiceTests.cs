﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class ActiveStatementTrackingServiceTests : EditingTestBase
    {
        [Fact, WorkItem(846042, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846042")]
        public void MovedOutsideOfMethod1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Goo(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
    <AS:0>}</AS:0>

    static void Goo()
    {
        // tracking span moves to another method as the user types around it
        <TS:0>Goo(1);</TS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void MovedOutsideOfMethod2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Goo(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Goo(1);</AS:0>
    }

    static void Goo()
    {
        <TS:0>Goo(2);</TS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void MovedOutsideOfLambda1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Goo(1);</AS:0> };
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>}</AS:0>;
        <TS:0>Goo(1);</TS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void MovedOutsideOfLambda2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Goo(1);</AS:0> };
        Action b = () => { Goo(2); };
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Goo(1);</AS:0> };
        Action b = () => { <TS:0>Goo(2);</TS:0> };
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }
    }
}

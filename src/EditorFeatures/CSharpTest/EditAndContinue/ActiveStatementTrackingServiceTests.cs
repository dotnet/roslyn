// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EditAndContinue;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class ActiveStatementTrackingServiceTests : RudeEditTestBase
    {
        [Fact, WorkItem(846042)]
        public void MovedOutsideOfMethod1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Foo(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
    <AS:0>}</AS:0>

    static void Foo()
    {
        // tracking span moves to another method as the user types around it
        <TS:0>Foo(1);</TS:0>
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
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Foo(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Foo(1);</AS:0>
    }

    static void Foo()
    {
        <TS:0>Foo(2);</TS:0>
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
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Foo(1);</AS:0> };
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>}</AS:0>;
        <TS:0>Foo(1);</TS:0>
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
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Foo(1);</AS:0> };
        Action b = () => { Foo(2); };
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Foo(1);</AS:0> };
        Action b = () => { <TS:0>Foo(2);</TS:0> };
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }
    }
}

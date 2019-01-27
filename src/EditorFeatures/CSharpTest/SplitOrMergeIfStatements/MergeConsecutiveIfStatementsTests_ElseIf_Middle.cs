// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements
{
    public sealed partial class MergeConsecutiveIfStatementsTests
    {
        [Fact]
        public async Task MergedOnMiddleIfMergableWithNextOnly()
        {
            const string Initial =
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
            System.Console.WriteLine(null);
        [||]else if (b)
            System.Console.WriteLine();
        else if (c)
            System.Console.WriteLine();
    }
}";
            const string Expected =
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
            System.Console.WriteLine(null);
        else if (b || c)
            System.Console.WriteLine();
    }
}";

            await TestActionCountAsync(Initial, 1);
            await TestInRegularAndScriptAsync(Initial, Expected);
        }

        [Fact]
        public async Task MergedOnMiddleIfMergableWithPreviousOnly()
        {
            const string Initial =
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
            System.Console.WriteLine();
        [||]else if (b)
            System.Console.WriteLine();
        else if (c)
            System.Console.WriteLine(null);
    }
}";
            const string Expected =
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (c)
            System.Console.WriteLine(null);
    }
}";

            await TestActionCountAsync(Initial, 1);
            await TestInRegularAndScriptAsync(Initial, Expected);
        }

        [Fact]
        public async Task MergedOnMiddleIfMergableWithBoth()
        {
            const string Initial =
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
            System.Console.WriteLine();
        [||]else if (b)
            System.Console.WriteLine();
        else if (c)
            System.Console.WriteLine();
    }
}";
            const string Expected1 =
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (c)
            System.Console.WriteLine();
    }
}";
            const string Expected2 =
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
            System.Console.WriteLine();
        else if (b || c)
            System.Console.WriteLine();
    }
}";

            await TestActionCountAsync(Initial, 2);
            await TestInRegularAndScriptAsync(Initial, Expected1, index: 0);
            await TestInRegularAndScriptAsync(Initial, Expected2, index: 1);
        }
    }
}

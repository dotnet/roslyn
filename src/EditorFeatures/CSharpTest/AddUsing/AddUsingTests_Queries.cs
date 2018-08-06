﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing
{
    public partial class AddUsingTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestSimpleQuery()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var q = [|from x in args
                select x|]}
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q = from x in args
                select x}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestSimpleWhere()
        {
            await TestInRegularAndScriptAsync(
@"class Test
{
    public void SimpleWhere()
    {
        int[] numbers = {
            1,
            2,
            3
        };
        var lowNums = [|from n in numbers
                      where n < 5
                      select n|];
    }
}",
@"using System.Linq;

class Test
{
    public void SimpleWhere()
    {
        int[] numbers = {
            1,
            2,
            3
        };
        var lowNums = from n in numbers
                      where n < 5
                      select n;
    }
}");
        }
    }
}

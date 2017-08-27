// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    public partial class CSharpAsAndNullCheckTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task FixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        string a;
        int[] b;
        {|FixAllInDocument:var|} x = o as string;
        if (x != null)
        {
        }

        var y = o as string;
        if (y != null)
        {
        }

        if ((a = o as string) != null)
        {
        }

        if ((b = o as int[]) != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x)
        {
        }

        if (o is string y)
        {
        }

        if (o is string a)
        {
        }

        if (o is int[] b)
        {
        }
    }
}");
        }
    }
}

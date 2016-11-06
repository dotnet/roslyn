// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.UseCoalesceExpression;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCoalesceExpression
{
    public class UseCoalesceExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseCoalesceExpressionDiagnosticAnalyzer(),
                new UseCoalesceExpressionCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestOnLeft_Equals()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||]x == null ? y : x;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestOnLeft_NotEquals()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||]x != null ? x : y;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestOnRight_Equals()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||]null == x ? y : x;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestOnRight_NotEquals()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||]null != x ? x : y;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestComplexExpression()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||]x.ToString() == null ? y : x.ToString();
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x.ToString() ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestParens1()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||](x == null) ? y : x;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestParens2()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||](x) == null ? y : x;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestParens3()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||]x == null ? y : (x);
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestParens4()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = [||]x == null ? (y) : x;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestFixAll1()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y)
    {
        var z1 = {|FixAllInDocument:x|} == null ? y : x;
        var z2 = x != null ? x : y;
    }
}",
@"using System;

class C
{
    void M(string x, string y)
    {
        var z1 = x ?? y;
        var z2 = x ?? y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestFixAll2()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y, string z)
    {
        var w = {|FixAllInDocument:x|} != null ? x : y.ToString(z != null ? z : y);
    }
}",
@"using System;

class C
{
    void M(string x, string y, string z)
    {
        var w = x ?? y.ToString(z ?? y);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestFixAll3()
        {
            await TestAsync(
@"using System;

class C
{
    void M(string x, string y, string z)
    {
        var w = {|FixAllInDocument:x|} != null ? x : y != null ? y : z;
    }
}",
@"using System;

class C
{
    void M(string x, string y, string z)
    {
        var w = x ?? y ?? z;
    }
}");
        }
    }
}
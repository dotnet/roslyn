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
    public class UseCoalesceExpressionForNullableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseCoalesceExpressionForNullableDiagnosticAnalyzer(),
                new UseCoalesceExpressionForNullableCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
        public async Task TestOnLeft_Equals()
        {
            await TestAsync(
@"using System;

class C
{
    void M(int? x, int? y)
    {
        var z = [||]!x.HasValue ? y : x.Value;
    }
}",
@"using System;

class C
{
    void M(int? x, int? y)
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
    void M(int? x, int? y)
    {
        var z = [||]x.HasValue ? x.Value : y;
    }
}",
@"using System;

class C
{
    void M(int? x, int? y)
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
    void M(int? x, int? y)
    {
        var z = [||]!(x + y).HasValue ? y : (x + y).Value;
    }
}",
@"using System;

class C
{
    void M(int? x, int? y)
    {
        var z = (x + y) ?? y;
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
    void M(int? x, int? y)
    {
        var z = [||](x.HasValue) ? x.Value : y;
    }
}",
@"using System;

class C
{
    void M(int? x, int? y)
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
    void M(int? x, int? y)
    {
        var z1 = {|FixAllInDocument:x|}.HasValue ? x.Value : y;
        var z2 = !x.HasValue ? y : x.Value;
    }
}",
@"using System;

class C
{
    void M(int? x, int? y)
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
    void M(int? x, int? y, int? z)
    {
        var w = {|FixAllInDocument:x|}.HasValue ? x.Value : y.ToString(z.HasValue ? z.Value : y);
    }
}",
@"using System;

class C
{
    void M(int? x, int? y, int? z)
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
    void M(int? x, int? y, int? z)
    {
        var w = {|FixAllInDocument:x|}.HasValue ? x.Value : y.HasValue ? y.Value : z;
    }
}",
@"using System;

class C
{
    void M(int? x, int? y, int? z)
    {
        var w = x ?? y ?? z;
    }
}");
        }
    }
}
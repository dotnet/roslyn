// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIsNullCheck
{
    public partial class UseIsNullCheckForEqualityOperatorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseIsNullCheckForEqualityOperatorDiagnosticAnalyzer(), new CSharpUseIsNullCheckForEqualityOperatorCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestEquality()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](object)s == null)
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if (s is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestEqualitySwapped()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]null == (object)s)
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if (s is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotEquality()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](object)s != null)
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if (s is object)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotEqualitySwapped()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]null != (object)s)
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if (s is object)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestMissingPreCSharp7()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](object)s == null)
            return;
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOnlyForObjectCast()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](string)s == null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ({|FixAllInDocument:|}(object)s == null)
            return;

        if (null == (object)s)
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if (s is null)
            return;

        if (s is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFixAllNested1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s2)
    {
        if ({|FixAllInDocument:|}(object)((object)s2 == null) == null))
            return;
    }
}",
@"using System;

class C
{
    void M(string s2)
    {
        if ((object)(s2 is null) is null))
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ( /*1*/ [||]( /*2*/ object /*3*/ ) /*4*/ s /*5*/ == /*6*/ null /*7*/ )
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if ( /*1*/ s /*5*/ is /*6*/ null /*7*/ )
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestTrivia2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ( /*1*/ [||]null /*2*/ == /*3*/ ( /*4*/ object /*5*/ ) /*6*/ s /*7*/ )
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if ( /*1*/ s /*2*/ is /*3*/ null /*7*/ )
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestConstrainedTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M<T>(T s) where T : class
    {
        if ([||](object)s == null)
            return;
    }
}",
@"using System;

class C
{
    void M<T>(T s) where T : class
    {
        if (s is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestUnconstrainedTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M<T>(T s)
    {
        if ([||](object)s == null)
            return;
    }
}",
@"using System;

class C
{
    void M<T>(T s)
    {
        if ((object)s is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestComplexExpr()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string[] s)
    {
        if ([||](object)s[0] == null)
            return;
    }
}",
@"using System;

class C
{
    void M(string[] s)
    {
        if (s[0] is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotOnDefault()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string[] s)
    {
        if ([||](object)s[0] == default)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNoCast()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(C s)
    {
        if ([||]s == null)
            return;
    }
}",
@"using System;

class C
{
    void M(C s)
    {
        if (s is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNoCastReversed()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(C s)
    {
        if ([||]s != null)
            return;
    }
}",
@"using System;

class C
{
    void M(C s)
    {
        if (s is object)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOnlyForObjectOperator()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]s == null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestPreserveNonObjectCast()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{

    void M(int i)
    {
        if ([||](C)i == null)
            return;
    }

    public static explicit operator C(int i)
        => new C();
}",
@"using System;

class C
{

    void M(int i)
    {
        if ((C)i is null)
            return;
    }

    public static explicit operator C(int i)
        => new C();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestValueTypeConstraint()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M<T>(T t) where T : struct
    {
        if ([||](object)t == null)
            return;
    }
}",
@"using System;

class C
{
    void M<T>(T t) where T : struct
    {
        if ((object)t is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestValueType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int i)
    {
        if ([||](object)i == null)
            return;
    }
}",
@"using System;

class C
{
    void M(int i)
    {
        if ((object)i is null)
            return;
    }
}");
        }
    }
}

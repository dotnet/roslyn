// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIsNullCheck
{
    public partial class UseIsNullCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseIsNullCheckDiagnosticAnalyzer(), new CSharpUseIsNullCheckCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestIdentifierName()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]ReferenceEquals(s, null))
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
        public async Task TestBuiltInType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if (object.[||]ReferenceEquals(s, null))
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
        public async Task TestNamedType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if (Object.[||]ReferenceEquals(s, null))
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
        public async Task TestReversed()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]ReferenceEquals(null, s))
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
        public async Task TestNegated()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if (![||]ReferenceEquals(null, s))
            return;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        if (!(s is null))
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotInCSharp6()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]ReferenceEquals(null, s))
            return;
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s1, string s2)
    {
        if ({|FixAllInDocument:ReferenceEquals|}(s1, null) ||
            ReferenceEquals(s2, null))
            return;
    }
}",
@"using System;

class C
{
    void M(string s1, string s2)
    {
        if (s1 is null ||
            s2 is null)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s1, string s2)
    {
        if (ReferenceEquals(s1, null) ||
            {|FixAllInDocument:ReferenceEquals|}(s2, null))
            return;
    }
}",
@"using System;

class C
{
    void M(string s1, string s2)
    {
        if (s1 is null ||
            s2 is null)
            return;
    }
}");
        }
    }
}

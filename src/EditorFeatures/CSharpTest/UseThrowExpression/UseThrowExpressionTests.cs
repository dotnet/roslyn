// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseThrowExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.UseThrowExpression;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseThrowExpression
{
    public partial class UseThrowExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseThrowExpressionDiagnosticAnalyzer(),
                new UseThrowExpressionCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task WithoutBraces()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s)
    {
        if (s == null) [|throw|] new ArgumentNullException(nameof(s));
        _s = s;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task TestOnIf()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s)
    {
        [|if|] (s == null) throw new ArgumentNullException(nameof(s));
        _s = s;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task WithBraces()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s)
    {
        if (s == null) { [|throw|] new ArgumentNullException(nameof(s)); }
        _s = s;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task TestNotOnAssign()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    void M(string s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        _s = [|s|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task OnlyInCSharp7AndHigher()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    void M(string s)
    {
        if (s == null) { [|throw|] new ArgumentNullException(nameof(s)) };
        _s = s;
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task WithIntermediaryStatements()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s, string t)
    {
        if (s == null) { [|throw|] new ArgumentNullException(nameof(s)); }
        if (t == null) { throw new ArgumentNullException(nameof(t)); }
        _s = s;
    }
}",
@"using System;

class C
{
    void M(string s, string t)
    {
        if (t == null) { throw new ArgumentNullException(nameof(t)); }
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task NotWithIntermediaryWrite()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    void M(string s, string t)
    {
        if (s == null) { [|throw|] new ArgumentNullException(nameof(s)) };
        s = ""something"";
        _s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task TestNullCheckOnLeft()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s)
    {
        if (null == s) [|throw|] new ArgumentNullException(nameof(s));
        _s = s;
    }
}",
@"using System;

class C
{
    void M(string s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task TestWithLocal()
        {
            await TestAsync(
@"
using System;

class C
{
    void M()
    {
        string s = null;
        if (null == s) [|throw|] new ArgumentNullException(nameof(s));
        _s = s;
    }
}",
@"using System;

class C
{
    void M()
    {
        string s = null;
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task TestNotOnField()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    string s;

    void M()
    {
        if (null == s) [|throw|] new ArgumentNullException(nameof(s));
        _s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task TestAssignBeforeCheck()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    void M(string s)
    {
        _s = s;
        if (s == null) [|throw|] new ArgumentNullException(nameof(s));
    }
}");
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SimplifyConditional;
using Microsoft.CodeAnalysis.CSharp.UseLocalFunction;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyConditional
{
    public partial class SimplifyConditionalTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyConditionalDiagnosticAnalyzer(), new CSharpSimplifyConditionalCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleCase()
        {
            await TestInRegularAndScript1Async(
@"using System;

using System;

class C
{
    bool M()
    {
        return [|X() && Y() ? true : false|];
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}",
@"using System;

using System;

class C
{
    bool M()
    {
        return X() && Y();
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleNegatedCase()
        {
            await TestInRegularAndScript1Async(
@"using System;

using System;

class C
{
    bool M()
    {
        return [|X() && Y() ? false : true|];
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}",
@"using System;

using System;

class C
{
    bool M()
    {
        return !X() || !Y();
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMustBeBool1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

using System;

class C
{
    string M()
    {
        return [|X() && Y() ? """" : null|];
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMustBeBool2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

using System;

class C
{
    string M()
    {
        return [|X() && Y() ? null : """"|];
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotWithTrueTrue()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

using System;

class C
{
    bool M()
    {
        return [|X() && Y() ? true : true|];
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotWithFalseFalse()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

using System;

class C
{
    bool M()
    {
        return [|X() && Y() ? false : false|];
    }

    private bool X() => throw new NotImplementedException();
    private bool Y() => throw new NotImplementedException();
}");
        }
    }
}

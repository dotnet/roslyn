// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.InlineDeclaration;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDeclaration
{
    public class CSharpInlineDeclarationTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpInlineDeclarationDiagnosticAnalyzer(),
                new CSharpInlineDeclarationCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task InlineVariable1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (int.TryParse(v, out i))
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task InlineVariablePreferVar1()
        {
            await TestAsync(
@"class C
{
    void M(string v)
    {
        [|int|] i;
        if (int.TryParse(v, out i))
        {
        } 
    }
}",
@"class C
{
    void M(string v)
    {
        if (int.TryParse(v, out var i))
        {
        } 
    }
}", options: UseImplicitTypeTests.ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestAvailableWhenWrittenAfter1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (int.TryParse(v, out i))
        {
        }
        i = 0;
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i))
        {
        }
        i = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingWhenWrittenBetween1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        i = 0;
        if (int.TryParse(v, out i))
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingWithInitializer()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i = 0;
        if (int.TryParse(v, out i))
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestAvailableInOuterScopeIfNotWrittenOutside()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i = 0;
        {
            if (int.TryParse(v, out i))
            {
            }
            i = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingIfWrittenAfterInOuterScope()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i = 0;
        {
            if (int.TryParse(v, out i))
            {
            }
        }
        i = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingIfWrittenBetweenInOuterScope()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i = 0;
        {
            i = 1;
            if (int.TryParse(v, out i))
            {
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingInNonOut()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (int.TryParse(v, i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingInField()
        {
            await TestMissingAsync(
@"class C
{
    [|int|] i;
    void M()
    {
        if (int.TryParse(v, out this.i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingInEmbeddedStatement()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        while (true)
            if (int.TryParse(v, out this.i))
            {
            } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestAvailabeInNestedBlock()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        while (true)
        {
            if (int.TryParse(v, out i))
            {
            } 
        }
    }
}",
@"class C
{
    void M()
    {
        while (true)
        {
            if (int.TryParse(v, out int i))
            {
            }
        } 
    }
}");
        }
    }
}
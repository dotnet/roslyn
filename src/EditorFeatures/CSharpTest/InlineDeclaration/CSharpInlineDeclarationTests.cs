// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.InlineDeclaration;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDeclaration
{
    public partial class CSharpInlineDeclarationTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
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
        public async Task InlineVariableWithConstructor1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (new C1(v, out i))
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (new C1(v, out int i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task InlineVariableMissingWithIndexer1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (this[out i])
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task InlineVariableIntoFirstOut1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (int.TryParse(v, out i, out i))
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i, out i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task InlineVariableIntoFirstOut2()
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
        if (int.TryParse(v, out i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingInCSharp6()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (int.TryParse(v, out i))
        {
        } 
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
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
        public async Task InlineVariablePreferVarExceptForPredefinedTypes1()
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
        if (int.TryParse(v, out int i))
        {
        } 
    }
}", options: UseImplicitTypeTests.ImplicitTypeButKeepIntrinsics());
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
        public async Task TestMissingWhenReadBetween1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i = 0;
        M1(i);
        if (int.TryParse(v, out i))
        {
        }
    }

    void M1(int i)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingWithComplexInitializer()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i = M1();
        if (int.TryParse(v, out i))
        {
        }
    }

    int M1()
    {
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
        public async Task TestMissingInField2()
        {
            await TestMissingAsync(
@"class C
{
    [|int|] i;
    void M()
    {
        if (int.TryParse(v, out i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingInNonLocalStatement()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        foreach ([|int|] i in e)
        {
            if (int.TryParse(v, out i))
            {
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestMissingInEmbeddedStatementWithWriteAfterwards()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        while (true)
            if (int.TryParse(v, out i))
            {
            } 
        i = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestInEmbeddedStatement()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        while (true)
            if (int.TryParse(v, out i))
            {
                i = 1;
            } 
    }
}",
@"class C
{
    void M()
    {
        while (true)
            if (int.TryParse(v, out int i))
            {
                i = 1;
            } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestAvailableInNestedBlock()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestOverloadResolutionDoNotUseVar1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (M2(out i))
        {
        } 
    }

    void M2(out int i)
    {
    }

    void M2(out string s)
    {
    }
}",
@"class C
{
    void M()
    {
        if (M2(out int i))
        {
        } 
    }

    void M2(out int i)
    {
    }

    void M2(out string s)
    {
    }
}", options: UseImplicitTypeTests.ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestOverloadResolutionDoNotUseVar2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|var|] i = 0;
        if (M2(out i))
        {
        } 
    }

    void M2(out int i)
    {
    }

    void M2(out string s)
    {
    }
}",
@"class C
{
    void M()
    {
        if (M2(out int i))
        {
        } 
    }

    void M2(out int i)
    {
    }

    void M2(out string s)
    {
    }
}", options: UseImplicitTypeTests.ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestGenericInferenceDoNotUseVar3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (M2(out i))
        {
        } 
    }

    void M2<T>(out T i)
    {
    }
}",
@"class C
{
    void M()
    {
        if (M2(out int i))
        {
        } 
    }

    void M2<T>(out T i)
    {
    }
}", options: UseImplicitTypeTests.ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        // prefix comment
        [|int|] i;
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
        {
            // prefix comment
            if (int.TryParse(v, out int i))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i; // suffix comment
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
        {
            // suffix comment
            if (int.TryParse(v, out int i))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        // prefix comment
        [|int|] i; // suffix comment
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
        {
            // prefix comment
            // suffix comment
            if (int.TryParse(v, out int i))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int [|i|] /*suffix*/, j;
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
        int j;
        {
            if (int.TryParse(v, out int i /*suffix*/))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments5()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int /*prefix*/ [|i|], j;
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
        int j;
        {
            if (int.TryParse(v, out int /*prefix*/ i))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments6()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int /*prefix*/ [|i|] /*suffix*/, j;
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
        int j;
        {
            if (int.TryParse(v, out int /*prefix*/ i /*suffix*/))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments7()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int j, /*prefix*/ [|i|] /*suffix*/;
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
        int j;
        {
            if (int.TryParse(v, out int /*prefix*/ i /*suffix*/))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments8()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        // prefix
        int j, [|i|]; // suffix
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
        // prefix
        int j; // suffix
        {
            if (int.TryParse(v, out int i))
            {
            }
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task TestComments9()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int /*int comment*/
            /*prefix*/ [|i|] /*suffix*/,
            j;
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
        int /*int comment*/
            j;
        {
            if (int.TryParse(v, out int /*prefix*/ i /*suffix*/))
            {
            }
        }
    }
}", compareTokens: false);
        }
    }
}
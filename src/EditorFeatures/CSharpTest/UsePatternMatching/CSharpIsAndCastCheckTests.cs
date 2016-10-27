// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    public partial class CSharpIsAndCastCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpIsAndCastCheckDiagnosticAnalyzer(),
                new CSharpIsAndCastCheckCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheck1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        } 
    }
}",
@"class C
{
    void M()
    {
        if (x is string v)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingInCSharp6()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        } 
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingInWrongName()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)y;
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingInWrongType()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (bool)x;
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnMultiVar()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            var [|v|] = (string)x, v1 = "";
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnNonDeclaration()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|v|] = (string)x;
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnAsExpression()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x as string)
        {
            [|var|] v = (string)x;
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexExpression1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if ((x ? y : z) is string)
        {
            [|var|] v = (string)(x ? y : z);
        } 
    }
}",
@"class C
{
    void M()
    {
        if ((x ? y : z) is string v)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestInlineTypeCheckWithElse()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        } 
        else
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (x is string v)
        {
        } 
        else
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            // prefix comment
            [|var|] v = (string)x;
        } 
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        if (x is string v)
        {
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x; // suffix comment
        } 
    }
}",
@"class C
{
    void M()
    {
        // suffix comment
        if (x is string v)
        {
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            // prefix comment
            [|var|] v = (string)x; // suffix comment
        } 
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        // suffix comment
        if (x is string v)
        {
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckParenthesized1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if ((x) is string)
        {
            [|var|] v = (string)x;
        } 
    }
}",
@"class C
{
    void M()
    {
        if ((x) is string v)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckParenthesized2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)(x);
        } 
    }
}",
@"class C
{
    void M()
    {
        if (x is string v)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckParenthesized3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = ((string)x);
        } 
    }
}",
@"class C
{
    void M()
    {
        if (x is string v)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeConflict1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        } 
        else
        {
            var v = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeConflict2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        }
 
        if (true)
        {
            var v = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeConflict3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            var v = (string)x;
        }
 
        if (x is bool)
        {
            [|var|] v = (bool)x;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeNonConflict1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        {
            if (x is string)
            {
                [|var|] v = ((string)x);
            }
        } 

        {
            var v = 1;
        }
    }
}",
@"class C
{
    void M()
    {
        {
            if (x is string v)
            {
            }
        } 

        {
            var v = 1;
        }
    }
}");
        }
    }
}
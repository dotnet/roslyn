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
    public partial class CSharpAsAndNullCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpAsAndNullCheckDiagnosticAnalyzer(),
                new CSharpAsAndNullCheckCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheck1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null)
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
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckInvertedCheck1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (null != x)
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
        [|var|] x = o as string;
        if (x != null)
        {
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
        [|var|] y = o as string;
        if (x != null)
        {
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
        [|y|] = o as string;
        if (x != null)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnIsExpression()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|var|] x = o is string;
        if (x != null)
        {
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
        [|var|] x = (o ? z : w) as string;
        if (x != null)
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if ((o ? z : w) is string x)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnNullEquality()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|var|] x = o is string;
        if (x == null)
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
        [|var|] x = o as string;
        if (null != x)
        {
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
        if (o is string x)
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
        // prefix comment
        [|var|] x = o as string;
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        if (o is string x)
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
        [|var|] x = o as string; // suffix comment
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        // suffix comment
        if (o is string x)
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
        // prefix comment
        [|var|] x = o as string; // suffix comment
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        // suffix comment
        if (o is string x)
        {
        }
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexCondition1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null ? 0 : 1)
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (o is string x ? 0 : 1)
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexCondition2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if ((x != null))
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if ((o is string x))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexCondition3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null && x.Length > 0)
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (o is string x && x.Length > 0)
        {
        } 
    }
}");
        }
    }
}
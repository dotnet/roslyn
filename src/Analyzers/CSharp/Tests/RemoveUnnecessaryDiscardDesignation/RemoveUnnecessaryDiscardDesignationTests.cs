// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryDiscardDesignation
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpRemoveUnnecessaryDiscardDesignationDiagnosticAnalyzer,
        CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider>;

    public class RemoveUnnecessaryDiscardDesignationTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestDeclarationPatternInSwitchStatement()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        switch (o)
        {
            case int [|_|]:
                break;
        }
    }
}",
                FixedCode = @"
class C
{
    void M(object o)
    {
        switch (o)
        {
            case int:
                break;
        }
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestNotInCSharp8()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        switch (o)
        {
            case int _:
                break;
        }
    }
}",
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestDeclarationPatternInSwitchExpression()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            int [|_|] => 0,
        };
    }
}",
                FixedCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            int => 0,
        };
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestDeclarationPatternInIfStatement()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        if (o is int [|_|]) { }
    }
}",
                FixedCode = @"
class C
{
    void M(object o)
    {
        if (o is int) { }
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestRecursivePropertyPattern()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            { } [|_|] => 0,
        };
    }
}",
                FixedCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            { } => 0,
        };
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestEmptyRecursiveParameterPattern()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            () [|_|] => 0,
        };
    }
}",
                FixedCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            () => 0,
        };
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestTwoElementRecursiveParameterPattern()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            (int i, int j) [|_|] => 0,
        };
    }
}",
                FixedCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            (int i, int j) => 0,
        };
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestNotWithOneElementRecursiveParameterPattern()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(object o)
    {
        var v = o switch
        {
            (int i) _ => 0,
        };
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
        public async Task TestNestedFixAll()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    void M(string o)
    {
        var v = o switch
        {
            { Length: int [|_|] } [|_|] => 1,
        };
    }
}",
                FixedCode = @"
class C
{
    void M(string o)
    {
        var v = o switch
        {
            { Length: int } => 1,
        };
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }
    }
}

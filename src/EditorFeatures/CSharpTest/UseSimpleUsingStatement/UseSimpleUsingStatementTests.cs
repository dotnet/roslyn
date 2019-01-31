// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSimpleUsingStatement
{
    public partial class UseSimpleUsingStatementTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseSimpleUsingStatementDiagnosticAnalyzer(), new UseSimpleUsingStatementCodeFixProvider());

        private static ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);
        private static ParseOptions CSharp8ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestAboveCSharp8()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b)
        {
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b;
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfOnSimpleUsingStatement()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        [||]using var a = b;
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingPriorToCSharp8()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b)
        {
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp72ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfExpressionUsing()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (a)
        {
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfCodeFollows()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b)
        {
        }
        Console.WriteLine();
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestAsyncUsing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        async [||]using (var a = b)
        {
        }
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        async using var a = b;
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestWithBlockBodyWithContents()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b)
        {
            Console.WriteLine(a);
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b;
        Console.WriteLine(a);
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestWithNonBlockBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b)
            Console.WriteLine(a);
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b;
        Console.WriteLine(a);
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestMultiUsing1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b)
        using (var c = d)
        {
            Console.WriteLine(a);
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b;
        using var c = d;
        Console.WriteLine(a);
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestMultiUsingOnlyOnTopmostUsing()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        using (var a = b)
        [||]using (var c = d)
        {
            Console.WriteLine(a);
        }
    }
}",
new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        {|FixAllInDocument:|}using (var a = b)
        {
            using (var c = d)
            {
                Console.WriteLine(a);
            }
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b;
        using var c = d;
        Console.WriteLine(a);
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        using (var a = b)
        {
            {|FixAllInDocument:|}using (var c = d)
            {
                Console.WriteLine(a);
            }
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b;
        using var c = d;
        Console.WriteLine(a);
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestWithFollowingReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b)
        {
        }
        return;
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b;
        return;
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestWithFollowingBreak()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        switch (0)
        {
            case 0:
                {
                    [||]using (var a = b)
                    {
                    }
                    break;
                }
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        switch (0)
        {
            case 0:
                {
                    using var a = b;
                    break;
                }
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingInSwitchSection()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        switch (0)
        {
            case 0:
                [||]using (var a = b)
                {
                }
                break;
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }
    }
}

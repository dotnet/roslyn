// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSimpleUsingStatement
{
    public partial class UseSimpleUsingStatementTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseSimpleUsingStatementDiagnosticAnalyzer(), new UseSimpleUsingStatementCodeFixProvider());

        private static readonly ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);
        private static readonly ParseOptions CSharp8ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestWithOptionOff()
        {
            await TestMissingInRegularAndScriptAsync(
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
new TestParameters(
    parseOptions: CSharp8ParseOptions,
    options: Option(CSharpCodeStyleOptions.PreferSimpleUsingStatement, CodeStyleOptions.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestMultiDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [||]using (var a = b, c = d)
        {
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        using var a = b, c = d;
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
            // not actually legal code.
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestAwaitUsing()
        {
            // not actually legal code.
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        await [||]using (var a = b)
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
        await using var a = b;
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
        public async Task TestFixAll3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        {|FixAllInDocument:|}using (var a = b)
        using (var c = d)
        {
            using (var e = f)
            using (var g = h)
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
        using var e = f;
        using var g = h;
        Console.WriteLine(a);
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestFixAll4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        {|FixAllInDocument:|}using (var a = b)
        using (var c = d)
        {
            using (e)
            using (f)
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
        using (e)
        using (f)
        {
            Console.WriteLine(a);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestFixAll5()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        {|FixAllInDocument:|}using (var a = b) { }
        using (var c = d) { }
    }
}",
new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestFixAll6()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        using (var a = b) { }
        {|FixAllInDocument:|}using (var c = d) { }
    }
}",
@"using System;

class C
{
    void M()
    {
        using (var a = b) { }
        using var c = d;
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingWithJumpInsideToOutside()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        label:
        [||]using (var a = b)
        {
            goto label;
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingWithJumpBeforeToAfter()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        {
            goto label;
            [||]using (var a = b)
            {
            }
        }
        label:
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestCollision1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream = File.OpenRead(""test""))
        {
        }
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNoCollision1()
        {
            await TestInRegularAndScript1Async(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
        {
        }
    }
}",
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        using Stream stream1 = File.OpenRead(""test"");
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestCollision2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
        {
            Stream stream;
        }
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNoCollision2()
        {
            await TestInRegularAndScript1Async(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
        {
            Stream stream2;
        }
    }
}",
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        using Stream stream1 = File.OpenRead(""test"");
        Stream stream2;
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestCollision3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
        {
            Goo(out var stream);
        }
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNoCollision3()
        {
            await TestInRegularAndScript1Async(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
        {
            Goo(out var stream2);
        }
    }
}",
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        using Stream stream1 = File.OpenRead(""test"");
        Goo(out var stream2);
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestCollision4()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
            Goo(out var stream);
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNoCollision4()
        {
            await TestInRegularAndScript1Async(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
            Goo(out var stream2);
    }
}",
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
        }
        using Stream stream1 = File.OpenRead(""test"");
        Goo(out var stream2);
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestCollision5()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
            Stream stream1;
        }
        [||]using (Stream stream1 = File.OpenRead(""test""))
        {
        }
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [WorkItem(35879, "https://github.com/dotnet/roslyn/issues/35879")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNoCollision5()
        {
            await TestInRegularAndScript1Async(
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
            Stream stream1;
        }
        [||]using (Stream stream2 = File.OpenRead(""test""))
        {
        }
    }
}",
@"using System.IO;

class Program
{
    static void Main()
    {
        using (Stream stream = File.OpenRead(""test""))
        {
            Stream stream1;
        }
        using Stream stream2 = File.OpenRead(""test"");
    }
}",
parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }
    }
}

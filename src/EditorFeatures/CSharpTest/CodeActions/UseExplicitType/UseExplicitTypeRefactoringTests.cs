// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseExplicitType;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseExplicitType
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
    public class UseExplicitTypeRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseExplicitTypeCodeRefactoringProvider();

        private readonly CodeStyleOption<bool> onWithSilent = new CodeStyleOption<bool>(true, NotificationOption.Silent);
        private readonly CodeStyleOption<bool> offWithSilent = new CodeStyleOption<bool>(false, NotificationOption.Silent);
        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);

        private IDictionary<OptionKey, object> PreferExplicitTypeWithInfo() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> PreferExplicitTypeWithSilent() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithSilent),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithSilent),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithSilent));

        private IDictionary<OptionKey, object> PreferImplicitTypeWithInfo() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo));

        private IDictionary<OptionKey, object> PreferImplicitTypeWithSilent() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithSilent));

        [Fact]
        public async Task TestIntLocalDeclaration()
        {
            var code = @"
class C
{
    static void Main()
    {
        var[||] i = 0;
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        int i = 0;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithSilent());
            await TestInRegularAndScriptAsync(code, expected, options: PreferExplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
        }

        [Fact]
        public async Task TestForeachInsideLocalDeclaration()
        {
            var code = @"
class C
{
    static void Main()
    {
        System.Action notThisLocal = () => { foreach (var[||] i in new int[0]) { } };
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        System.Action notThisLocal = () => { foreach (int[||] i in new int[0]) { } };
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithSilent());
        }

        [Fact]
        public async Task TestInVarPattern()
        {
            var code = @"
class C
{
    static void Main()
    {
        _ = 0 is var[||] i;
    }
}";

            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
        }

        [Fact]
        public async Task TestIntLocalDeclaration_Multiple()
        {
            var code = @"
class C
{
    static void Main()
    {
        var[||] i = 0, j = j;
    }
}";


            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
        }

        [Fact]
        public async Task TestIntLocalDeclaration_NoInitializer()
        {
            var code = @"
class C
{
    static void Main()
    {
        var[||] i;
    }
}";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task TestIntForLoop()
        {
            var code = @"
class C
{
    static void Main()
    {
        for (var[||] i = 0;;) { }
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        for (int i = 0;;) { }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithSilent());
        }

        [Fact]
        public async Task TestInDispose()
        {
            var code = @"
class C : System.IDisposable
{
    static void Main()
    {
        using (var[||] c = new C()) { }
    }
}";

            var expected = @"
class C : System.IDisposable
{
    static void Main()
    {
        using (C c = new C()) { }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithSilent());
        }

        [Fact]
        public async Task TestTypelessVarLocalDeclaration()
        {
            var code = @"
class var
{
    static void Main()
    {
        var[||] i = null;
    }
}";
            await TestMissingInRegularAndScriptAsync(code, options: PreferImplicitTypeWithSilent());
        }

        [Fact]
        public async Task TestIntForeachLoop()
        {
            var code = @"
class C
{
    static void Main()
    {
        foreach (var[||] i in new[] { 0 }) { }
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        foreach (int i in new[] { 0 }) { }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithSilent());
        }

        [Fact]
        public async Task TestIntDeconstruction()
        {
            var code = @"
class C
{
    static void Main()
    {
        var[||] (i, j) = (0, 1);
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        (int i, int j) = (0, 1);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithSilent());
        }

        [Fact]
        public async Task TestIntDeconstruction2()
        {
            var code = @"
class C
{
    static void Main()
    {
        (var[||] i, var j) = (0, 1);
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        (int i, var j) = (0, 1);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithSilent());
            await TestInRegularAndScriptAsync(code, expected, options: PreferExplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
        }

        [Fact]
        public async Task TestWithAnonymousType()
        {
            var code = @"
class C
{
    static void Main()
    {
        [|var|] x = new { Amount = 108, Message = ""Hello"" };
    }
}";

            await TestMissingInRegularAndScriptAsync(code, options: PreferImplicitTypeWithSilent());
        }

        [Fact, WorkItem(26923, "https://github.com/dotnet/roslyn/issues/26923")]
        public async Task NoSuggestionOnForeachCollectionExpression()
        {
            var code = @"using System;
using System.Collections.Generic;

class Program
{
    void Method(List<int> var)
    {
        foreach (int value in [|var|])
        {
            Console.WriteLine(value.Value);
        }
    }
}";

            // We never want to get offered here under any circumstances.
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithSilent());
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithInfo());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
        }

        private Task TestMissingInRegularAndScriptAsync(string initialMarkup, IDictionary<OptionKey, object> options)
        {
            return TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: options));
        }
    }
}

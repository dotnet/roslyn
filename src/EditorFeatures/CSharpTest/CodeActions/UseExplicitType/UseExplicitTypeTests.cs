// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseExplicitType;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseExplicitType
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
    public class UseExplicitTypeTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseExplicitTypeCodeRefactoringProvider();

        private readonly CodeStyleOption<bool> onWithNone = new CodeStyleOption<bool>(true, NotificationOption.None);
        private readonly CodeStyleOption<bool> offWithNone = new CodeStyleOption<bool>(false, NotificationOption.None);
        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);

        private IDictionary<OptionKey, object> PreferExplicitTypeWithInfo() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> PreferExplicitTypeWithNone() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithNone));

        private IDictionary<OptionKey, object> PreferImplicitTypeWithNone() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithNone));


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

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
            await TestInRegularAndScriptAsync(code, expected, options: PreferExplicitTypeWithNone());
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

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
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

            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithNone());
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


            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithNone());
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

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
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

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
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
            await TestMissingInRegularAndScriptAsync(code, options: PreferImplicitTypeWithNone());
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

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
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

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
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

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
            await TestInRegularAndScriptAsync(code, expected, options: PreferExplicitTypeWithNone());
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

            await TestMissingInRegularAndScriptAsync(code, options: PreferImplicitTypeWithNone());
        }

        private Task TestMissingInRegularAndScriptAsync(string initialMarkup, IDictionary<OptionKey, object> options)
        {
            return TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: options));
        }
    }
}

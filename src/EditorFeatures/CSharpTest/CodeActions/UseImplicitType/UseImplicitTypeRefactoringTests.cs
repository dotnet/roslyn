// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseExplicitType
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
    public class UseImplicitTypeRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseImplicitTypeCodeRefactoringProvider();

        private readonly CodeStyleOption<bool> onWithNone = new CodeStyleOption<bool>(true, NotificationOption.None);
        private readonly CodeStyleOption<bool> offWithNone = new CodeStyleOption<bool>(false, NotificationOption.None);
        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);

        private IDictionary<OptionKey, object> PreferExplicitTypeWithInfo() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> PreferImplicitTypeWithInfo() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo));

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
        int[||] i = 0;
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        var i = 0;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
            await TestInRegularAndScriptAsync(code, expected, options: PreferExplicitTypeWithNone());
            await TestInRegularAndScriptAsync(code, expected, options: PreferExplicitTypeWithInfo());
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithInfo());
        }

        [Fact]
        public async Task TestForeachInsideLocalDeclaration()
        {
            var code = @"
class C
{
    static void Main()
    {
        System.Action notThisLocal = () => { foreach (int[||] i in new int[0]) { } };
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        System.Action notThisLocal = () => { foreach (var[||] i in new int[0]) { } };
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
        }

        [Fact]
        public async Task TestInIntPattern()
        {
            var code = @"
class C
{
    static void Main()
    {
        _ = 0 is int[||] i;
    }
}";

            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithInfo());
        }

        [Fact]
        public async Task TestIntLocalDeclaration_Multiple()
        {
            var code = @"
class C
{
    static void Main()
    {
        int[||] i = 0, j = j;
    }
}";


            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithInfo());
        }

        [Fact]
        public async Task TestIntLocalDeclaration_NoInitializer()
        {
            var code = @"
class C
{
    static void Main()
    {
        int[||] i;
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
        for (int[||] i = 0;;) { }
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        for (var i = 0;;) { }
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
        using (C[||] c = new C()) { }
    }
}";

            var expected = @"
class C : System.IDisposable
{
    static void Main()
    {
        using (var c = new C()) { }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
        }

        [Fact]
        public async Task TestIntForeachLoop()
        {
            var code = @"
class C
{
    static void Main()
    {
        foreach (int[||] i in new[] { 0 }) { }
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        foreach (var i in new[] { 0 }) { }
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
        (int[||] i, var j) = (0, 1);
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        (var i, var j) = (0, 1);
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
        (int[||] i, var j) = (0, 1);
    }
}";

            var expected = @"
class C
{
    static void Main()
    {
        (var i, var j) = (0, 1);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: PreferImplicitTypeWithNone());
            await TestInRegularAndScriptAsync(code, expected, options: PreferExplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithInfo());
        }

        [Fact, WorkItem(26923, "https://github.com/dotnet/roslyn/issues/26923")]
        public async Task NoSuggestionOnForeachCollectionExpression()
        {
            var code = @"using System;
using System.Collections.Generic;

class C
{
    static void Main(string[] args)
    {
        foreach (string arg in [|args|])
        {

        }
    }
}";

            // We never want to get offered here under any circumstances.
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithNone());
            await TestMissingInRegularAndScriptAsync(code, PreferImplicitTypeWithInfo());
            await TestMissingInRegularAndScriptAsync(code, PreferExplicitTypeWithInfo());
        }

        private Task TestMissingInRegularAndScriptAsync(string initialMarkup, IDictionary<OptionKey, object> options)
        {
            return TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: options));
        }
    }
}

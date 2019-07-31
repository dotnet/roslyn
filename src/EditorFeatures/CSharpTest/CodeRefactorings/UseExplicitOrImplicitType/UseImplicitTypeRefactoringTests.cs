// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseExplicitOrImplicitType
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
    public class UseImplicitTypeRefactoringTests : AbstractUseTypeRefactoringTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseImplicitTypeCodeRefactoringProvider();

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

            await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
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

            await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
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

            await TestMissingInRegularAndScriptAsync(code);
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


            await TestMissingInRegularAndScriptAsync(code);
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

            await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
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

            await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
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

            await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
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

            await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
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

            await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
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
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
        public async Task NoSuggestionWithinAnExpression()
        {
            var code = @"using System;
using System;

class C
{
    static void Main(string[] args)
    {
        int a = 40 [||]+ 2;
    }
}";

            // We never want to get offered here under any circumstances.
            await TestMissingInRegularAndScriptAsync(code);
        }

        private async Task TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(string initialMarkup, string expectedMarkup)
        {
            // Enabled because the diagnostic is disabled
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: PreferImplicitTypeWithNone());

            // Enabled because the diagnostic is checking for the other direction
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: PreferExplicitTypeWithNone());
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: PreferExplicitTypeWithSilent());
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: PreferExplicitTypeWithInfo());

            // Disabled because the diagnostic will report it instead
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithSilent()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithInfo()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithWarning()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithError()));

            // Currently this refactoring is still enabled in cases where it would cause a warning or error
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: PreferExplicitTypeWithWarning());
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: PreferExplicitTypeWithError());
        }

        private async Task TestMissingInRegularAndScriptAsync(string initialMarkup)
        {
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithNone()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferExplicitTypeWithNone()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithSilent()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferExplicitTypeWithSilent()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithInfo()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferExplicitTypeWithInfo()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithWarning()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferExplicitTypeWithWarning()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferImplicitTypeWithError()));
            await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: PreferExplicitTypeWithError()));
        }
    }
}

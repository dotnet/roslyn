// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseExplicitType
{
    public sealed class UseExplicitTypeCompilerErrorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new UseExplicitTypeCodeFixProvider());

        private readonly CodeStyleOption<bool> _onWithError = new CodeStyleOption<bool>(true, NotificationOption.Error);
        private readonly CodeStyleOption<bool> _offWithError = new CodeStyleOption<bool>(false, NotificationOption.Error);

        private IDictionary<OptionKey, object> ExplicitTypeEverywhere() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, _offWithError),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, _offWithError),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, _offWithError));

        private IDictionary<OptionKey, object> ImplicitTypeEverywhere() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, _onWithError),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, _onWithError),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, _onWithError));

        // Verify that the code fix fixes the compiler error (CS0822) in addition to the analyzer diagnostic,
        // and does so regardless of user preference.

        [WorkItem(30516, "https://github.com/dotnet/roslyn/issues/30516")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task ConstVar()
        {
            var before = @"
class C
{
    void M()
    {
        const [|var|] v = 0;
    }
}";
            var after = @"
class C
{
    void M()
    {
        const int v = 0;
    }
}";
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task ConstVar_NonConstInitializer()
        {
            var before = @"
class C
{
    void M()
    {
        const [|var|] v = System.Console.ReadLine();
    }
}";
            var after = @"
class C
{
    void M()
    {
        const string v = System.Console.ReadLine();
    }
}";
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task ConstVar_Lambda()
        {
            var before = @"
class C
{
    void M()
    {
        const [|var|] v = () => { };
    }
}";
            var after = @"
class C
{
    void M()
    {
        const object v = () => { };
    }
}";
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task ConstVar_AnonymousType()
        {
            var before = @"
class C
{
    void M()
    {
        const [|var|] v = new { a = 0 };
    }
}";
            var after = @"
class C
{
    void M()
    {
        const object v = new { a = 0 };
    }
}";
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
        }
    }
}

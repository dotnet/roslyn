﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForAccessorsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyDiagnosticAnalyzer(), new UseExpressionBodyCodeFixProvider());

        private IDictionary<OptionKey, object> UseExpressionBody =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement));

        private IDictionary<OptionKey, object> UseExpressionBodyIncludingPropertiesAndIndexers =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));

        private IDictionary<OptionKey, object> UseBlockBodyIncludingPropertiesAndIndexers =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|return|] Bar();
        }
    }
}",
@"class C
{
    int Foo
    {
        get => Bar();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUpdatePropertyInsteadOfAccessor()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo
    {
        get
        {
            [|return|] Bar();
        }
    }
}",
@"class C
{
    int Foo => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyIncludingPropertiesAndIndexers));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOnIndexer1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int this[int i]
    {
        get
        {
            [|return|] Bar();
        }
    }
}",
@"class C
{
    int this[int i]
    {
        get => Bar();
        }
    }", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUpdateIndexerIfIndexerAndAccessorCanBeUpdated()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int this[int i]
    {
        get
        {
            [|return|] Bar();
        }
    }
}",
@"class C
{
    int this[int i] => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyIncludingPropertiesAndIndexers));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOnSetter1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        set
        {
            [|Bar|]();
        }
    }
}",
@"class C
{
    int Foo
    {
        set => [|Bar|]();
        }
    }", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithOnlySetter()
        {
            await TestMissingAsync(
@"class C
{
    int Foo
    {
        set => [|Bar|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|throw|] new NotImplementedException();
        }
    }
}",
@"class C
{
    int Foo
    {
        get => throw new NotImplementedException();
        }
    }", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|throw|] new NotImplementedException(); // comment
        }
    }
}",
@"class C
{
    int Foo
    {
        get => throw new NotImplementedException(); // comment
    }
}", ignoreTrivia: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get [|=>|] Bar();
    }
}",
@"class C
{
    int Foo
    {
        get
        {
            return Bar();
        }
    }
}", options: UseBlockBodyIncludingPropertiesAndIndexers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyForSetter1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        set [|=>|] Bar();
        }
    }",
@"class C
{
    int Foo
    {
        set
        {
            Bar();
        }
    }
}", options: UseBlockBodyIncludingPropertiesAndIndexers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get [|=>|] throw new NotImplementedException();
        }
    }",
@"class C
{
    int Foo
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}", options: UseBlockBodyIncludingPropertiesAndIndexers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get [|=>|] throw new NotImplementedException(); // comment
    }
}",
@"class C
{
    int Foo
    {
        get
        {
            throw new NotImplementedException(); // comment
        }
    }
}", ignoreTrivia: false, options: UseBlockBodyIncludingPropertiesAndIndexers);
        }

        [WorkItem(20350, "https://github.com/dotnet/roslyn/issues/20350")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestAccessorListFormatting()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo { get [|=>|] Bar(); }
}",
@"class C
{
    int Foo
    {
        get
        {
            return Bar();
        }
    }
}", options: UseBlockBodyIncludingPropertiesAndIndexers, ignoreTrivia: false);
        }

        [WorkItem(20350, "https://github.com/dotnet/roslyn/issues/20350")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestAccessorListFormatting_FixAll()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo { get {|FixAllInDocument:=>|} Bar(); set => Bar(); }
}",
@"class C
{
    int Foo
    {
        get
        {
            return Bar();
        }

        set
        {
            Bar();
        }
    }
}", options: UseBlockBodyIncludingPropertiesAndIndexers, ignoreTrivia: false);
        }

        [WorkItem(20362, "https://github.com/dotnet/roslyn/issues/20362")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7()
        {
            await TestAsync(
@"
using System;
class C
{
    int Foo { get [|=>|] throw new NotImplementedException(); }
}",
@"
using System;
class C
{
    int Foo 
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}", options: UseExpressionBody, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [WorkItem(20362, "https://github.com/dotnet/roslyn/issues/20362")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7_FixAll()
        {
            await TestAsync(
@"
using System;
class C
{
    int Foo { get {|FixAllInDocument:=>|} throw new NotImplementedException(); }
    int Bar { get => throw new NotImplementedException(); }
}",
@"
using System;
class C
{
    int Foo 
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    int Bar 
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}", options: UseExpressionBody, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }
    }
}

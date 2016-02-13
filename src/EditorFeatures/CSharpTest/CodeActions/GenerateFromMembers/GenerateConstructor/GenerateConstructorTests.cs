// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.GenerateFromMembers.GenerateConstructor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.GenerateFromMembers.GenerateConstructor
{
    public class GenerateConstructorTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new GenerateConstructorCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestSingleField()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Z { [|int a ;|] } ",
@"using System . Collections . Generic ; class Z { int a ; public Z ( int a ) { this . a = a ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestMultipleFields()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestSecondField()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Z { int a ; [|string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( string b ) { this . b = b ; } public Z ( int a ) { this . a = a ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestFieldAssigningConstructor()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestFieldAssigningConstructor2()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestDelegatingConstructor()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) : this ( a ) { this . b = b ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestMissingWithExistingConstructor()
        {
            await TestMissingAsync(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestMultipleProperties()
        {
            await TestAsync(
@"class Z { [|public int A { get ; private set ; } public string B { get ; private set ; }|] } ",
@"class Z { public Z ( int a , string b ) {  A = a ;  B = b ; } public int A { get ; private set ; } public string B { get ; private set ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestMultiplePropertiesWithQualification()
        {
            await TestAsync(
@"class Z { [|public int A { get ; private set ; } public string B { get ; private set ; }|] } ",
@"class Z { public Z ( int a , string b ) { this . A = a ; this . B = b ; } public int A { get ; private set ; } public string B { get ; private set ; } } ",
index: 0, options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestStruct()
        {
            await TestAsync(
@"using System . Collections . Generic ; struct S { [|int i ;|] } ",
@"using System . Collections . Generic ; struct S { int i ; public S ( int i ) { this . i = i ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestStruct1()
        {
            await TestAsync(
@"using System . Collections . Generic ; struct S { [|int i { get; set; }|] } ",
@"using System . Collections . Generic ; struct S { public S ( int i ) : this() { this . i = i ; } int i { get; set; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestStruct2()
        {
            await TestAsync(
@"using System . Collections . Generic ; struct S { int i { get; set; } [|int y;|] } ",
@"using System . Collections . Generic ; struct S { int i { get; set; } int y; public S ( int y ) : this() { this . y = y ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestStruct3()
        {
            await TestAsync(
@"using System . Collections . Generic ; struct S { [|int i { get; set; }|] int y; } ",
@"using System . Collections . Generic ; struct S { int i { get; set; } int y; public S ( int i ) : this() { this . i = i ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestGenericType()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program < T > { [|int i ;|] } ",
@"using System . Collections . Generic ; class Program < T > { int i ; public Program ( int i ) { this . i = i ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestSmartTagText1()
        {
            await TestSmartTagTextAsync(
@"using System.Collections.Generic; class Program { [|bool b; HashSet<string> s;|] }",
string.Format(FeaturesResources.GenerateConstructor, "Program", "bool, HashSet<string>"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestSmartTagText2()
        {
            await TestSmartTagTextAsync(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
string.Format(FeaturesResources.GenerateFieldAssigningConstructor, "Program", "bool, HashSet<string>"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestSmartTagText3()
        {
            await TestSmartTagTextAsync(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
string.Format(FeaturesResources.GenerateDelegatingConstructor, "Program", "bool, HashSet<string>"),
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestContextualKeywordName()
        {
            await TestAsync(
@"class Program { [|int yield ;|] } ",
@"class Program { int yield ; public Program ( int yield ) { this . yield = yield ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestGenerateConstructorNotOfferedForDuplicate()
        {
            await TestMissingAsync(
"using System ; class X { public X ( string v ) { } static void Test ( ) { new X ( new [|string|] ( ) ) ; } } ");
        }
    }
}

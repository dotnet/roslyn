// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        public void TestSingleField()
        {
            Test(
@"using System . Collections . Generic ; class Z { [|int a ;|] } ",
@"using System . Collections . Generic ; class Z { int a ; public Z ( int a ) { this . a = a ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestMultipleFields()
        {
            Test(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestSecondField()
        {
            Test(
@"using System . Collections . Generic ; class Z { int a ; [|string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( string b ) { this . b = b ; } public Z ( int a ) { this . a = a ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestFieldAssigningConstructor()
        {
            Test(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestFieldAssigningConstructor2()
        {
            Test(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestDelegatingConstructor()
        {
            Test(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } } ",
@"using System . Collections . Generic ; class Z { int a ; string b ; public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) : this ( a ) { this . b = b ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestMissingWithExistingConstructor()
        {
            TestMissing(
@"using System . Collections . Generic ; class Z { [|int a ; string b ;|] public Z ( int a ) { this . a = a ; } public Z ( int a , string b ) { this . a = a ; this . b = b ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestMultipleProperties()
        {
            Test(
@"class Z { [|public int A { get ; private set ; } public string B { get ; private set ; }|] } ",
@"class Z { public Z ( int a , string b ) {  A = a ;  B = b ; } public int A { get ; private set ; } public string B { get ; private set ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestMultiplePropertiesWithQualification()
        {
            Test(
@"class Z { [|public int A { get ; private set ; } public string B { get ; private set ; }|] } ",
@"class Z { public Z ( int a , string b ) { this . A = a ; this . B = b ; } public int A { get ; private set ; } public string B { get ; private set ; } } ",
index: 0, options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestStruct()
        {
            Test(
@"using System . Collections . Generic ; struct S { [|int i ;|] } ",
@"using System . Collections . Generic ; struct S { int i ; public S ( int i ) { this . i = i ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestStruct1()
        {
            Test(
@"using System . Collections . Generic ; struct S { [|int i { get; set; }|] } ",
@"using System . Collections . Generic ; struct S { public S ( int i ) : this() { this . i = i ; } int i { get; set; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestStruct2()
        {
            Test(
@"using System . Collections . Generic ; struct S { int i { get; set; } [|int y;|] } ",
@"using System . Collections . Generic ; struct S { int i { get; set; } int y; public S ( int y ) : this() { this . y = y ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestStruct3()
        {
            Test(
@"using System . Collections . Generic ; struct S { [|int i { get; set; }|] int y; } ",
@"using System . Collections . Generic ; struct S { int i { get; set; } int y; public S ( int i ) : this() { this . i = i ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestGenericType()
        {
            Test(
@"using System . Collections . Generic ; class Program < T > { [|int i ;|] } ",
@"using System . Collections . Generic ; class Program < T > { int i ; public Program ( int i ) { this . i = i ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestSmartTagText1()
        {
            TestSmartTagText(
@"using System.Collections.Generic; class Program { [|bool b; HashSet<string> s;|] }",
string.Format(FeaturesResources.GenerateConstructor, "Program", "bool, HashSet<string>"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestSmartTagText2()
        {
            TestSmartTagText(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
string.Format(FeaturesResources.GenerateFieldAssigningConstructor, "Program", "bool, HashSet<string>"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestSmartTagText3()
        {
            TestSmartTagText(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
string.Format(FeaturesResources.GenerateDelegatingConstructor, "Program", "bool, HashSet<string>"),
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestContextualKeywordName()
        {
            Test(
@"class Program { [|int yield ;|] } ",
@"class Program { int yield ; public Program ( int yield ) { this . yield = yield ; } } ");
        }
    }
}

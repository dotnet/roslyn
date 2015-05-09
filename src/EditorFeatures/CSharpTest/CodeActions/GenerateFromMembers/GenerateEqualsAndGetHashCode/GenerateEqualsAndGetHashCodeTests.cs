// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.GenerateFromMembers.GenerateEqualsAndGetHashCode;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.GenerateFromMembers.GenerateEqualsAndGetHashCode
{
    public class GenerateEqualsAndGetHashCodeTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new GenerateEqualsAndGetHashCodeCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsSingleField()
        {
            Test(
@"using System . Collections . Generic ; class Program { [|int a ;|] } ",
@"using System . Collections . Generic ; class Program { int a ; public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && EqualityComparer < int > . Default . Equals ( a , program . a ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsLongName()
        {
            Test(
@"using System . Collections . Generic ; class ReallyLongName { [|int a ;|] } ",
@"using System . Collections . Generic ; class ReallyLongName { int a ; public override bool Equals ( object obj ) { var name = obj as ReallyLongName ; return name != null && EqualityComparer < int > . Default . Equals ( a , name . a ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsKeywordName()
        {
            Test(
@"using System . Collections . Generic ; class ReallyLongLong { [|long a ;|] } ",
@"using System . Collections . Generic ; class ReallyLongLong { long a ; public override bool Equals ( object obj ) { var @long = obj as ReallyLongLong ; return @long != null && EqualityComparer < long > . Default . Equals ( a , @long . a ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsProperty()
        {
            Test(
@"using System . Collections . Generic ; class ReallyLongName { [|int a ; string B { get ; }|] } ",
@"using System . Collections . Generic ; class ReallyLongName { int a ; string B { get ; } public override bool Equals ( object obj ) { var name = obj as ReallyLongName ; return name != null && EqualityComparer < int > . Default . Equals ( a , name . a ) && EqualityComparer < string > . Default . Equals ( B , name . B ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsBaseTypeWithNoEquals()
        {
            Test(
@"class Base { } class Program : Base { [|int i ;|] } ",
@"using System . Collections . Generic; class Base { } class Program : Base { int i ; public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && EqualityComparer < int > . Default . Equals ( i , program . i ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsBaseWithOverriddenEquals()
        {
            Test(
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Program : Base { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Program : Base { int i ; string S { get ; } public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && base . Equals ( obj ) && EqualityComparer < int > . Default . Equals ( i , program . i ) && EqualityComparer < string > . Default . Equals ( S , program . S ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsOverriddenDeepBase()
        {
            Test(
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Middle : Base { } class Program : Middle { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Middle : Base { } class Program : Middle { int i ; string S { get ; } public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && base . Equals ( obj ) && EqualityComparer < int > . Default . Equals ( i , program . i ) && EqualityComparer < string > . Default . Equals ( S , program . S ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsStruct()
        {
            Test(
@"using System . Collections . Generic ; struct ReallyLongName { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; struct ReallyLongName { int i ; string S { get ; } public override bool Equals ( object obj ) { if ( ! ( obj is ReallyLongName ) ) { return false ; } var name = ( ReallyLongName ) obj ; return EqualityComparer < int > . Default . Equals ( i , name . i ) && EqualityComparer < string > . Default . Equals ( S , name . S ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestEqualsGenericType()
        {
            var code = @"
using System.Collections.Generic;
class Program<T>
{
    [|int i;|]
}
";

            var expected = @"
using System.Collections.Generic;
class Program<T>
{
    int i;

    public override bool Equals(object obj)
    {
        var program = obj as Program<T>;
        return program != null && EqualityComparer<int>.Default.Equals(i, program.i);
    }
}
";

            Test(code, expected, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestGetHashCodeSingleField()
        {
            Test(
@"using System . Collections . Generic ; class Program { [|int i ;|] } ",
@"using System . Collections . Generic ; class Program { int i ; public override int GetHashCode ( ) { return EqualityComparer < int > . Default . GetHashCode ( i ) ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestGetHashCodeTypeParameter()
        {
            Test(
@"using System . Collections . Generic ; class Program < T > { [|T i ;|] } ",
@"using System . Collections . Generic ; class Program < T > { T i ; public override int GetHashCode ( ) { return EqualityComparer < T > . Default . GetHashCode ( i ) ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestGetHashCodeGenericType()
        {
            Test(
@"using System . Collections . Generic ; class Program < T > { [|Program < T > i ;|] } ",
@"using System . Collections . Generic ; class Program < T > { Program < T > i ; public override int GetHashCode ( ) { return EqualityComparer < Program < T > > . Default . GetHashCode ( i ) ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestGetHashCodeMultipleMembers()
        {
            Test(
@"using System . Collections . Generic ; class Program { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; class Program { int i ; string S { get ; } public override int GetHashCode ( ) { var hashCode = EqualityComparer < int > . Default . GetHashCode ( i ) ; hashCode = hashCode * - 1521134295 + EqualityComparer < string > . Default . GetHashCode ( S ) ; return hashCode ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestSmartTagText1()
        {
            TestSmartTagText(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
FeaturesResources.GenerateEqualsObject);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestSmartTagText2()
        {
            TestSmartTagText(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
FeaturesResources.GenerateGetHashCode,
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void TestSmartTagText3()
        {
            TestSmartTagText(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
FeaturesResources.GenerateBoth,
index: 2);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.GenerateFromMembers.GenerateEqualsAndGetHashCode;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.GenerateFromMembers.GenerateEqualsAndGetHashCode
{
    public class GenerateEqualsAndGetHashCodeTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new GenerateEqualsAndGetHashCodeCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsSingleField()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|int a ;|] } ",
@"using System . Collections . Generic ; class Program { int a ; public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && EqualityComparer < int > . Default . Equals ( a , program . a ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsLongName()
        {
            await TestAsync(
@"using System . Collections . Generic ; class ReallyLongName { [|int a ;|] } ",
@"using System . Collections . Generic ; class ReallyLongName { int a ; public override bool Equals ( object obj ) { var name = obj as ReallyLongName ; return name != null && EqualityComparer < int > . Default . Equals ( a , name . a ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsKeywordName()
        {
            await TestAsync(
@"using System . Collections . Generic ; class ReallyLongLong { [|long a ;|] } ",
@"using System . Collections . Generic ; class ReallyLongLong { long a ; public override bool Equals ( object obj ) { var @long = obj as ReallyLongLong ; return @long != null && EqualityComparer < long > . Default . Equals ( a , @long . a ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsProperty()
        {
            await TestAsync(
@"using System . Collections . Generic ; class ReallyLongName { [|int a ; string B { get ; }|] } ",
@"using System . Collections . Generic ; class ReallyLongName { int a ; string B { get ; } public override bool Equals ( object obj ) { var name = obj as ReallyLongName ; return name != null && EqualityComparer < int > . Default . Equals ( a , name . a ) && EqualityComparer < string > . Default . Equals ( B , name . B ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsBaseTypeWithNoEquals()
        {
            await TestAsync(
@"class Base { } class Program : Base { [|int i ;|] } ",
@"using System . Collections . Generic; class Base { } class Program : Base { int i ; public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && EqualityComparer < int > . Default . Equals ( i , program . i ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsBaseWithOverriddenEquals()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Program : Base { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Program : Base { int i ; string S { get ; } public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && base . Equals ( obj ) && EqualityComparer < int > . Default . Equals ( i , program . i ) && EqualityComparer < string > . Default . Equals ( S , program . S ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsOverriddenDeepBase()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Middle : Base { } class Program : Middle { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; class Base { public override bool Equals ( object o ) { } } class Middle : Base { } class Program : Middle { int i ; string S { get ; } public override bool Equals ( object obj ) { var program = obj as Program ; return program != null && base . Equals ( obj ) && EqualityComparer < int > . Default . Equals ( i , program . i ) && EqualityComparer < string > . Default . Equals ( S , program . S ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsStruct()
        {
            await TestAsync(
@"using System . Collections . Generic ; struct ReallyLongName { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; struct ReallyLongName { int i ; string S { get ; } public override bool Equals ( object obj ) { if ( ! ( obj is ReallyLongName ) ) { return false ; } var name = ( ReallyLongName ) obj ; return EqualityComparer < int > . Default . Equals ( i , name . i ) && EqualityComparer < string > . Default . Equals ( S , name . S ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsGenericType()
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

            await TestAsync(code, expected, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSingleField()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|int i ;|] } ",
@"using System . Collections . Generic ; class Program { int i ; public override int GetHashCode ( ) { return EqualityComparer < int > . Default . GetHashCode ( i ) ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeTypeParameter()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program < T > { [|T i ;|] } ",
@"using System . Collections . Generic ; class Program < T > { T i ; public override int GetHashCode ( ) { return EqualityComparer < T > . Default . GetHashCode ( i ) ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeGenericType()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program < T > { [|Program < T > i ;|] } ",
@"using System . Collections . Generic ; class Program < T > { Program < T > i ; public override int GetHashCode ( ) { return EqualityComparer < Program < T > > . Default . GetHashCode ( i ) ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeMultipleMembers()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|int i ; string S { get ; }|] } ",
@"using System . Collections . Generic ; class Program { int i ; string S { get ; } public override int GetHashCode ( ) { var hashCode = EqualityComparer < int > . Default . GetHashCode ( i ) ; hashCode = hashCode * - 1521134295 + EqualityComparer < string > . Default . GetHashCode ( S ) ; return hashCode ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestSmartTagText1()
        {
            await TestSmartTagTextAsync(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
FeaturesResources.Generate_Equals_object);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestSmartTagText2()
        {
            await TestSmartTagTextAsync(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
FeaturesResources.Generate_GetHashCode,
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestSmartTagText3()
        {
            await TestSmartTagTextAsync(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
FeaturesResources.Generate_Both,
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task Tuple_Disabled()
        {
            await TestAsync(@"using System . Collections . Generic ; class C { [|(int, string) a ;|] } ",
@"using System . Collections . Generic ; class C { (int, string) a ; public override bool Equals ( object obj ) { var c = obj as C ; return c != null && EqualityComparer < (int, string) > . Default . Equals ( a , c . a ) ; } } ",
index: 0,
                parseOptions: TestOptions.Regular.WithLanguageVersion(CodeAnalysis.CSharp.LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task Tuples_Equals()
        {
            await TestAsync(
@"using System . Collections . Generic ; class C { [|(int, string) a ;|] } ",
@"using System . Collections . Generic ; class C { (int, string) a ; public override bool Equals ( object obj ) { var c = obj as C ; return c != null && EqualityComparer < (int, string) > . Default . Equals ( a , c . a ) ; } } ",
index: 0,
parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TupleWithNames_Equals()
        {
            await TestAsync(
@"using System . Collections . Generic ; class C { [|(int x, string y) a ;|] } ",
@"using System . Collections . Generic ; class C { (int x, string y) a ; public override bool Equals ( object obj ) { var c = obj as C ; return c != null && EqualityComparer < (int x, string y) > . Default . Equals ( a , c . a ) ; } } ",
index: 0,
parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task Tuple_HashCode()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|(int, string) i ;|] } ",
@"using System . Collections . Generic ; class Program { (int, string) i ; public override int GetHashCode ( ) { return EqualityComparer < (int, string) > . Default . GetHashCode ( i ) ; } } ",
index: 1,
parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TupleWithNames_HashCode()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|(int x, string y) i ;|] } ",
@"using System . Collections . Generic ; class Program { (int x, string y) i ; public override int GetHashCode ( ) { return EqualityComparer < (int x, string y) > . Default . GetHashCode ( i ) ; } } ",
index: 1,
parseOptions: TestOptions.Regular,
withScriptOption: true);
        }
    }
}

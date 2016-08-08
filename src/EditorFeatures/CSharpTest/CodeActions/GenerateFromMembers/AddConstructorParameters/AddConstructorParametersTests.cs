// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.GenerateFromMembers.AddConstructorParameters;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.GenerateFromMembers.AddConstructorParameters
{
    public class AddConstructorParametersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new AddConstructorParametersCodeRefactoringProvider();
        }

        [Fact, WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077"), Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestAdd1()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|int i ; string s ;|] public Program ( int i ) { this . i = i ; } } ",
@"using System . Collections . Generic ; class Program { int i ; string s ; public Program ( int i , string s ) { this . i = i ; this . s = s ; } } ",
index: 0);
        }

        [Fact, WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077"), Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestAddOptional1()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|int i ; string s ;|] public Program ( int i ) { this . i = i ; } } ",
@"using System . Collections . Generic ; class Program { int i ; string s ; public Program ( int i , string s = null ) { this . i = i ; this . s = s ; } } ",
index: 1);
        }

        [Fact, WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077"), Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestAddToConstructorWithMostMatchingParameters1()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|int i ; string s ; bool b ;|] public Program ( int i ) { this . i = i ; } public Program ( int i , string s ) : this ( i ) { this . s = s ; } } ",
@"using System . Collections . Generic ; class Program { int i ; string s ; bool b ; public Program ( int i ) { this . i = i ; } public Program ( int i , string s , bool b ) : this ( i ) { this . s = s ; this . b = b ; } } ",
index: 0);
        }

        [Fact, WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077"), Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestAddOptionalToConstructorWithMostMatchingParameters1()
        {
            await TestAsync(
@"using System . Collections . Generic ; class Program { [|int i ; string s ; bool b ;|] public Program ( int i ) { this . i = i ; } public Program ( int i , string s ) : this ( i ) { this . s = s ; } } ",
@"using System . Collections . Generic ; class Program { int i ; string s ; bool b ; public Program ( int i ) { this . i = i ; } public Program ( int i , string s , bool b = default(bool) ) : this ( i ) { this . s = s ; this . b = b ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestSmartTagDisplayText1()
        {
            await TestSmartTagTextAsync(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
string.Format(FeaturesResources.Add_parameters_to_0_1, "Program", "bool"),
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestSmartTagDisplayText2()
        {
            await TestSmartTagTextAsync(
@"using System . Collections . Generic ; class Program { [|bool b ; HashSet < string > s ;|] public Program ( bool b ) { this . b = b ; } } ",
string.Format(FeaturesResources.Add_optional_parameters_to_0_1, "Program", "bool"),
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTuple()
        {
            await TestAsync(
@"class Program { [|(int, string) i ; (string, int) s ;|] public Program ( (int, string) i ) { this . i = i ; } } ",
@"class Program { (int, string) i ; (string, int) s ; public Program ( (int, string) i , (string, int) s ) { this . i = i ; this . s = s ; } } ",
index: 0, parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTupleWithNames()
        {
            await TestAsync(
@"class Program { [|(int a, string b) i ; (string c, int d) s ;|] public Program ( (int a, string b) i ) { this . i = i ; } } ",
@"class Program { (int a, string b) i ; (string c, int d) s ; public Program ( (int a, string b) i , (string c, int d) s ) { this . i = i ; this . s = s ; } } ",
index: 0, parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTupleWithDifferentNames()
        {
            await TestMissingAsync(
@"class Program { [|(int a, string b) i ; (string c, int d) s ;|] public Program ( (int e, string f) i ) { this . i = i ; } } ",
parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTupleOptional()
        {
            await TestAsync(
@"class Program { [|(int, string) i ; (string, int) s ;|] public Program ( (int, string) i ) { this . i = i ; } } ",
@"class Program { (int, string) i ; (string, int) s ; public Program ( (int, string) i , (string, int) s = default((string, int)) ) { this . i = i ; this . s = s ; } } ",
index: 1, parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTupleOptionalWithNames()
        {
            await TestAsync(
@"class Program { [|(int a, string b) i ; (string c, int d) s ;|] public Program ( (int a, string b) i ) { this . i = i ; } } ",
@"class Program { (int a, string b) i ; (string c, int d) s ; public Program ( (int a, string b) i , (string c, int d) s = default((string c, int d)) ) { this . i = i ; this . s = s ; } } ",
index: 1, parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTupleOptionalWithDifferentNames()
        {
            await TestMissingAsync(
@"class Program { [|(int a, string b) i ; (string c, int d) s ;|] public Program ( (int e, string f) i ) { this . i = i ; } } ",
parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTupleWithNullable()
        {
            await TestAsync(
@"class Program { [|(int?, bool?) i ; (byte?, long?) s ;|] public Program ( (int?, bool?) i ) { this . i = i ; } } ",
@"class Program { (int?, bool?) i ; (byte?, long?) s ; public Program ( (int?, bool?) i , (byte?, long?) s ) { this . i = i ; this . s = s ; } } ",
index: 0, parseOptions: TestOptions.Regular, withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)]
        public async Task TestTupleWithGenericss()
        {
            await TestAsync(
@"class Program { [|(List<int>, List<bool>) i ; (List<byte>, List<long>) s ;|] public Program ( (List<int>, List<bool>) i ) { this . i = i ; } } ",
@"class Program { (List<int>, List<bool>) i ; (List<byte>, List<long>) s ; public Program ( (List<int>, List<bool>) i , (List<byte>, List<long>) s ) { this . i = i ; this . s = s ; } } ",
index: 0, parseOptions: TestOptions.Regular, withScriptOption: true);
        }
    }
}

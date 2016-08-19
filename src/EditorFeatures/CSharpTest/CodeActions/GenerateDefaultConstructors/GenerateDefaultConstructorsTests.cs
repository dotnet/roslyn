// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.GenerateDefaultConstructors
{
    public class GenerateDefaultConstructorsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new GenerateDefaultConstructorsCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestProtectedBase()
        {
            await TestAsync(
@"class C : [||]B { } class B { protected B(int x) { } }",
@"class C : B { protected C(int x) : base(x) { } } class B { protected B(int x) { } }",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestPublicBase()
        {
            await TestAsync(
@"class C : [||]B { } class B { public B(int x) { } }",
@"class C : B { public C(int x) : base(x) { } } class B { public B(int x) { } }",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestInternalBase()
        {
            await TestAsync(
@"class C : [||]B { } class B { internal B(int x) { } }",
@"class C : B { internal C(int x) : base(x) { } } class B { internal B(int x) { } }",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestPrivateBase()
        {
            await TestMissingAsync(
@"class C : [||]B { } class B { private B(int x) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestRefOutParams()
        {
            await TestAsync(
@"class C : [||]B { } class B { internal B(ref int x, out string s, params bool[] b) { } }",
@"class C : B { internal C(ref int x, out string s, params bool[] b) : base(ref x, out s, b) { } } class B { internal B(ref int x, out string s, params bool[] b) { } }",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFix1()
        {
            await TestAsync(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { internal C(int x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFix2()
        {
            await TestAsync(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { protected C(string x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestRefactoring1()
        {
            await TestAsync(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { public C(bool x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixAll1()
        {
            await TestAsync(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { public C(bool x) : base(x) { } protected C(string x) : base(x) { } internal C(int x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixAll2()
        {
            await TestAsync(
@"class C : [||]B { public C(bool x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { public C(bool x) { } protected C(string x) : base(x) { } internal C(int x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixAll_WithTuples()
        {
            await TestAsync(
@"class C : [||]B { public C((bool, bool) x) { } } class B { internal B((int, int) x) { } protected B((string, string) x) { } public B((bool, bool) x) { } }",
@"class C : B { public C((bool, bool) x) { } protected C((string, string) x) : base(x) { } internal C((int, int) x) : base(x) { } } class B { internal B((int, int) x) { } protected B((string, string) x) { } public B((bool, bool) x) { } }",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestMissing1()
        {
            await TestMissingAsync(
@"class C : [||]B { public C(int x) { } } class B { internal B(int x) { } }");
        }

        [WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestDefaultConstructorGeneration_1()
        {
            await TestAsync(
@"class C : [||]B { public C(int y) { } } class B { internal B(int x) { } }",
@"class C : B { public C(int y) { } internal C(int x) : base(x) { } } class B { internal B(int x) { } }");
        }

        [WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestDefaultConstructorGeneration_2()
        {
            await TestAsync(
@"class C : [||]B { private C(int y) { } } class B { internal B(int x) { } }",
@"class C : B { internal C(int x) : base(x) { } private C(int y) { } } class B { internal B(int x) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixCount1()
        {
            await TestActionCountAsync(
@"class C : [||]B { } class B { public B(int x) { } }",
count: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        [WorkItem(544070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544070")]
        public async Task TestException1()
        {
            await TestAsync(
@"using System;
class Program : Excep[||]tion
{
}",
@"using System;
using System.Runtime.Serialization;

class Program : Exception
{
    public Program()
    {
    }

    public Program(string message) : base(message)
    {
    }

    public Program(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected Program(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}",
index: 3,
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestException2()
        {
            await TestAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : [||]Exception { public Program ( ) { } static void Main ( string [ ] args ) { } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Runtime . Serialization; class Program : Exception { public Program ( ) { } public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program (SerializationInfo info , StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestException3()
        {
            await TestAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : [||]Exception { public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : Exception { public Program ( ) { } public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestException4()
        {
            await TestAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : [||]Exception { public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : Exception { public Program ( ) { } public Program ( ) { } public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task Tuple()
        {
            await TestAsync(
@"class C : [||]B { } class B { public B((int, string) x) { } }",
@"class C : B { public C((int, string) x) : base(x) { } } class B { public B((int, string) x) { } }",
index: 0,
parseOptions: TestOptions.Regular,
withScriptOption: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TupleWithNames()
        {
            await TestAsync(
@"class C : [||]B { } class B { public B((int a, string b) x) { } }",
@"class C : B { public C((int a, string b) x) : base(x) { } } class B { public B((int a, string b) x) { } }",
index: 0,
parseOptions: TestOptions.Regular,
withScriptOption: true);
        }
    }
}
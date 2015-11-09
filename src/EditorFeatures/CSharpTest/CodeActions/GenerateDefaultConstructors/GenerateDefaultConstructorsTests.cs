// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.GenerateDefaultConstructors
{
    public class GenerateDefaultConstructorsTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new GenerateDefaultConstructorsCodeRefactoringProvider();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestProtectedBase()
        {
            Test(
@"class C : [||]B { } class B { protected B(int x) { } }",
@"class C : B { protected C(int x) : base(x) { } } class B { protected B(int x) { } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestPublicBase()
        {
            Test(
@"class C : [||]B { } class B { public B(int x) { } }",
@"class C : B { public C(int x) : base(x) { } } class B { public B(int x) { } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestInternalBase()
        {
            Test(
@"class C : [||]B { } class B { internal B(int x) { } }",
@"class C : B { internal C(int x) : base(x) { } } class B { internal B(int x) { } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestPrivateBase()
        {
            TestMissing(
@"class C : [||]B { } class B { private B(int x) { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestRefOutParams()
        {
            Test(
@"class C : [||]B { } class B { internal B(ref int x, out string s, params bool[] b) { } }",
@"class C : B { internal C(ref int x, out string s, params bool[] b) : base(ref x, out s, b) { } } class B { internal B(ref int x, out string s, params bool[] b) { } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestFix1()
        {
            Test(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { internal C(int x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestFix2()
        {
            Test(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { protected C(string x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestRefactoring1()
        {
            Test(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { public C(bool x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestFixAll1()
        {
            Test(
@"class C : [||]B { } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { public C(bool x) : base(x) { } protected C(string x) : base(x) { } internal C(int x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestFixAll2()
        {
            Test(
@"class C : [||]B { public C(bool x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
@"class C : B { public C(bool x) { } protected C(string x) : base(x) { } internal C(int x) : base(x) { } } class B { internal B(int x) { } protected B(string x) { } public B(bool x) { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestMissing1()
        {
            TestMissing(
@"class C : [||]B { public C(int x) { } } class B { internal B(int x) { } }");
        }

        [WorkItem(889349)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestDefaultConstructorGeneration_1()
        {
            Test(
@"class C : [||]B { public C(int y) { } } class B { internal B(int x) { } }",
@"class C : B { public C(int y) { } internal C(int x) : base(x) { } } class B { internal B(int x) { } }");
        }

        [WorkItem(889349)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestDefaultConstructorGeneration_2()
        {
            Test(
@"class C : [||]B { private C(int y) { } } class B { internal B(int x) { } }",
@"class C : B { internal C(int x) : base(x) { } private C(int y) { } } class B { internal B(int x) { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestFixCount1()
        {
            TestActionCount(
@"class C : [||]B { } class B { public B(int x) { } }",
count: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        [WorkItem(544070)]
        public void TestException1()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestException2()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : [||]Exception { public Program ( ) { } static void Main ( string [ ] args ) { } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Runtime . Serialization; class Program : Exception { public Program ( ) { } public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program (SerializationInfo info , StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestException3()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : [||]Exception { public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : Exception { public Program ( ) { } public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public void TestException4()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : [||]Exception { public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program : Exception { public Program ( ) { } public Program ( ) { } public Program ( string message ) : base ( message ) { } public Program ( string message , Exception innerException ) : base ( message , innerException ) { } protected Program ( System . Runtime . Serialization . SerializationInfo info , System . Runtime . Serialization . StreamingContext context ) : base ( info , context ) { } static void Main ( string [ ] args ) { } } ",
index: 2);
        }
    }
}

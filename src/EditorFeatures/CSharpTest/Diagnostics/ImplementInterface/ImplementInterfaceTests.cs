// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ImplementInterface;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.ImplementInterface
{
    public partial class ImplementInterfaceTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(null, new ImplementInterfaceCodeFixProvider());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMethod()
        {
            Test(
@"interface IInterface { void Method1 ( ) ; } class Class : [|IInterface|] { } ",
@"using System; interface IInterface { void Method1 ( ) ; } class Class : IInterface { public void Method1 ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMethodWhenClassBracesAreMissing()
        {
            Test(
@"interface IInterface { void Method1 ( ) ; } class Class : [|IInterface|] ",
@"using System; interface IInterface { void Method1 ( ) ; } class Class : IInterface { public void Method1 ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestInheritance1()
        {
            Test(
@"interface IInterface1 { void Method1 ( ) ; } interface IInterface2 : IInterface1 { } class Class : [|IInterface2|] { } ",
@"using System; interface IInterface1 { void Method1 ( ) ; } interface IInterface2 : IInterface1 { } class Class : IInterface2 { public void Method1 ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestInheritance2()
        {
            Test(
@"interface IInterface1 { } interface IInterface2 : IInterface1 { void Method1 ( ) ; } class Class : [|IInterface2|] { } ",
@"using System; interface IInterface1 { } interface IInterface2 : IInterface1 { void Method1 ( ) ; } class Class : IInterface2 { public void Method1 ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestInheritance3()
        {
            Test(
@"interface IInterface1 { void Method1 ( ) ; } interface IInterface2 : IInterface1 { void Method2 ( ) ; } class Class : [|IInterface2|] { } ",
@"using System; interface IInterface1 { void Method1 ( ) ; } interface IInterface2 : IInterface1 { void Method2 ( ) ; } class Class : IInterface2 { public void Method1 ( ) { throw new NotImplementedException ( ) ; } public void Method2 ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestInheritanceMatchingMethod()
        {
            Test(
@"interface IInterface1 { void Method1 ( ) ; } interface IInterface2 : IInterface1 { void Method1 ( ) ; } class Class : [|IInterface2|] { } ",
@"using System; interface IInterface1 { void Method1 ( ) ; } interface IInterface2 : IInterface1 { void Method1 ( ) ; } class Class : IInterface2 { public void Method1 ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestExistingConflictingMethodReturnType()
        {
            Test(
@"interface IInterface1 { void Method1 ( ) ; } class Class : [|IInterface1|] { public int Method1 ( ) { return 0 ; } } ",
@"using System; interface IInterface1 { void Method1 ( ) ; } class Class : IInterface1 { public int Method1 ( ) { return 0 ; } void IInterface1 . Method1 ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestExistingConflictingMethodParameters()
        {
            Test(
@"interface IInterface1 { void Method1 ( int i ) ; } class Class : [|IInterface1|] { public void Method1 ( string i ) { } } ",
@"using System; interface IInterface1 { void Method1 ( int i ) ; } class Class : IInterface1 { public void Method1 ( int i ) { throw new NotImplementedException ( ) ; } public void Method1 ( string i ) { } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementGenericType()
        {
            Test(
@"interface IInterface1 < T > { void Method1 ( T t ) ; } class Class : [|IInterface1 < int >|] { } ",
@"using System; interface IInterface1 < T > { void Method1 ( T t ) ; } class Class : IInterface1 < int > { public void Method1 ( int t ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementGenericTypeWithGenericMethod()
        {
            Test(
@"interface IInterface1 < T > { void Method1 < U > ( T t , U u ) ; } class Class : [|IInterface1 < int >|] { } ",
@"using System; interface IInterface1 < T > { void Method1 < U > ( T t , U u ) ; } class Class : IInterface1 < int > { public void Method1 < U > ( int t , U u ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementGenericTypeWithGenericMethodWithNaturalConstraint()
        {
            Test(
@"interface IInterface1 < T > { void Method1 < U > ( T t , U u ) where U : IList < T > ; } class Class : [|IInterface1 < int >|] { } ",
@"using System; interface IInterface1 < T > { void Method1 < U > ( T t , U u ) where U : IList < T > ; } class Class : IInterface1 < int > { public void Method1 < U > ( int t , U u ) where U : IList<int> { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementGenericTypeWithGenericMethodWithUnexpressibleConstraint()
        {
            Test(
@"interface IInterface1 < T > { void Method1 < U > ( T t , U u ) where U : T ; } class Class : [|IInterface1 < int >|] { } ",
@"using System; interface IInterface1 < T > { void Method1 < U > ( T t , U u ) where U : T ; } class Class : IInterface1 < int > { void IInterface1 < int > . Method1 < U > ( int t , U u ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestArrayType()
        {
            Test(
@"interface I { string [ ] M ( ) ; } class C : [|I|] { } ",
@"using System; interface I { string [ ] M ( ) ; } class C : I { public string [ ] M ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementThroughFieldMember()
        {
            Test(
@"interface I { void Method1 ( ) ; } class C : [|I|] { I i ; } ",
@"interface I { void Method1 ( ) ; } class C : I { I i ; public void Method1 ( ) { i . Method1 ( ) ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementThroughFieldMemberInterfaceWithIndexer()
        {
            Test(
@"interface IFoo { int this[int x] { get; set; } } class Foo : [|IFoo|] { IFoo f; }",
@"interface IFoo { int this[int x] { get; set; } } class Foo : IFoo { IFoo f; public int this[int x] { get { return f[x]; } set { f[x] = value; } } }",
index: 1);
        }

        [WorkItem(472, "https://github.com/dotnet/roslyn/issues/472")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementThroughFieldMemberRemoveUnnecessaryCast()
        {
            Test(
@"using System.Collections; sealed class X : [|IComparer|] { X x; }",
@"using System.Collections; sealed class X : IComparer { X x; public int Compare(object x, object y) { return this.x.Compare(x, y); } }",
index: 1);
        }

        [WorkItem(472, "https://github.com/dotnet/roslyn/issues/472")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementThroughFieldMemberRemoveUnnecessaryCastAndThis()
        {
            Test(
@"using System.Collections; sealed class X : [|IComparer|] { X a; }",
@"using System.Collections; sealed class X : IComparer { X a; public int Compare(object x, object y) { return a.Compare(x, y); } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementAbstract()
        {
            Test(
@"interface I { void Method1 ( ) ; } abstract class C : [|I|] { } ",
@"interface I { void Method1 ( ) ; } abstract class C : I { public abstract void Method1 ( ) ; } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceWithRefOutParameters()
        {
            Test(
@"class C : [|I|] { I foo ; } interface I { void Method1 ( ref int x , out int y , int z ) ; int Method2 ( ) ; } ",
@"class C : I { I foo ; public void Method1 ( ref int x , out int y , int z ) { foo . Method1 ( ref x , out y , z ) ; } public int Method2 ( ) { return foo . Method2 ( ) ; } } interface I { void Method1 ( ref int x , out int y , int z ) ; int Method2 ( ) ; } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestConflictingMethods1()
        {
            Test(
@"class B { public int Method1 ( ) { } } class C : B , [|I|] { } interface I { void Method1 ( ) ; } ",
@"using System; class B { public int Method1 ( ) { } } class C : B , I { void I.Method1 ( ) { throw new NotImplementedException ( ) ; } } interface I { void Method1 ( ) ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestConflictingProperties()
        {
            Test(
@"class Test : [|I1|] { int Prop { get ; set ; } } interface I1 { int Prop { get ; set ; } } ",
@"using System ; class Test : I1 { int I1 . Prop { get { throw new NotImplementedException ( ) ; } set { throw new NotImplementedException ( ) ; } } int Prop { get ; set ; } } interface I1 { int Prop { get ; set ; } } ");
        }

        [WorkItem(539043)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestExplicitProperties()
        {
            TestMissing(
@"interface I2 { decimal Calc { get ; } } class C : [|I2|] { protected decimal pay ; decimal I2 . Calc { get { return pay ; } } } ");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEscapedMethodName()
        {
            Test(
@"interface IInterface { void @M ( ) ; } class Class : [|IInterface|] { } ",
@"using System; interface IInterface { void @M ( ) ; } class Class : IInterface { public void M ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEscapedMethodKeyword()
        {
            Test(
@"interface IInterface { void @int ( ) ; } class Class : [|IInterface|] { } ",
@"using System; interface IInterface { void @int ( ) ; } class Class : IInterface { public void @int ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEscapedInterfaceName1()
        {
            Test(
@"interface @IInterface { void M ( ) ; } class Class : [|@IInterface|] { string M ( ) ; } ",
@"using System; interface @IInterface { void M ( ) ; } class Class : @IInterface { void IInterface . M ( ) { throw new NotImplementedException ( ) ; } string M ( ) ; } ");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEscapedInterfaceName2()
        {
            Test(
@"interface @IInterface { void @M ( ) ; } class Class : [|@IInterface|] { string M ( ) ; } ",
@"using System; interface @IInterface { void @M ( ) ; } class Class : @IInterface { void IInterface . M ( ) { throw new NotImplementedException ( ) ; } string M ( ) ; } ");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEscapedInterfaceKeyword1()
        {
            Test(
@"interface @int { void M ( ) ; } class Class : [|@int|] { string M ( ) ; } ",
@"using System; interface @int { void M ( ) ; } class Class : @int { void @int . M ( ) { throw new NotImplementedException ( ) ; } string M ( ) ; } ");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEscapedInterfaceKeyword2()
        {
            Test(
@"interface @int { void @bool ( ) ; } class Class : [|@int|] { string @bool ( ) ; } ",
@"using System; interface @int { void @bool ( ) ; } class Class : @int { void @int . @bool ( ) { throw new NotImplementedException ( ) ; } string @bool ( ) ; } ");
        }

        [WorkItem(539522)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestPropertyFormatting()
        {
            Test(
@"public interface DD
{
    int Prop { get; set; }
}
public class A : [|DD|]
{
}",
@"using System;

public interface DD
{
    int Prop { get; set; }
}
public class A : DD
{
    public int Prop
    {
        get
        {
            throw new NotImplementedException();
        }

        set
        {
            throw new NotImplementedException();
        }
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestCommentPlacement()
        {
            Test(
@"public interface DD
{
    void Foo();
}
public class A : [|DD|]
{
    //comments
}",
@"using System;

public interface DD
{
    void Foo();
}
public class A : DD
{
    //comments
    public void Foo()
    {
        throw new NotImplementedException();
    }
}",
compareTokens: false);
        }

        [WorkItem(539991)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestBracePlacement()
        {
            Test(
@"using System;
class C : [|IServiceProvider|]",
@"using System;
class C : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
",
compareTokens: false);
        }

        [WorkItem(540318)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMissingWithIncompleteMember()
        {
            TestMissing(
@"interface ITest { void Method ( ) ; } class Test : [|ITest|] { p public void Method ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(541380)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestExplicitProperty()
        {
            Test(
@"interface i1 { int p { get ; set ; } } class c1 : [|i1|] { } ",
@"using System ; interface i1 { int p { get ; set ; } } class c1 : i1 { int i1 . p { get { throw new NotImplementedException ( ) ; } set { throw new NotImplementedException ( ) ; } } } ",
index: 1);
        }

        [WorkItem(541981)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoDelegateThroughField1()
        {
            TestActionCount(
@"interface I { void Method1 ( ) ; } class C : [|I|] { I i { get ; set ; } } ",
count: 3);
            Test(
@"interface I { void Method1 ( ) ; } class C : [|I|] { I i { get ; set ; } } ",
@"using System ; interface I { void Method1 ( ) ; } class C : I { I i { get ; set ; } public void Method1 ( ) { throw new NotImplementedException ( ) ; } } ",
index: 0);
            Test(
@"interface I { void Method1 ( ) ; } class C : [|I|] { I i { get ; set ; } } ",
@"interface I { void Method1 ( ) ; } class C : I { I i { get ; set ; } public void Method1 ( ) { i . Method1 ( ); } } ",
index: 1);
            Test(
@"interface I { void Method1 ( ) ; } class C : [|I|] { I i { get ; set ; } } ",
@"using System ; interface I { void Method1 ( ) ; } class C : I { I i { get ; set ; } void I . Method1 ( ) { throw new NotImplementedException ( ) ; } } ",
index: 2);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIReadOnlyListThroughField()
        {
            Test(
@"using System.Collections.Generic; class A : [|IReadOnlyList<int>|] { int[] field; }",
@"using System.Collections; using System.Collections.Generic; class A : IReadOnlyList<int> {
int[] field;
public int this[int index] { get { return ((IReadOnlyList<int>)field)[index]; } }
public int Count { get { return ((IReadOnlyList<int>)field).Count; } }
public IEnumerator<int> GetEnumerator() { return ((IReadOnlyList<int>)field).GetEnumerator(); }
IEnumerator IEnumerable.GetEnumerator() { return ((IReadOnlyList<int>)field).GetEnumerator(); } }",
index: 1);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIReadOnlyListThroughProperty()
        {
            Test(
@"using System.Collections.Generic; class A : [|IReadOnlyList<int>|] { int[] field { get; set; } }",
@"using System.Collections; using System.Collections.Generic; class A : IReadOnlyList<int> { 
public int this[int index] { get { return ((IReadOnlyList<int>)field)[index]; } } 
public int Count { get { return ((IReadOnlyList<int>)field).Count; } }
int[] field { get; set; }
public IEnumerator<int> GetEnumerator() { return ((IReadOnlyList<int>)field).GetEnumerator(); }
IEnumerator IEnumerable.GetEnumerator() { return ((IReadOnlyList<int>)field).GetEnumerator(); } }",
index: 1);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceThroughField()
        {
            Test(
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : [|I|] { A a; }",
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : I { A a; public int M() { return ((I)a).M(); } }",
index: 1);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceThroughField_FieldImplementsMultipleInterfaces()
        {
            TestActionCount(
@"interface I { int M(); } interface I2 { int M2() } class A : I, I2 { int I.M() { return 0; } int I2.M2() { return 0; } } class B : [|I|], I2 { A a; }",
count: 3);
            TestActionCount(
@"interface I { int M(); } interface I2 { int M2() } class A : I, I2 { int I.M() { return 0; } int I2.M2() { return 0; } } class B : I, [|I2|] { A a; }",
count: 3);
            Test(
@"interface I { int M(); } interface I2 { int M2() } class A : I, I2 { int I.M() { return 0; } int I2.M2() { return 0; } } class B : [|I|], I2 { A a; }",
@"interface I { int M(); } interface I2 { int M2() } class A : I, I2 { int I.M() { return 0; } int I2.M2() { return 0; } } class B : I, I2 { A a; public int M() { return ((I)a).M(); } }",
index: 1);
            Test(
@"interface I { int M(); } interface I2 { int M2() } class A : I, I2 { int I.M() { return 0; } int I2.M2() { return 0; } } class B : I, [|I2|] { A a; }",
@"interface I { int M(); } interface I2 { int M2() } class A : I, I2 { int I.M() { return 0; } int I2.M2() { return 0; } } class B : I, I2 { A a; public int M2() { return ((I2)a).M2(); } }",
index: 1);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceThroughField_MultipleFieldsCanImplementInterface()
        {
            TestActionCount(
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : [|I|] { A a; A aa; }",
count: 4);
            Test(
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : [|I|] { A a; A aa; }",
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : I { A a; A aa; public int M() { return ((I)a).M(); } }",
index: 1);
            Test(
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : [|I|] { A a; A aa; }",
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : I { A a; A aa; public int M() { return ((I)aa).M(); } }",
index: 2);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceThroughField_MultipleFieldsForMultipleInterfaces()
        {
            TestActionCount(
@"interface I { int M(); } interface I2 { int M2() } class A : I { int I.M() { return 0; } } class B : I2 { int I2.M2() { return 0; } } class C : [|I|], I2 { A a; B b; }",
count: 3);
            TestActionCount(
@"interface I { int M(); } interface I2 { int M2() } class A : I { int I.M() { return 0; } } class B : I2 { int I2.M2() { return 0; } } class C : I, [|I2|] { A a; B b; }",
count: 3);
            Test(
@"interface I { int M(); } interface I2 { int M2() } class A : I { int I.M() { return 0; } } class B : I2 { int I2.M2() { return 0; } } class C : [|I|], I2 { A a; B b; }",
@"interface I { int M(); } interface I2 { int M2() } class A : I { int I.M() { return 0; } } class B : I2 { int I2.M2() { return 0; } } class C : I, I2 { A a; B b; public int M() { return ((I)a).M(); } }",
index: 1);
            Test(
@"interface I { int M(); } interface I2 { int M2() } class A : I { int I.M() { return 0; } } class B : I2 { int I2.M2() { return 0; } } class C : I, [|I2|] { A a; B b; }",
@"interface I { int M(); } interface I2 { int M2() } class A : I { int I.M() { return 0; } } class B : I2 { int I2.M2() { return 0; } } class C : I, I2 { A a; B b; public int M2() { return ((I2)b).M2(); } }",
index: 1);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoImplementThroughIndexer()
        {
            TestActionCount(
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : [|I|] { A this[int index] { get { return null; } }; }",
count: 2);
        }

        [WorkItem(768799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoImplementThroughWriteOnlyProperty()
        {
            TestActionCount(
@"interface I { int M(); } class A : I { int I.M() { return 0; } } class B : [|I|] { A a { set { } } }",
count: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementEvent()
        {
            Test(
@"interface IFoo { event System . EventHandler E ; } abstract class Foo : [|IFoo|] { } ",
@"using System ; interface IFoo { event System . EventHandler E ; } abstract class Foo : IFoo { public event EventHandler E ; } ",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementEventAbstractly()
        {
            Test(
@"interface IFoo { event System . EventHandler E ; } abstract class Foo : [|IFoo|] { } ",
@"using System ; interface IFoo { event System . EventHandler E ; } abstract class Foo : IFoo { public abstract event EventHandler E ; } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementEventExplicitly()
        {
            Test(
@"interface IFoo { event System . EventHandler E ; } abstract class Foo : [|IFoo|] { } ",
@"using System ; interface IFoo { event System . EventHandler E ; } abstract class Foo : IFoo { event EventHandler IFoo . E { add { throw new NotImplementedException ( ) ; } remove { throw new NotImplementedException ( ) ; } } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestFaultToleranceInStaticMembers()
        {
            Test(
@"interface IFoo { static string Name { set ; get ; } static int Foo ( string s ) ; } class Program : [|IFoo|] { } ",
@"using System ; interface IFoo { static string Name { set ; get ; } static int Foo ( string s ) ; } class Program : IFoo { public string Name { get { throw new NotImplementedException ( ) ; } set { throw new NotImplementedException ( ) ; } } public int Foo ( string s ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestIndexers()
        {
            Test(
@"public interface ISomeInterface { int this [ int index ] { get ; set ; } } class IndexerClass : [|ISomeInterface|] { } ",
@"using System ; public interface ISomeInterface { int this [ int index ] { get ; set ; } } class IndexerClass : ISomeInterface { public int this [ int index ] { get { throw new NotImplementedException ( ) ; } set { throw new NotImplementedException ( ) ; } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestIndexersExplicit()
        {
            Test(
@"public interface ISomeInterface { int this [ int index ] { get ; set ; } } class IndexerClass : [|ISomeInterface|] { } ",
@"using System ; public interface ISomeInterface { int this [ int index ] { get ; set ; } } class IndexerClass : ISomeInterface { int ISomeInterface . this [ int index ] { get { throw new NotImplementedException ( ) ; } set { throw new NotImplementedException ( ) ; } } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestIndexersWithASingleAccessor()
        {
            Test(
@"public interface ISomeInterface { int this [ int index ] { get ; } } class IndexerClass : [|ISomeInterface|] { } ",
@"using System ; public interface ISomeInterface { int this [ int index ] { get ; } } class IndexerClass : ISomeInterface { public int this [ int index ] { get { throw new NotImplementedException ( ) ; } } } ");
        }

        [WorkItem(542357)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestConstraints1()
        {
            Test(
@"interface I { void Foo < T > ( ) where T : class ; } class A : [|I|] { } ",
@"using System ; interface I { void Foo < T > ( ) where T : class ; } class A : I { public void Foo < T > ( ) where T : class { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542357)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestConstraintsExplicit()
        {
            Test(
@"interface I { void Foo < T > ( ) where T : class ; } class A : [|I|] { } ",
@"using System ; interface I { void Foo < T > ( ) where T : class ; } class A : I { void I . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(542357)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUsingAddedForConstraint()
        {
            Test(
@"interface I { void Foo < T > ( ) where T : System . Attribute ; } class A : [|I|] { } ",
@"using System ; interface I { void Foo < T > ( ) where T : System . Attribute ; } class A : I { public void Foo < T > ( ) where T : Attribute { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542379)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestIndexer()
        {
            Test(
@"interface I { int this [ int x ] { get ; set ; } } class C : [|I|] { } ",
@"using System ; interface I { int this [ int x ] { get ; set ; } } class C : I { public int this [ int x ] { get { throw new NotImplementedException ( ) ; } set { throw new NotImplementedException ( ) ; } } } ");
        }

        [WorkItem(542588)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestRecursiveConstraint1()
        {
            Test(
@"using System ; interface I { void Foo < T > ( ) where T : IComparable < T > ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo < T > ( ) where T : IComparable < T > ; } class C : I { public void Foo < T > ( ) where T : IComparable < T > { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542588)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestRecursiveConstraint2()
        {
            Test(
@"using System ; interface I { void Foo < T > ( ) where T : IComparable < T > ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo < T > ( ) where T : IComparable < T > ; } class C : I { void I . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint1()
        {
            Test(
@"interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : [|I < string >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : I < string > { void I < string > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint2()
        {
            Test(
@"interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : [|I < object >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : I < object > { public void Foo < T > ( ) where T : class { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint3()
        {
            Test(
@"interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : [|I < object >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : I < object > { void I < object > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint4()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : [|I < Delegate >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : I < Delegate > { void I < Delegate > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint5()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : [|I < MulticastDelegate >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : I < MulticastDelegate > { void I < MulticastDelegate > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint6()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } delegate void Bar ( ) ; class A : [|I < Bar >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } delegate void Bar ( ) ; class A : I < Bar > { void I < Bar > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint7()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : [|I < Enum >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : I < Enum > { void I < Enum > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint8()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : [|I < int [ ] >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class A : I < int [ ] > { void I < int [ ] > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint9()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } enum E { } class A : [|I < E >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } enum E { } class A : I < E > { void I < E > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542621)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnexpressibleConstraint10()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : S ; } class A : [|I < ValueType >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : S ; } class A : I < ValueType > { void I < ValueType > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542669)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestArrayConstraint()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : S ; } class C : [|I < Array >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : S ; } class C : I < Array > { void I < Array > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542743)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMultipleClassConstraints()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : Exception , S ; } class C : [|I < Attribute >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : Exception , S ; } class C : I < Attribute > { void I < Attribute > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542751)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestClassConstraintAndRefConstraint()
        {
            Test(
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class C : [|I < Exception >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( ) where T : class , S ; } class C : I < Exception > { void I < Exception > . Foo < T > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542505)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestRenameConflictingTypeParameters1()
        {
            Test(
@"using System ; using System . Collections . Generic ; interface I < T > { void Foo < S > ( T x , IList < S > list ) where S : T ; } class A < S > : [|I < S >|] { } ",
@"using System ; using System . Collections . Generic ; interface I < T > { void Foo < S > ( T x , IList < S > list ) where S : T ; } class A < S > : I < S > { public void Foo < S1 > ( S x , IList < S1 > list ) where S1 : S { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542505)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestRenameConflictingTypeParameters2()
        {
            Test(
@"using System ; using System . Collections . Generic ; interface I < T > { void Foo < S > ( T x , IList < S > list ) where S : T ; } class A < S > : [|I < S >|] { } ",
@"using System ; using System . Collections . Generic ; interface I < T > { void Foo < S > ( T x , IList < S > list ) where S : T ; } class A < S > : I < S > { void I < S > . Foo < S1 > ( S x , IList < S1 > list ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(542505)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestRenameConflictingTypeParameters3()
        {
            Test(
@"using System ; using System . Collections . Generic ; interface I < X , Y > { void Foo < A , B > ( X x , Y y , IList < A > list1 , IList < B > list2 ) where A : IList < B > where B : IList < A > ; } class C < A , B > : [|I < A , B >|] { } ",
@"using System ; using System . Collections . Generic ; interface I < X , Y > { void Foo < A , B > ( X x , Y y , IList < A > list1 , IList < B > list2 ) where A : IList < B > where B : IList < A > ; } class C < A , B > : I < A , B > { public void Foo < A1 , B1 > ( A x , B y , IList < A1 > list1 , IList < B1 > list2 ) where A1 : IList < B1 > where B1 : IList < A1 > { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(542505)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestRenameConflictingTypeParameters4()
        {
            Test(
@"using System ; using System . Collections . Generic ; interface I < X , Y > { void Foo < A , B > ( X x , Y y , IList < A > list1 , IList < B > list2 ) where A : IList < B > where B : IList < A > ; } class C < A , B > : [|I < A , B >|] { } ",
@"using System ; using System . Collections . Generic ; interface I < X , Y > { void Foo < A , B > ( X x , Y y , IList < A > list1 , IList < B > list2 ) where A : IList < B > where B : IList < A > ; } class C < A , B > : I < A , B > { void I < A , B > . Foo < A1 , B1 > ( A x , B y , IList < A1 > list1 , IList < B1 > list2 ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(542506)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNameSimplification()
        {
            Test(
@"using System ; class A < T > { class B { } interface I { void Foo ( B x ) ; } class C < U > : [|I|] { } } ",
@"using System ; class A < T > { class B { } interface I { void Foo ( B x ) ; } class C < U > : I { public void Foo ( B x ) { throw new NotImplementedException ( ) ; } } } ");
        }

        [WorkItem(542506)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNameSimplification2()
        {
            Test(
@"class A < T > { class B { } interface I { void Foo ( B [ ] x ) ; } class C < U > : [|I|] { } } ",
@"using System ; class A < T > { class B { } interface I { void Foo ( B [ ] x ) ; } class C < U > : I { public void Foo ( B [ ] x ) { throw new NotImplementedException ( ) ; } } } ");
        }

        [WorkItem(542506)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNameSimplification3()
        {
            Test(
@"class A < T > { class B { } interface I { void Foo ( B [ ] [ , ] [ , , ] [ , , , ] x ) ; } class C < U > : [|I|] { } } ",
@"using System ; class A < T > { class B { } interface I { void Foo ( B [ ] [ , ] [ , , ] [ , , , ] x ) ; } class C < U > : I { public void Foo ( B [ ] [ , ] [ , , ] [ , , , ] x ) { throw new NotImplementedException ( ) ; } } } ");
        }

        [WorkItem(544166)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementAbstractProperty()
        {
            Test(
@"interface IFoo { int Gibberish { get ; set ; } } abstract class Foo : [|IFoo|] { } ",
@"interface IFoo { int Gibberish { get ; set ; } } abstract class Foo : IFoo { public abstract int Gibberish { get ; set ; } } ",
index: 1);
        }

        [WorkItem(544210)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMissingOnWrongArity()
        {
            TestMissing(
@"interface I1<T> { int X { get; set; } } class C : [|I1|] { }");
        }

        [WorkItem(544281)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplicitDefaultValue()
        {
            Test(
@"interface IOptional { int Foo ( int g = 0 ) ; } class Opt : [|IOptional|] { } ",
@"using System ; interface IOptional { int Foo ( int g = 0 ) ; } class Opt : IOptional { public int Foo ( int g = 0 ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(544281)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestExplicitDefaultValue()
        {
            Test(
@"interface IOptional { int Foo ( int g = 0 ) ; } class Opt : [|IOptional|] { } ",
@"using System ; interface IOptional { int Foo ( int g = 0 ) ; } class Opt : IOptional { int IOptional . Foo ( int g ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMissingInHiddenType()
        {
            TestMissing(
@"using System;

class Program : [|IComparable|]
{
#line hidden
}
#line default");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestGenerateIntoVisiblePart()
        {
            Test(
@"#line default
using System;

partial class Program : [|IComparable|]
{
    void Foo()
    {
#line hidden
    }
}
#line default",
@"#line default
using System;

partial class Program : IComparable
{
    public int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }

    void Foo()
    {
#line hidden
    }
}
#line default",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestGenerateIfAvailableRegionExists()
        {
            Test(
@"using System;

partial class Program : [|IComparable|]
{
#line hidden
}
#line default

partial class Program
{
}",
@"using System;

partial class Program : IComparable
{
#line hidden
}
#line default

partial class Program
{
    public int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}",
compareTokens: false);
        }

        [WorkItem(545334)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoGenerateInVenusCase1()
        {
            TestMissing(
@"using System;
#line 1 ""Bar""
class Foo : [|IComparable|]
#line default
#line hidden
");
        }

        [WorkItem(545476)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestOptionalDateTime1()
        {
            Test(
@"using System ; using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo ( [ Optional ] [ DateTimeConstant ( 100 ) ] DateTime x ) ; } public class C : [|IFoo|] { } ",
@"using System ; using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo ( [ Optional ] [ DateTimeConstant ( 100 ) ] DateTime x ) ; } public class C : IFoo { public void Foo ( [ DateTimeConstant ( 100 ), Optional ] DateTime x ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545476)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestOptionalDateTime2()
        {
            Test(
@"using System ; using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo ( [ Optional ] [ DateTimeConstant ( 100 ) ] DateTime x ) ; } public class C : [|IFoo|] { } ",
@"using System ; using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo ( [ Optional ] [ DateTimeConstant ( 100 ) ] DateTime x ) ; } public class C : IFoo { void IFoo . Foo ( DateTime x ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(545477)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestIUnknownIDispatchAttributes1()
        {
            Test(
@"using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo1 ( [ Optional ] [ IUnknownConstant ] object x ) ; void Foo2 ( [ Optional ] [ IDispatchConstant ] object x ) ; } public class C : [|IFoo|] { } ",
@"using System ; using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo1 ( [ Optional ] [ IUnknownConstant ] object x ) ; void Foo2 ( [ Optional ] [ IDispatchConstant ] object x ) ; } public class C : IFoo { public void Foo1 ( [ IUnknownConstant, Optional ] object x ) { throw new NotImplementedException ( ) ; } public void Foo2 ( [ IDispatchConstant, Optional ] object x ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545477)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestIUnknownIDispatchAttributes2()
        {
            Test(
@"using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo1 ( [ Optional ] [ IUnknownConstant ] object x ) ; void Foo2 ( [ Optional ] [ IDispatchConstant ] object x ) ; } public class C : [|IFoo|] { } ",
@"using System ; using System . Runtime . CompilerServices ; using System . Runtime . InteropServices ; interface IFoo { void Foo1 ( [ Optional ] [ IUnknownConstant ] object x ) ; void Foo2 ( [ Optional ] [ IDispatchConstant ] object x ) ; } public class C : IFoo { void IFoo . Foo1 ( object x ) { throw new NotImplementedException ( ) ; } void IFoo . Foo2 ( object x ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(545464)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestTypeNameConflict()
        {
            Test(
@"interface IFoo { void Foo ( ) ; } public class Foo : [|IFoo|] { } ",
@"using System ; interface IFoo { void Foo ( ) ; } public class Foo : IFoo { void IFoo . Foo ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestStringLiteral()
        {
            Test(
@"interface IFoo { void Foo ( string s = ""\"""" ) ; } class B : [|IFoo|] { } ",
@"using System ; interface IFoo { void Foo ( string s = ""\"""" ) ; } class B : IFoo { public void Foo ( string s = ""\"""" ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(916114)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestOptionalNullableStructParameter1()
        {
            Test(
@"struct b { } interface d { void m ( b? x = null, b? y = default(b?) ) ; } class c : [|d|] { }",
@"using System ; struct b { } interface d { void m ( b? x = null, b? y = default(b?) ) ; } class c : d { public void m ( b? x = default(b?), b? y = default(b?) ) { throw new NotImplementedException ( ) ; } }");
        }

        [WorkItem(916114)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestOptionalNullableStructParameter2()
        {
            Test(
@"struct b { } interface d { void m ( b? x = null, b? y = default(b?) ) ; } class c : [|d|] { }",
@"using System ; struct b { } interface d { void m ( b? x = null, b? y = default(b?) ) ; } class c : d { void d.m ( b? x, b? y ) { throw new NotImplementedException ( ) ; } }", 1);
        }

        [WorkItem(916114)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestOptionalNullableIntParameter()
        {
            Test(
@"interface d { void m ( int? x = 5, int? y = null ) ; } class c : [|d|] { }",
@"using System ; interface d { void m ( int? x = 5, int? y = null ) ; } class c : d { public void m ( int? x = 5, int? y = default(int?) ) { throw new NotImplementedException ( ) ; } }");
        }

        [WorkItem(545613)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestOptionalWithNoDefaultValue()
        {
            Test(
@"using System . Runtime . InteropServices ; interface I { void Foo ( [ Optional ] I o ) ; } class C : [|I|] { } ",
@"using System ; using System . Runtime . InteropServices ; interface I { void Foo ( [ Optional ] I o ) ; } class C : I { public void Foo ( [ Optional ] I o ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestIntegralAndFloatLiterals()
        {
            Test(
@"interface I
{
    void M01(short s = short.MinValue);
    void M02(short s = -1);
    void M03(short s = short.MaxValue);
    void M04(ushort s = ushort.MinValue);
    void M05(ushort s = 1);
    void M06(ushort s = ushort.MaxValue);
    void M07(int s = int.MinValue);
    void M08(int s = -1);
    void M09(int s = int.MaxValue);
    void M10(uint s = uint.MinValue);
    void M11(uint s = 1);
    void M12(uint s = uint.MaxValue);
    void M13(long s = long.MinValue);
    void M14(long s = -1);
    void M15(long s = long.MaxValue);
    void M16(ulong s = ulong.MinValue);
    void M17(ulong s = 1);
    void M18(ulong s = ulong.MaxValue);
    void M19(float s = float.MinValue);
    void M20(float s = 1);
    void M21(float s = float.MaxValue);
    void M22(double s = double.MinValue);
    void M23(double s = 1);
    void M24(double s = double.MaxValue);
}

class C : [|I|]
{
}",
@"using System;

interface I
{
    void M01(short s = short.MinValue);
    void M02(short s = -1);
    void M03(short s = short.MaxValue);
    void M04(ushort s = ushort.MinValue);
    void M05(ushort s = 1);
    void M06(ushort s = ushort.MaxValue);
    void M07(int s = int.MinValue);
    void M08(int s = -1);
    void M09(int s = int.MaxValue);
    void M10(uint s = uint.MinValue);
    void M11(uint s = 1);
    void M12(uint s = uint.MaxValue);
    void M13(long s = long.MinValue);
    void M14(long s = -1);
    void M15(long s = long.MaxValue);
    void M16(ulong s = ulong.MinValue);
    void M17(ulong s = 1);
    void M18(ulong s = ulong.MaxValue);
    void M19(float s = float.MinValue);
    void M20(float s = 1);
    void M21(float s = float.MaxValue);
    void M22(double s = double.MinValue);
    void M23(double s = 1);
    void M24(double s = double.MaxValue);
}

class C : I
{
    public void M01(short s = short.MinValue)
    {
        throw new NotImplementedException();
    }

    public void M02(short s = -1)
    {
        throw new NotImplementedException();
    }

    public void M03(short s = short.MaxValue)
    {
        throw new NotImplementedException();
    }

    public void M04(ushort s = 0)
    {
        throw new NotImplementedException();
    }

    public void M05(ushort s = 1)
    {
        throw new NotImplementedException();
    }

    public void M06(ushort s = ushort.MaxValue)
    {
        throw new NotImplementedException();
    }

    public void M07(int s = int.MinValue)
    {
        throw new NotImplementedException();
    }

    public void M08(int s = -1)
    {
        throw new NotImplementedException();
    }

    public void M09(int s = int.MaxValue)
    {
        throw new NotImplementedException();
    }

    public void M10(uint s = 0)
    {
        throw new NotImplementedException();
    }

    public void M11(uint s = 1)
    {
        throw new NotImplementedException();
    }

    public void M12(uint s = uint.MaxValue)
    {
        throw new NotImplementedException();
    }

    public void M13(long s = long.MinValue)
    {
        throw new NotImplementedException();
    }

    public void M14(long s = -1)
    {
        throw new NotImplementedException();
    }

    public void M15(long s = long.MaxValue)
    {
        throw new NotImplementedException();
    }

    public void M16(ulong s = 0)
    {
        throw new NotImplementedException();
    }

    public void M17(ulong s = 1)
    {
        throw new NotImplementedException();
    }

    public void M18(ulong s = ulong.MaxValue)
    {
        throw new NotImplementedException();
    }

    public void M19(float s = float.MinValue)
    {
        throw new NotImplementedException();
    }

    public void M20(float s = 1)
    {
        throw new NotImplementedException();
    }

    public void M21(float s = float.MaxValue)
    {
        throw new NotImplementedException();
    }

    public void M22(double s = double.MinValue)
    {
        throw new NotImplementedException();
    }

    public void M23(double s = 1)
    {
        throw new NotImplementedException();
    }

    public void M24(double s = double.MaxValue)
    {
        throw new NotImplementedException();
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEnumLiterals()
        {
            Test(
@"using System;

enum E
{
   A = 1,
   B = 2  
}

[FlagsAttribute]
enum FlagE
{
   A = 1,
   B = 2
}

interface I
{
    void M1(E e = E.A | E.B);
    void M2(FlagE e = FlagE.A | FlagE.B);
}

class C : [|I|]
{
}",
@"using System;

enum E
{
   A = 1,
   B = 2  
}

[FlagsAttribute]
enum FlagE
{
   A = 1,
   B = 2
}

interface I
{
    void M1(E e = E.A | E.B);
    void M2(FlagE e = FlagE.A | FlagE.B);
}

class C : I
{
    public void M1(E e = (E)3)
    {
        throw new NotImplementedException();
    }

    public void M2(FlagE e = FlagE.A | FlagE.B)
    {
        throw new NotImplementedException();
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestCharLiterals()
        {
            Test(
@"using System;

interface I
{
    void M01(char c = '\0');
    void M02(char c = '\r');
    void M03(char c = '\n');
    void M04(char c = '\t');
    void M05(char c = '\b');
    void M06(char c = '\v');
    void M07(char c = '\'');
    void M08(char c = '“');
    void M09(char c = 'a');
    void M10(char c = '""');
    void M11(char c = '\u2029');
}

class C : [|I|]
{
}",
@"using System;

interface I
{
    void M01(char c = '\0');
    void M02(char c = '\r');
    void M03(char c = '\n');
    void M04(char c = '\t');
    void M05(char c = '\b');
    void M06(char c = '\v');
    void M07(char c = '\'');
    void M08(char c = '“');
    void M09(char c = 'a');
    void M10(char c = '""');
    void M11(char c = '\u2029');
}

class C : I
{
    public void M01(char c = '\0')
    {
        throw new NotImplementedException();
    }

    public void M02(char c = '\r')
    {
        throw new NotImplementedException();
    }

    public void M03(char c = '\n')
    {
        throw new NotImplementedException();
    }

    public void M04(char c = '\t')
    {
        throw new NotImplementedException();
    }

    public void M05(char c = '\b')
    {
        throw new NotImplementedException();
    }

    public void M06(char c = '\v')
    {
        throw new NotImplementedException();
    }

    public void M07(char c = '\'')
    {
        throw new NotImplementedException();
    }

    public void M08(char c = '“')
    {
        throw new NotImplementedException();
    }

    public void M09(char c = 'a')
    {
        throw new NotImplementedException();
    }

    public void M10(char c = '""')
    {
        throw new NotImplementedException();
    }

    public void M11(char c = '\u2029')
    {
        throw new NotImplementedException();
    }
}",
compareTokens: false);
        }

        [WorkItem(545695)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestRemoveParenthesesAroundTypeReference1()
        {
            Test(
@"using System ; interface I { void Foo ( DayOfWeek x = DayOfWeek . Friday ) ; } class C : [|I|] { DayOfWeek DayOfWeek { get ; set ; } } ",
@"using System ; interface I { void Foo ( DayOfWeek x = DayOfWeek . Friday ) ; } class C : I { DayOfWeek DayOfWeek { get ; set ; } public void Foo ( DayOfWeek x = DayOfWeek . Friday ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545696)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDecimalConstants1()
        {
            Test(
@"interface I { void Foo ( decimal x = decimal . MaxValue ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo ( decimal x = decimal . MaxValue ) ; } class C : I { public void Foo ( decimal x = decimal . MaxValue ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545711)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNullablePrimitiveLiteral()
        {
            Test(
@"interface I { void Foo ( decimal ? x = decimal . MaxValue ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo ( decimal ? x = decimal . MaxValue ) ; } class C : I { public void Foo ( decimal ? x = decimal . MaxValue ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545715)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNullableEnumType()
        {
            Test(
@"using System ; interface I { void Foo ( DayOfWeek ? x = DayOfWeek . Friday ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo ( DayOfWeek ? x = DayOfWeek . Friday ) ; } class C : I { public void Foo ( DayOfWeek ? x = DayOfWeek . Friday ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545752)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestByteLiterals()
        {
            Test(
@"interface I { void Foo ( byte x = 1 ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo ( byte x = 1 ) ; } class C : I { public void Foo ( byte x = 1 ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545736)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestCastedOptionalParameter1()
        {
            const string code = @"
using System;
interface I
{
    void Foo(ConsoleColor x = (ConsoleColor)(-1));
}

class C : [|I|]
{
}";

            const string expected = @"
using System;
interface I
{
    void Foo(ConsoleColor x = (ConsoleColor)(-1));
}

class C : I
{
    public void Foo(ConsoleColor x = (ConsoleColor)(-1))
    {
        throw new NotImplementedException();
    }
}";

            Test(code, expected, compareTokens: false);
        }

        [WorkItem(545737)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestCastedEnumValue()
        {
            Test(
@"using System ; interface I { void Foo ( ConsoleColor x = ( ConsoleColor ) int . MaxValue ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo ( ConsoleColor x = ( ConsoleColor ) int . MaxValue ) ; } class C : I { public void Foo ( ConsoleColor x = ( ConsoleColor ) int . MaxValue ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545785)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoCastFromZeroToEnum()
        {
            Test(
@"enum E { A = 1 , } interface I { void Foo ( E x = 0 ) ; } class C : [|I|] { } ",
@"using System ; enum E { A = 1 , } interface I { void Foo ( E x = 0 ) ; } class C : I { public void Foo ( E x = 0 ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545793)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMultiDimArray()
        {
            Test(
@"using System . Runtime . InteropServices ; interface I { void Foo ( [ Optional ] [ DefaultParameterValue ( 1 ) ] int x , int [ , ] y ) ; } class C : [|I|] { } ",
@"using System ; using System . Runtime . InteropServices ; interface I { void Foo ( [ Optional ] [ DefaultParameterValue ( 1 ) ] int x , int [ , ] y ) ; } class C : I { public void Foo ( [ DefaultParameterValue ( 1 ), Optional ] int x = 1 , int [ , ] y = null ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545794)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestParametersAfterOptionalParameter()
        {
            Test(
@"using System . Runtime . InteropServices ; interface I { void Foo ( [ Optional , DefaultParameterValue ( 1 ) ] int x , int [ ] y , int [ ] z ) ; } class C : [|I|] { } ",
@"using System ; using System . Runtime . InteropServices ; interface I { void Foo ( [ Optional , DefaultParameterValue ( 1 ) ] int x , int [ ] y , int [ ] z ) ; } class C : I { public void Foo ( [ DefaultParameterValue ( 1 ), Optional ] int x = 1 , int [ ] y = null , int [ ] z = null ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545605)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestAttributeInParameter()
        {
            Test(
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface I
{
    void Foo([Optional][DateTimeConstant(100)] DateTime d1, [Optional][IUnknownConstant] object d2);
}
class C : [|I|]
{
}
",
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface I
{
    void Foo([Optional][DateTimeConstant(100)] DateTime d1, [Optional][IUnknownConstant] object d2);
}
class C : I
{
    public void Foo([DateTimeConstant(100), Optional] DateTime d1, [IUnknownConstant, Optional] object d2)
    {
        throw new NotImplementedException();
    }
}
",
compareTokens: false);
        }

        [WorkItem(545897)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNameConflictBetweenMethodAndTypeParameter()
        {
            Test(
@"interface I < S > { void T1 < T > ( S x , T y ) ; } class C < T > : [|I < T >|] { } ",
@"using System ; interface I < S > { void T1 < T > ( S x , T y ) ; } class C < T > : I < T > { public void T1 < T2 > ( T x , T2 y ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545895)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestTypeParameterReplacementWithOuterType()
        {
            Test(
@"using System . Collections . Generic ; interface I < S > { void Foo < T > ( S y , List < T > . Enumerator x ) ; } class D < T > : [|I < T >|] { } ",
@"using System ; using System . Collections . Generic ; interface I < S > { void Foo < T > ( S y , List < T > . Enumerator x ) ; } class D < T > : I < T > { public void Foo < T1 > ( T y , List < T1 > . Enumerator x ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545864)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestFloatConstant()
        {
            Test(
@"interface I { void Foo ( float x = 1E10F ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo ( float x = 1E10F ) ; } class C : I { public void Foo ( float x = 1E+10F ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(544640)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestKeywordForTypeParameterName()
        {
            Test(
@"interface I { void Foo < @class > ( ) ; } class C : [|I|] ",
@"using System ; interface I { void Foo < @class > ( ) ; } class C : I { public void Foo < @class > ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545922)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestExtremeDecimals()
        {
            Test(
@"interface I { void Foo1 ( decimal x = 1E28M ) ; void Foo2 ( decimal x = - 1E28M ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo1 ( decimal x = 1E28M ) ; void Foo2 ( decimal x = - 1E28M ) ; } class C : I { public void Foo1 ( decimal x = 10000000000000000000000000000M ) { throw new NotImplementedException ( ) ; } public void Foo2 ( decimal x = -10000000000000000000000000000M ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(544659)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNonZeroScaleDecimals()
        {
            Test(
@"interface I { void Foo ( decimal x = 0.1M ) ; } class C : [|I|] { } ",
@"using System ; interface I { void Foo ( decimal x = 0.1M ) ; } class C : I { public void Foo ( decimal x = 0.1M ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(544639)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnterminatedComment()
        {
            Test(
@"using System;
 
// Implement interface
class C : [|IServiceProvider|] /*
",
@"using System;

// Implement interface
class C : IServiceProvider /*
*/
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
", compareTokens: false);
        }

        [WorkItem(529920)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNewLineBeforeDirective()
        {
            Test(
@"using System;
 
// Implement interface
class C : [|IServiceProvider|]
#pragma warning disable
",
@"using System;

// Implement interface
class C : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
#pragma warning disable
", compareTokens: false);
        }

        [WorkItem(529947)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestCommentAfterInterfaceList1()
        {
            Test(
@"using System;
 
class C : [|IServiceProvider|] // Implement interface
",
@"using System;

class C : IServiceProvider // Implement interface
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
", compareTokens: false);
        }

        [WorkItem(529947)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestCommentAfterInterfaceList2()
        {
            Test(
@"using System;
 
class C : [|IServiceProvider|] 
// Implement interface
",
@"using System;

class C : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
// Implement interface
", compareTokens: false);
        }

        [WorkItem(994456)]
        [WorkItem(958699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposable_NoDisposePattern()
        {
            Test(
@"using System;
class C : [|IDisposable|]",
@"using System;
class C : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
", index: 0, compareTokens: false);
        }

        [WorkItem(994456)]
        [WorkItem(958699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposable_DisposePattern()
        {
            Test(
@"using System;
class C : [|IDisposable|]",
$@"using System;
class C : IDisposable
{{
{DisposePattern("protected virtual ", "C", "public void ")}
}}
", index: 1, compareTokens: false);
        }

        [WorkItem(994456)]
        [WorkItem(958699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableExplicitly_NoDisposePattern()
        {
            Test(
@"using System;
class C : [|IDisposable|]",
@"using System;
class C : IDisposable
{
    void IDisposable.Dispose()
    {
        throw new NotImplementedException();
    }
}
", index: 2, compareTokens: false);
        }

        [WorkItem(994456)]
        [WorkItem(941469)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableExplicitly_DisposePattern()
        {
            Test(
@"using System;
class C : [|System.IDisposable|]
{
    class IDisposable
    {
    }
}",
$@"using System;
class C : System.IDisposable
{{
    class IDisposable
    {{
    }}

{DisposePattern("protected virtual ", "C", "void System.IDisposable.")}
}}", index: 3, compareTokens: false);
        }

        [WorkItem(994456)]
        [WorkItem(958699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableAbstractly_NoDisposePattern()
        {
            Test(
@"using System;
abstract class C : [|IDisposable|]",
@"using System;
abstract class C : IDisposable
{
    public abstract void Dispose();
}
", index: 2, compareTokens: false);
        }

        [WorkItem(994456)]
        [WorkItem(958699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableThroughMember_NoDisposePattern()
        {
            Test(
@"using System;
class C : [|IDisposable|]
{
    private IDisposable foo;
}",
@"using System;
class C : IDisposable
{
    private IDisposable foo;

    public void Dispose()
    {
        foo.Dispose();
    }
}", index: 2, compareTokens: false);
        }

        [WorkItem(941469)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableExplicitly_NoNamespaceImportForSystem()
        {
            Test(
@"class C : [|System.IDisposable|]",
$@"class C : System.IDisposable
{{
{DisposePattern("protected virtual ", "C", "void System.IDisposable.")}
}}
", index: 3, compareTokens: false);
        }

        [WorkItem(951968)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableViaBaseInterface_NoDisposePattern()
        {
            Test(
@"using System;
interface I : IDisposable
{
    void F();
}
class C : [|I|]
{
}",
@"using System;
interface I : IDisposable
{
    void F();
}
class C : I
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void F()
    {
        throw new NotImplementedException();
    }
}", index: 0, compareTokens: false);
        }

        [WorkItem(951968)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableViaBaseInterface()
        {
            Test(
@"using System;
interface I : IDisposable
{
    void F();
}
class C : [|I|]
{
}",
$@"using System;
interface I : IDisposable
{{
    void F();
}}
class C : I
{{
    public void F()
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "public void ")}
}}", index: 1, compareTokens: false);
        }

        [WorkItem(951968)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementIDisposableExplicitlyViaBaseInterface()
        {
            Test(
@"using System;
interface I : IDisposable
{
    void F();
}
class C : [|I|]
{
}",
$@"using System;
interface I : IDisposable
{{
    void F();
}}
class C : I
{{
    void I.F()
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "void IDisposable.")}
}}", index: 3, compareTokens: false);
        }

        [WorkItem(941469)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDontImplementDisposePatternForLocallyDefinedIDisposable()
        {
            Test(
@"namespace System
{
    interface IDisposable
    {
        void Dispose();
    }

    class C : [|IDisposable|]
}",
@"namespace System
{
    interface IDisposable
    {
        void Dispose();
    }

    class C : IDisposable
    {
        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}", index: 1, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDontImplementDisposePatternForStructures1()
        {
            Test(
@"using System;
struct S : [|IDisposable|]",
@"using System;
struct S : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
", compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDontImplementDisposePatternForStructures2()
        {
            Test(
@"using System;
struct S : [|IDisposable|]",
@"using System;
struct S : IDisposable
{
    void IDisposable.Dispose()
    {
        throw new NotImplementedException();
    }
}
", index: 1, compareTokens: false);
        }

        [WorkItem(545924)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestEnumNestedInGeneric()
        {
            Test(
@"class C < T > { public enum E { X } } interface I { void Foo < T > ( C < T > . E x = C < T > . E . X ) ; } class D : [|I|] { } ",
@"using System ; class C < T > { public enum E { X } } interface I { void Foo < T > ( C < T > . E x = C < T > . E . X ) ; } class D : I { public void Foo < T > ( C < T > . E x = C < T > . E . X ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545939)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnterminatedString1()
        {
            Test(
@"using System ; class C : [|IServiceProvider|] @"" ",
@"using System ; class C : IServiceProvider @"" "" { public object GetService (Type serviceType) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545939)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnterminatedString2()
        {
            Test(
@"using System ; class C : [|IServiceProvider|] "" ",
@"using System ; class C : IServiceProvider "" "" { public object GetService (Type serviceType) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545939)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnterminatedString3()
        {
            Test(
@"using System ; class C : [|IServiceProvider|] @""",
@"using System ; class C : IServiceProvider @"""" { public object GetService (Type serviceType) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545939)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnterminatedString4()
        {
            Test(
@"using System ; class C : [|IServiceProvider|] """,
@"using System ; class C : IServiceProvider """" { public object GetService (Type serviceType) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545940)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDecimalENotation()
        {
            Test(
@"interface I
{
    void Foo1(decimal x = 1E-25M);
    void Foo2(decimal x = -1E-25M);
    void Foo3(decimal x = 1E-24M);
    void Foo4(decimal x = -1E-24M);
}
 
class C : [|I|]
{
}",
@"using System;

interface I
{
    void Foo1(decimal x = 1E-25M);
    void Foo2(decimal x = -1E-25M);
    void Foo3(decimal x = 1E-24M);
    void Foo4(decimal x = -1E-24M);
}

class C : I
{
    public void Foo1(decimal x = 0.0000000000000000000000001M)
    {
        throw new NotImplementedException();
    }

    public void Foo2(decimal x = -0.0000000000000000000000001M)
    {
        throw new NotImplementedException();
    }

    public void Foo3(decimal x = 0.000000000000000000000001M)
    {
        throw new NotImplementedException();
    }

    public void Foo4(decimal x = -0.000000000000000000000001M)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545938)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestGenericEnumWithRenamedTypeParameters()
        {
            Test(
@"class C < T > { public enum E { X } } interface I < S > { void Foo < T > ( S y , C < T > . E x = C < T > . E . X ) ; } class D < T > : [|I < T >|] { } ",
@"using System ; class C < T > { public enum E { X } } interface I < S > { void Foo < T > ( S y , C < T > . E x = C < T > . E . X ) ; } class D < T > : I < T > { public void Foo < T1 > ( T y , C < T1 > . E x = C < T1 > . E . X ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545919)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDoNotRenameTypeParameterToParameterName()
        {
            Test(
@"interface I < S > { void Foo < T > ( S T1 ) ; } class C < T > : [|I < T >|] { } ",
@"using System ; interface I < S > { void Foo < T > ( S T1 ) ; } class C < T > : I < T > { public void Foo < T2 > ( T T1 ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(530265)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestAttributes()
        {
            Test(
@"using System . Runtime . InteropServices ; interface I { [ return : MarshalAs ( UnmanagedType . U1 ) ] bool Foo ( [ MarshalAs ( UnmanagedType . U1 ) ] bool x ) ; } class C : [|I|] { } ",
@"using System ; using System . Runtime . InteropServices ; interface I { [ return : MarshalAs ( UnmanagedType . U1 ) ] bool Foo ( [ MarshalAs ( UnmanagedType . U1 ) ] bool x ) ; } class C : I { [ return : MarshalAs ( UnmanagedType . U1 ) ] public bool Foo ( [ MarshalAs ( UnmanagedType . U1 ) ] bool x ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(530265)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestAttributesExplicit()
        {
            Test(
@"using System . Runtime . InteropServices ; interface I { [ return : MarshalAs ( UnmanagedType . U1 ) ] bool Foo ( [ MarshalAs ( UnmanagedType . U1 ) ] bool x ) ; } class C : [|I|] { } ",
@"using System ; using System . Runtime . InteropServices ; interface I { [ return : MarshalAs ( UnmanagedType . U1 ) ] bool Foo ( [ MarshalAs ( UnmanagedType . U1 ) ] bool x ) ; } class C : I { bool I . Foo ( bool x ) { throw new NotImplementedException ( ) ; } } ",
index: 1);
        }

        [WorkItem(546443)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestParameterNameWithTypeName()
        {
            Test(
@"using System ; interface IFoo { void Bar ( DateTime DateTime ) ; } class C : [|IFoo|] { } ",
@"using System ; interface IFoo { void Bar ( DateTime DateTime ) ; } class C : IFoo { public void Bar ( DateTime DateTime ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(530521)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestUnboundGeneric()
        {
            Test(
@"using System . Collections . Generic ; using System . Runtime . InteropServices ; interface I { [ return : MarshalAs ( UnmanagedType . CustomMarshaler , MarshalTypeRef = typeof ( List < > ) ) ] void Foo ( ) ; } class C : [|I|] { } ",
@"using System ; using System . Collections . Generic ; using System . Runtime . InteropServices ; interface I { [ return : MarshalAs ( UnmanagedType . CustomMarshaler , MarshalTypeRef = typeof ( List < > ) ) ] void Foo ( ) ; } class C : I { [ return : MarshalAs ( UnmanagedType . CustomMarshaler , MarshalTypeRef = typeof ( List < > ) ) ] public void Foo ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(752436)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestQualifiedNameImplicitInterface()
        {
            Test(
@"namespace N { public interface I { void M ( ) ; } } class C : [|N . I|] { } ",
@"using System ; namespace N { public interface I { void M ( ) ; } } class C : N . I { public void M ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(752436)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestQualifiedNameExplicitInterface()
        {
            Test(
@"namespace N { public interface I { void M ( ) ; } } class C : [|N . I|] { } ",
@"using System ; using N ; namespace N { public interface I { void M ( ) ; } } class C : N . I { void I . M ( ) { throw new NotImplementedException ( ) ; } } ", index: 1);
        }

        [WorkItem(847464)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForPartialType()
        {
            Test(
@"
public interface I { void Foo(); }
partial class C { }
partial class C : [|I|] { }
",
@"using System;

public interface I { void Foo(); }
partial class C { }
partial class C : I
{
    void I.Foo()
    {
        throw new NotImplementedException();
    }
}
", index: 1);
        }

        [WorkItem(847464)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForPartialType2()
        {
            Test(
@"
public interface I { void Foo(); }
partial class C : [|I|] { }
partial class C { }
",
@"using System;

public interface I { void Foo(); }
partial class C : I
{
    void I.Foo()
    {
        throw new NotImplementedException();
    }
}
partial class C { }
", index: 1);
        }

        [WorkItem(847464)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForPartialType3()
        {
            Test(
@"
public interface I { void Foo(); }
public interface I2 { void Foo2(); }
partial class C : [|I|] { }
partial class C : I2 { }
",
@"using System;

public interface I { void Foo(); }
public interface I2 { void Foo2(); }
partial class C : I
{
    void I.Foo()
    {
        throw new NotImplementedException();
    }
}
partial class C : I2 { }
", index: 1);
        }

        [WorkItem(752447)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestExplicitImplOfIndexedProperty()
        {
            var initial = @"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
Public Interface IFoo
    Property IndexProp(ByVal p1 As Integer) As String
End Interface
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
public class Test : [|IFoo|]
{
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
using System;

public class Test : IFoo
{
    string IFoo.get_IndexProp(int p1)
    {
        throw new NotImplementedException();
    }

    void IFoo.set_IndexProp(int p1, string Value)
    {
        throw new NotImplementedException();
    }
}
";

            Test(initial, expected, index: 1);
        }

        [WorkItem(602475)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplicitImplOfIndexedProperty()
        {
            var initial = @"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
Public Interface I
    Property P(x As Integer)
End Interface
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
using System;

class C : [|I|]
{
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
using System;

class C : I
{
    public object get_P(int x)
    {
        throw new NotImplementedException();
    }
 
    public void set_P(int x, object Value)
    {
        throw new NotImplementedException();
    }
}
";

            Test(initial, expected, index: 0);
        }

#if false
        [WorkItem(13677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoGenerateInVenusCase2()
        {
            TestMissing(
@"using System;
#line 1 ""Bar""
class Foo : [|IComparable|]
#line default
#line hidden");
        }
#endif

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForImplicitIDisposable()
        {
            Test(
@"
using System;

class Program : [|IDisposable|]
{
}
",
$@"
using System;

class Program : IDisposable
{{
{DisposePattern("protected virtual ", "C", "public void ")}
}}
", index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForExplicitIDisposable()
        {
            Test(
@"
using System;

class Program : [|IDisposable|]
{
    private bool DisposedValue;
}
",
$@"
using System;

class Program : IDisposable
{{
    private bool DisposedValue;
{DisposePattern("protected virtual ", "Program", "void IDisposable.")}
}}
", index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForIDisposableNonApplicable1()
        {
            Test(
@"
using System;

class Program : [|IDisposable|]
{
    private bool disposedValue;
}
",
@"
using System;

class Program : IDisposable
{
    private bool disposedValue;

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
", index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForIDisposableNonApplicable2()
        {
            Test(
@"
using System;

class Program : [|IDisposable|]
{
    public void Dispose(bool flag)
    {
    }
}
",
@"
using System;

class Program : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Dispose(bool flag)
    {
    }
}
", index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceForExplicitIDisposableWithSealedClass()
        {
            Test(
@"
using System;

sealed class Program : [|IDisposable|]
{
}
",
$@"
using System;

sealed class Program : IDisposable
{{
{DisposePattern("", "Program", "void IDisposable.")}
}}
", index: 3);
        }

        [WorkItem(939123)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoComAliasNameAttributeOnMethodParameters()
        {
            Test(
@"interface I { void M([System.Runtime.InteropServices.ComAliasName(""pAlias"")] int p); }
class C : [|I|] { }",
@"using System;
interface I { void M([System.Runtime.InteropServices.ComAliasName(""pAlias"")]int p); }
class C : I { public void M(int p) { throw new NotImplementedException(); } }");
        }

        [WorkItem(939123)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoComAliasNameAttributeOnMethodReturnType()
        {
            Test(
@"using System.Runtime.InteropServices;
interface I { [return:ComAliasName(""pAlias1"")] long M([ComAliasName(""pAlias2"")] int p); }
class C : [|I|] { }",
@"using System;
using System.Runtime.InteropServices;
interface I { [return:ComAliasName(""pAlias1"")] long M([ComAliasName(""pAlias2"")] int p); }
class C : I { public long M(int p) { throw new NotImplementedException(); } }");
        }

        [WorkItem(939123)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestNoComAliasNameAttributeOnIndexerParameters()
        {
            Test(
@"interface I { long this[[System.Runtime.InteropServices.ComAliasName(""pAlias"")] int p] { get; } }
class C : [|I|] { }",
@"using System;
interface I { long this[[System.Runtime.InteropServices.ComAliasName(""pAlias"")] int p] { get; } }
class C : I { public long this[int p] { get { throw new NotImplementedException(); } } }");
        }

        [WorkItem(947819)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestMissingOpenBrace()
        {
            Test(
@"namespace Scenarios
{
    public interface TestInterface
    {
        void M1();
    }
    struct TestStruct1 : [|TestInterface|]
 
    // Comment
}",
@"using System;

namespace Scenarios
{
    public interface TestInterface
    {
        void M1();
    }
    struct TestStruct1 : TestInterface
    {
        public void M1()
        {
            throw new NotImplementedException();
        }
    }
    // Comment
}");
        }

        [WorkItem(994328)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDisposePatternWhenAdditionalUsingsAreIntroduced1()
        {
            //CSharpFeaturesResources.DisposePattern
            Test(
@"interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}

partial class C
{
}

partial class C : [|I<System.Exception, System.AggregateException>|], System.IDisposable
{
}",
$@"using System;
using System.Collections.Generic;

interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}}

partial class C
{{
}}

partial class C : I<System.Exception, System.AggregateException>, System.IDisposable
{{
    public bool Equals(int other)
    {{
        throw new NotImplementedException();
    }}

    public List<AggregateException> M(Dictionary<Exception, List<AggregateException>> a, Exception b, AggregateException c)
    {{
        throw new NotImplementedException();
    }}

    public List<UU> M<TT, UU>(Dictionary<TT, List<UU>> a, TT b, UU c) where UU : TT
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "public void ")}
}}", index: 1, compareTokens: false);
        }

        [WorkItem(994328)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestDisposePatternWhenAdditionalUsingsAreIntroduced2()
        {
            Test(
@"interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}

partial class C : [|I<System.Exception, System.AggregateException>|], System.IDisposable
{
}

partial class C
{
}",
$@"using System;
using System.Collections.Generic;

interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}}

partial class C : I<System.Exception, System.AggregateException>, System.IDisposable
{{
    bool IEquatable<int>.Equals(int other)
    {{
        throw new NotImplementedException();
    }}

    List<AggregateException> I<Exception, AggregateException>.M(Dictionary<Exception, List<AggregateException>> a, Exception b, AggregateException c)
    {{
        throw new NotImplementedException();
    }}

    List<UU> I<Exception, AggregateException>.M<TT, UU>(Dictionary<TT, List<UU>> a, TT b, UU c)
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "void IDisposable.")}
}}

partial class C
{{
}}", index: 3, compareTokens: false);
        }

        private static string DisposePattern(string disposeVisibility, string className, string implementationVisibility)
        {
            return $@"    #region IDisposable Support
    private bool disposedValue = false; // {FeaturesResources.ToDetectRedundantCalls}

    {disposeVisibility}void Dispose(bool disposing)
    {{
        if (!disposedValue)
        {{
            if (disposing)
            {{
                // {FeaturesResources.DisposeManagedStateTodo}
            }}

            // {CSharpFeaturesResources.FreeUnmanagedResourcesTodo}
            // {FeaturesResources.SetLargeFieldsToNullTodo}

            disposedValue = true;
        }}
    }}

    // {CSharpFeaturesResources.OverrideAFinalizerTodo}
    // ~{className}() {{
    //   // {CSharpFeaturesResources.DoNotChangeThisCodeUseDispose}
    //   Dispose(false);
    // }}

    // {CSharpFeaturesResources.ThisCodeAddedToCorrectlyImplementDisposable}
    {implementationVisibility}Dispose()
    {{
        // {CSharpFeaturesResources.DoNotChangeThisCodeUseDispose}
        Dispose(true);
        // {CSharpFeaturesResources.UncommentTheFollowingIfFinalizerOverriddenTodo}
        // GC.SuppressFinalize(this);
    }}
    #endregion";
        }

        [WorkItem(1132014)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestInaccessibleAttributes()
        {
            Test(
@"using System;

public class Foo : [|Holder.SomeInterface|]
{
}

public class Holder
{
    public interface SomeInterface
    {
        void Something([SomeAttribute] string helloWorld);
    }

    private class SomeAttribute : Attribute
    {
    }
}",
@"using System;

public class Foo : Holder.SomeInterface
{
    public void Something(string helloWorld)
    {
        throw new NotImplementedException();
    }
}

public class Holder
{
    public interface SomeInterface
    {
        void Something([SomeAttribute] string helloWorld);
    }

    private class SomeAttribute : Attribute
    {
    }
}");
        }

        [WorkItem(2785, "https://github.com/dotnet/roslyn/issues/2785")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public void TestImplementInterfaceThroughStaticMemberInGenericClass()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Issue2785 < T > : [|IList < object >|] { private static List < object > innerList = new List < object > ( ) ; } ",
@"using System ; using System . Collections ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Issue2785 < T > : IList < object > { private static List < object > innerList = new List < object > ( ) ; public object this [ int index ] { get { return ( ( IList < object > ) innerList ) [ index ] ; } set { ( ( IList < object > ) innerList ) [ index ] = value ; } } public int Count { get { return ( ( IList < object > ) innerList ) . Count ; } } public bool IsReadOnly { get { return ( ( IList < object > ) innerList ) . IsReadOnly ; } } public void Add ( object item ) { ( ( IList < object > ) innerList ) . Add ( item ) ; } public void Clear ( ) { ( ( IList < object > ) innerList ) . Clear ( ) ; } public bool Contains ( object item ) { return ( ( IList < object > ) innerList ) . Contains ( item ) ; } public void CopyTo ( object [ ] array , int arrayIndex ) { ( ( IList < object > ) innerList ) . CopyTo ( array , arrayIndex ) ; } public IEnumerator < object > GetEnumerator ( ) { return ( ( IList < object > ) innerList ) . GetEnumerator ( ) ; } public int IndexOf ( object item ) { return ( ( IList < object > ) innerList ) . IndexOf ( item ) ; } public void Insert ( int index , object item ) { ( ( IList < object > ) innerList ) . Insert ( index , item ) ; } public bool Remove ( object item ) { return ( ( IList < object > ) innerList ) . Remove ( item ) ; } public void RemoveAt ( int index ) { ( ( IList < object > ) innerList ) . RemoveAt ( index ) ; } IEnumerator IEnumerable . GetEnumerator ( ) { return ( ( IList < object > ) innerList ) . GetEnumerator ( ) ; } } ",
index: 1);
        }
    }
}

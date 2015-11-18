// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateType;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateTypeTests
{
    public partial class GenerateTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                null, new GenerateTypeCodeFixProvider());
        }

        protected override IList<CodeAction> MassageActions(IList<CodeAction> codeActions)
        {
            return FlattenActions(codeActions);
        }

        #region Generate Class

        #region Generics

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeParameterFromArgumentInferT()
        {
            await TestAsync(
@"class Program { void Main ( ) { [|Foo < int >|] f ; } } ",
@"class Program { void Main ( ) { Foo < int > f ; } } internal class Foo < T > { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromTypeParameter()
        {
            await TestAsync(
@"class Class { System.Action<[|Employee|]> employees; }",
@"class Class { System.Action<Employee> employees; private class Employee { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromASingleConstraintClause()
        {
            await TestAsync(
@"class EmployeeList<T> where T : [|Employee|], new() { }",
@"class EmployeeList<T> where T : Employee, new() { } internal class Employee { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassFromConstructorConstraint()
        {
            await TestMissingAsync(
@"class EmployeeList<T> where T : Employee, [|new()|] { }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromMultipleTypeConstraintClauses()
        {
            await TestAsync(
@"class Derived<T, U> where U : struct where T : [|Base|], new() { }",
@"class Derived<T, U> where U : struct where T : Base, new() { } internal class Base { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassFromClassOrStructConstraint()
        {
            await TestMissingAsync(
@"class Derived<T, U> where U : [|struct|] where T : Base, new() { }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAbsenceOfGenerateIntoInvokingTypeForConstraintList()
        {
            await TestActionCountAsync(
@"class EmployeeList<T> where T : [|Employee|] { }",
count: 3,
parseOptions: Options.Regular);
        }

        #endregion

        #region Lambdas

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromParenthesizedLambdaExpressionsParameter()
        {
            await TestAsync(
@"class Class { Func<Employee, int, bool> l = ([|Employee|] e, int age) => e.Age > age; }",
@"class Class { Func<Employee, int, bool> l = (Employee e, int age) => e.Age > age; private class Employee { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromParenthesizedLambdaExpressionsBody()
        {
            await TestAsync(
@"class Class { System.Action<Class, int> l = (Class e, int age) => { [|Wage|] w; }; }",
@"class Class { System.Action<Class, int> l = (Class e, int age) => { Wage w; }; private class Wage { } }",
index: 2);
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoSameType()
        {
            await TestAsync(
@"class Class { [|Foo|] f; }",
@"class Class { Foo f; private class Foo { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoGlobalNamespace()
        {
            await TestAddDocument(
@"class Program { void Main ( ) { [|Foo|] f ; } } ",
@"internal class Foo { } ",
expectedContainers: Array.Empty<string>(),
expectedDocumentName: "Foo.cs").ConfigureAwait(true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoCustomNamespace()
        {
            await TestAddDocument(
@"class Class { [|TestNamespace|].Foo f; }",
@"namespace TestNamespace { internal class Foo { } }",
expectedContainers: new List<string> { "TestNamespace" },
expectedDocumentName: "Foo.cs").ConfigureAwait(true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoSameNamespace()
        {
            await TestAsync(
@"class Class { [|Foo|] f; }",
@"class Class { Foo f; } internal class Foo { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassWithCtorFromObjectCreation()
        {
            await TestAsync(
@"class Class { Foo f = new [|Foo|](); }",
@"class Class { Foo f = new Foo(); private class Foo { public Foo() { } } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromBaseList()
        {
            await TestAsync(
@"class Class : [|BaseClass|] { }",
@"class Class : BaseClass { } internal class BaseClass { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromMethodParameters()
        {
            await TestAsync(
@"class Class { void Method([|Foo|] f) { } }",
@"class Class { void Method(Foo f) { } private class Foo { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromMethodReturnType()
        {
            await TestAsync(
@"class Class { [|Foo|] Method() { } }",
@"class Class { Foo Method() { } private class Foo { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromAttribute()
        {
            await TestAsync(
@"class Class { [[|Obsolete|]] void Method() { } }",
@"using System; class Class { [Obsolete] void Method() { } private class ObsoleteAttribute : Attribute { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromExpandedAttribute()
        {
            await TestAsync(
@"class Class { [[|ObsoleteAttribute|]] void Method() { } }",
@"using System; class Class { [ObsoleteAttribute] void Method() { } private class ObsoleteAttribute : Attribute { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromCatchClause()
        {
            await TestAsync(
@"class Class { void Method() { try { } catch([|ExType|]) { } } }",
@"using System; using System.Runtime.Serialization; 
class Class { void Method() { try { } catch(ExType) { } } 
[Serializable] private class ExType : Exception 
{ 
public ExType() { } 
public ExType(string message) : base(message) { } 
public ExType(string message, Exception innerException) : base(message, innerException) { } 
protected ExType(SerializationInfo info, StreamingContext context) : base(info, context) { } 
} 
}",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromThrowStatement()
        {
            await TestAsync(
@"class Class { void Method() { throw new [|ExType|](); } }",
@"using System; using System.Runtime.Serialization; class Class { void Method() { throw new ExType(); }
[Serializable] private class ExType : Exception 
{ 
public ExType() { } 
public ExType(string message) : base(message) { } 
public ExType(string message, Exception innerException) : base(message, innerException) { } 
protected ExType(SerializationInfo info, StreamingContext context) : base(info, context) { } 
} 
}",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromThrowStatementWithDifferentArg()
        {
            await TestAsync(
@"class Class { void Method() { throw new [|ExType|](1); } }",
@"using System; using System.Runtime.Serialization; class Class { void Method() { throw new ExType(1); }
[Serializable] private class ExType : Exception 
{ 
private int v;
public ExType() { } 
public ExType(string message) : base(message) { } 
public ExType(int v) { this.v = v; }
public ExType(string message, Exception innerException) : base(message, innerException) { } 
protected ExType(SerializationInfo info, StreamingContext context) : base(info, context) { } 
} 
}",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromThrowStatementWithMatchingArg()
        {
            await TestAsync(
@"class Class { void Method() { throw new [|ExType|](""message""); } }",
@"using System; using System.Runtime.Serialization; class Class { void Method() { throw new ExType(""message""); }
[Serializable] private class ExType : Exception 
{ 
public ExType() { } 
public ExType(string message) : base(message) { } 
public ExType(string message, Exception innerException) : base(message, innerException) { } 
protected ExType(SerializationInfo info, StreamingContext context) : base(info, context) { } 
} 
}",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAbsenceOfGenerateIntoInvokingTypeForBaseList()
        {
            await TestActionCountAsync(
@"class Class : [|BaseClass|] { }",
count: 3,
parseOptions: Options.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromUsingStatement()
        {
            await TestAsync(
@"class Class { void Method() { using([|Foo|] f = new Foo()) { } } }",
@"class Class { void Method() { using(Foo f = new Foo()) { } } private class Foo { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromForeachStatement()
        {
            await TestAsync(
@"class Class { void Method() { foreach([|Employee|] e in empList) { } } }",
@"class Class { void Method() { foreach(Employee e in empList) { } } private class Employee { } }",
index: 2);
        }

        [WorkItem(538346)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassWhereKeywordBecomesTypeName()
        {
            await TestAsync(
@"class Class { [|@class|] c; }",
@"class Class { @class c; private class @class { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassOnContextualKeyword()
        {
            await TestAsync(
@"class Class { [|@Foo|] c; }",
@"class Class { @Foo c; private class Foo { } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassOnFrameworkTypes()
        {
            await TestMissingAsync(
@"class Class { void Method() { [|System|].Console.Write(5); } }");

            await TestMissingAsync(
@"class Class { void Method() { System.[|Console|].Write(5); } }");

            await TestMissingAsync(
@"class Class { void Method() { System.Console.[|Write|](5); } }");
        }

        [WorkItem(538409)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateIntoRightPart()
        {
            await TestAsync(
@"partial class Class { } partial class Class { [|C|] c; }",
@"partial class Class { } partial class Class { C c; private class C { } }",
index: 2);
        }

        [WorkItem(538408)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoCompilationUnit()
        {
            await TestAsync(
@"class Class { [|C|] c; void Main() { } }",
@"class Class { C c; void Main() { } } internal class C { }",
index: 1);
        }

        [WorkItem(538408)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoNamespace()
        {
            await TestAsync(
@"namespace N { class Class { [|C|] c; void Main() { } } }",
@"namespace N { class Class { C c; void Main() { } } internal class C { } }",
index: 1);
        }

        [WorkItem(538115)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithPreprocessor()
        {
            await TestAsync(
@"class C
{
#if true 
    void Foo([|A|] x) { }
#else
#endif
}",
@"class C
{
#if true 
    void Foo(A x) { }

    private class A
    {
    }
#else
#endif
}",
index: 2,
compareTokens: false);
        }

        [WorkItem(538495)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoContainingNamespace()
        {
            await TestAsync(
@"namespace N { class Class { N.[|C|] c; } }",
@"namespace N { class Class { N.C c; } internal class C { } }",
index: 1);
        }

        [WorkItem(538516)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromIntoNewNamespace()
        {
            await TestAddDocument(
@"class Class { static void Main(string[] args) { [|N|].C c; } }",
@"namespace N { internal class C { } }",
expectedContainers: new List<string> { "N" },
expectedDocumentName: "C.cs").ConfigureAwait(true);
        }

        [WorkItem(538558)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGlobalAlias()
        {
            await TestMissingAsync(
@"class Class { void Method() { [|global|]::System.String s; } }");

            await TestMissingAsync(
@"class Class { void Method() { global::[|System|].String s; } }");
        }

        [WorkItem(538069)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeFromArrayCreation1()
        {
            await TestAsync(
@"class A { void Foo() { A[] x = new [|C|][] { }; } } ",
@"class A { void Foo() { A[] x = new C[] { }; } } internal class C : A { }",
index: 1,
parseOptions: null);
        }

        [WorkItem(538069)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeFromArrayCreation2()
        {
            await TestAsync(
@"class A { void Foo() { A[][] x = new [|C|][][] { }; } } ",
@"class A { void Foo() { A[][] x = new C[][] { }; } } internal class C : A { }",
index: 1,
parseOptions: null);
        }

        [WorkItem(538069)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeFromArrayCreation3()
        {
            await TestAsync(
@"class A { void Foo() { A[] x = new [|C|][][] { }; } } ",
@"class A { void Foo() { A[] x = new C[][] { }; } } internal class C { }",
index: 1,
parseOptions: null);
        }

        [WorkItem(539329)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestNotInUsingDirective()
        {
            await TestMissingAsync(
@"using [|A|];");

            await TestMissingAsync(
@"using [|A.B|];");

            await TestMissingAsync(
@"using [|A|].B;");

            await TestMissingAsync(
@"using A.[|B|];");

            await TestMissingAsync(
@"using X = [|A|];");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateSimpleConstructor()
        {
            await TestAsync(
@"class Class { void M() { new [|T|](); } }",
@"class Class { void M() { new T(); } } internal class T { public T() { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithValueParameter()
        {
            await TestAsync(
@"class Class { void M() { new [|T|](1); } }",
@"class Class { void M() { new T(1); } } internal class T { private int v; public T(int v) { this.v = v; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithTwoValueParameters()
        {
            await TestAsync(
@"class Class { void M() { new [|T|](1, """"); } }",
@"class Class { void M() { new T(1, """"); } } internal class T { private int v1; private string v2; public T(int v1, string v2) { this.v1 = v1; this.v2 = v2; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNamedParameter()
        {
            await TestAsync(
@"class Class { void M() { new [|T|](arg: 1); } }",
@"class Class { void M() { new T(arg: 1); } } internal class T { private int arg; public T(int arg) { this.arg = arg; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithRefParameter()
        {
            await TestAsync(
@"class Class { void M(int i) { new [|T|](ref i); } }",
@"class Class { void M(int i) { new T(ref i); } } internal class T { private int i; public T(ref int i) { this.i = i; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameter()
        {
            await TestAsync(
@"class Class { void M(int i, bool b) { new [|T|](out i, ref b, null); } }",
@"class Class { void M(int i, bool b) { new T(out i, ref b, null); } } internal class T { private bool b; private object p; public T(out int i, ref bool b, object p) { i = 0; this.b = b; this.p = p; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters1()
        {
            await TestAsync(
@"class Class { void M(string s) { new [|T|](out s); } }",
@"class Class { void M(string s) { new T(out s); } } internal class T { public T(out string s) { s = null; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters2()
        {
            await TestAsync(
@"using System; class Class { void M(DateTime d) { new [|T|](out d); } }",
@"using System; class Class { void M(DateTime d) { new T(out d); } } internal class T { public T(out DateTime d) { d = default(DateTime); } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters3()
        {
            await TestAsync(
@"using System.Collections.Generic; class Class { void M(IList<int> d) { new [|T|](out d); } }",
@"using System.Collections.Generic; class Class { void M(IList<int> d) { new T(out d); } } internal class T { public T(out IList<int> d) { d = null; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters4()
        {
            await TestAsync(
@"class Class { void M(int? d) { new [|T|](out d); } }",
@"class Class { void M(int? d) { new T(out d); } } internal class T { public T(out int? d) { d = null; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters5()
        {
            await TestAsync(
@"class Class<X> { void M(X d) { new [|T|](out d); } }",
@"class Class<X> { void M(X d) { new T(out d); } } internal class T { public T(out object d) { d = null; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters6()
        {
            await TestAsync(
@"class Class < X > { void M ( X d ) { new [|T|] ( out d ) ; } } ",
@"class Class < X > { void M ( X d ) { new T ( out d ) ; } private class T { public T ( out X d ) { d = default ( X ) ; } } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters7()
        {
            await TestAsync(
@"class Class<X> where X : class { void M(X d) { new [|T|](out d); } }",
@"class Class<X> where X : class { void M(X d) { new T(out d); } } internal class T { public T(out object d) { d = null; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters8()
        {
            await TestAsync(
@"class Class<X> where X : class { void M(X d) { new [|T|](out d); } }",
@"class Class<X> where X : class { void M(X d) { new T(out d); } private class T { public T(out X d) { d = null; } } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithMethod()
        {
            await TestAsync(
@"class Class { string M(int i) { new [|T|](M); } }",
@"using System; class Class { string M(int i) { new T(M); } } internal class T { private Func<int,string> m; public T(Func<int,string> m) { this.m = m; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithLambda()
        {
            await TestAsync(
@"class Class { string M(int i) { new [|T|](a => a.ToString()); } }",
@"using System; class Class { string M(int i) { new T(a => a.ToString()); } } internal class T { private Func<object,object> p; public T(Func<object,object> p) { this.p = p; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor1()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](1); } } class Base { protected Base(int i) { } } ",
@"class Class { void M(int i) { Base b = new T(1); } } internal class T : Base { public T(int i) : base(i) { } } class Base { protected Base(int i) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor2()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](1); } } class Base { protected Base(object i) { } } ",
@"class Class { void M(int i) { Base b = new T(1); } } internal class T : Base { public T(object i) : base(i) { } } class Base { protected Base(object i) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor3()
        {
            await TestAsync(
@"using System.Collections.Generic; class Class { void M() { Base b = new [|T|](new List<int>()); } } class Base { protected Base(IEnumerable<int> values) { } } ",
@"using System.Collections.Generic; class Class { void M() { Base b = new T(new List<int>()); } } internal class T : Base { public T(IEnumerable<int> values) : base(values) { } } class Base { protected Base(IEnumerable<int> values) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor4()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](ref i); } } class Base { protected Base(ref int o) { } } ",
@"class Class { void M(int i) { Base b = new T(ref i); } } internal class T : Base { public T(ref int o) : base(ref o) { } } class Base { protected Base(ref int o) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor5()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](a => a.ToString()); } } class Base { protected Base(System.Func<int,string> f) { } } ",
@"using System; class Class { void M(int i) { Base b = new T(a => a.ToString()); } } internal class T : Base { public T(Func<int,string> f) : base(f) { } } class Base { protected Base(System.Func<int,string> f) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor6()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](out i); } } class Base { protected Base(out int o) { } } ",
@"class Class { void M(int i) { Base b = new T(out i); } } internal class T : Base { public T(out int o) : base(out o) { } } class Base { protected Base(out int o) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor1()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](1); } } class Base { protected Base(string i) { } } ",
@"class Class { void M(int i) { Base b = new T(1); } } internal class T : Base { private int v; public T(int v) { this.v = v; } } class Base { protected Base(string i) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor2()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](ref i); } } class Base { protected Base(out int o) { } } ",
@"class Class { void M(int i) { Base b = new T(ref i); } } internal class T : Base { private int i; public T(ref int i) { this.i = i; } } class Base { protected Base(out int o) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor3()
        {
            await TestAsync(
@"class Class { void M(int i, bool f) { Base b = new [|T|](out i, out f); } } class Base { protected Base(ref int o, out bool b) { } } ",
@"class Class { void M(int i, bool f) { Base b = new T(out i, out f); } } internal class T : Base { public T(out int i, out bool f) { i = 0; f = false; } } class Base { protected Base(ref int o, out bool b) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor4()
        {
            await TestAsync(
@"class Class { void M() { Base b = new [|T|](1); } } class Base { private Base(int i) { } } ",
@"class Class { void M() { Base b = new T(1); } } internal class T : Base { private int v; public T(int v) { this.v = v; } } class Base { private Base(int i) { } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField1()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { protected int i; } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { public T(int i) { this.i = i; } } class Base { protected int i; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField2()
        {
            await TestAsync(
@"class Class { void M(string i) { Base b = new [|T|](i); } } class Base { protected object i; } ",
@"class Class { void M(string i) { Base b = new T(i); } } internal class T : Base { public T(string i) { this.i = i; } } class Base { protected object i; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField3()
        {
            await TestAsync(
@"class Class { void M(string i) { Base b = new [|T|](i); } } class Base { protected bool i; } ",
@"class Class { void M(string i) { Base b = new T(i); } } internal class T : Base { private string i; public T(string i) { this.i = i; } } class Base { protected bool i; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField4()
        {
            await TestAsync(
@"class Class { void M(bool i) { Base b = new [|T|](i); } } class Base { protected bool ii; } ",
@"class Class { void M(bool i) { Base b = new T(i); } } internal class T : Base { private bool i; public T(bool i) { this.i = i; } } class Base { protected bool ii; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField5()
        {
            await TestAsync(
@"class Class { void M(bool i) { Base b = new [|T|](i); } } class Base { private bool i; } ",
@"class Class { void M(bool i) { Base b = new T(i); } } internal class T : Base { private bool i; public T(bool i) { this.i = i; } } class Base { private bool i; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField6()
        {
            await TestAsync(
@"class Class { void M(bool i) { Base b = new [|T|](i); } } class Base { protected readonly bool i; } ",
@"class Class { void M(bool i) { Base b = new T(i); } } internal class T : Base { private bool i; public T(bool i) { this.i = i; } } class Base { protected readonly bool i; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField7()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { protected int I; } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { public T(int i) { I = i; } } class Base { protected int I; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField7WithQualification()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { protected int I; } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { public T(int i) { this.I = i; } } class Base { protected int I; }",
index: 1,
options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField8()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { private int I; } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { private int i; public T(int i) { this.i = i; } } class Base { private int I; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField9()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { public static int i; } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { private int i; public T(int i) { this.i = i; } } class Base { public static int i; }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField10()
        {
            await TestAsync(
@"class Class { void M(int i) { D d = new [|T|](i); } } class D : B { protected int I; } class B { protected int i }",
@"class Class { void M(int i) { D d = new T(i); } } internal class T : D { public T(int i) { this.i = i; } } class D : B { protected int I; } class B { protected int i }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty1()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { public int I { get; private set; } } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { private int i; public T(int i) { this.i = i; } } class Base { public int I { get; private set; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty2()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { public int I { get; protected set; } } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { public T(int i) { I = i; } } class Base { public int I { get; protected set; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty2WithQualification()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { public int I { get; protected set; } } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { public T(int i) { this.I = i; } } class Base { public int I { get; protected set; } }",
index: 1,
options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty3()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { protected int I { get; set; } } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { public T(int i) { I = i; } } class Base { protected int I { get; set; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty3WithQualification()
        {
            await TestAsync(
@"class Class { void M(int i) { Base b = new [|T|](i); } } class Base { protected int I { get; set; } } ",
@"class Class { void M(int i) { Base b = new T(i); } } internal class T : Base { public T(int i) { this.I = i; } } class Base { protected int I { get; set; } }",
index: 1,
options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WorkItem(942568)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithPreferIntrinsicPredefinedKeywordFalse()
        {
            await TestAsync(
@"class Class {
    void M(int i) 
    {
        var b = new [|T|](i);
    }
}",
@"class Class {
    void M(int i) 
    {
        var b = new T(i);
    }
}

internal class T
{
    private System.Int32 i;

    public T(System.Int32 i)
    {
        this.i = i;
    }
}",
index: 1,
compareTokens: false,
options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, "C#"), false } });
        }

        #endregion

        #region Generate Interface

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromTypeConstraint()
        {
            await TestAsync(
@"class EmployeeList<T> where T : Employee, [|IEmployee|], new() { }",
@"class EmployeeList<T> where T : Employee, IEmployee, new() { } internal interface IEmployee { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromTypeConstraints()
        {
            await TestAsync(
@"class EmployeeList<T> where T : Employee, IEmployee, [|IComparable<T>|], new() { }",
@"class EmployeeList<T> where T : Employee, IEmployee, IComparable<T>, new() { } internal interface IComparable<T> where T : Employee, IEmployee, IComparable<T>, new() { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateInterfaceFromTypeConstraint()
        {
            await TestMissingAsync(
@"using System; class EmployeeList<T> where T : Employee, IEmployee, [|IComparable<T>|], new() { }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromBaseList1()
        {
            await TestAsync(
@"interface A : [|B|] { }",
@"interface A : B { } internal interface B { }",
index: 1);
        }

        [WorkItem(538519)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromBaseList2()
        {
            await TestAsync(
@"class Test : [|ITest|] { }",
@"class Test : ITest { } internal interface ITest { }",
index: 1);
        }

        [WorkItem(538519)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromTypeConstraints2()
        {
            await TestAsync(
@"class Test<T> where T : [|ITest|] { }",
@"class Test<T> where T : ITest { } internal interface ITest { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromBaseList3()
        {
            await TestAsync(
@"class A : object, [|B|] { }",
@"class A : object, B { } internal interface B { }",
index: 1);
        }

        #endregion

        [WorkItem(539339)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NotInLeftSideOfAssignment()
        {
            await TestMissingAsync(
@"class Class { void M(int i) { [|Foo|] = 2; } }");
        }

        [WorkItem(539339)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task InLeftSideOfAssignment()
        {
            await TestAsync(
@"class Class { void M(int i) { [|Foo|].Bar = 2; } }",
@"class Class { void M(int i) { Foo.Bar = 2; } } internal class Foo { }",
index: 1);
        }

        [WorkItem(539339)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NotInRightSideOfAssignment()
        {
            await TestMissingAsync(
@"class Class { void M(int i) { x = [|Foo|]; } }");
        }

        [WorkItem(539339)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task InRightSideOfAssignment()
        {
            await TestAsync(
@"class Class { void M(int i) { x = [|Foo|].Bar; } }",
@"class Class { void M(int i) { x = Foo.Bar; } } internal class Foo { }",
index: 1);
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestEscapedName()
        {
            await TestAsync(
@"class Class { [|@Foo|] f; }",
@"class Class { @Foo f; } internal class Foo { }",
index: 1);
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestEscapedKeyword()
        {
            await TestAsync(
@"class Class { [|@int|] f; }",
@"class Class { @int f; } internal class @int { }",
index: 1);
        }

        [WorkItem(539535)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateIntoNewFile()
        {
            await TestAddDocument(
@"class Class { void F() { new [|Foo|].Bar(); } }",
@"namespace Foo { internal class Bar { public Bar() { } } }",
expectedContainers: new List<string> { "Foo" },
expectedDocumentName: "Bar.cs").ConfigureAwait(true);
        }

        [WorkItem(539620)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDeclarationSpan()
        {
            await TestSpansAsync(
@"class Class { void Foo() { [|Bar|] b; } }",
@"class Class { void Foo() { [|Bar|] b; } }",
index: 1);
        }

        [WorkItem(539674)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInEnumBaseList()
        {
            await TestMissingAsync(
@"enum E : [|A|] { }");
        }

        [WorkItem(539681)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInConditional()
        {
            await TestMissingAsync(
@"class Program { static void Main ( string [ ] args ) { if ( [|IsTrue|] ) { } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestInUsing()
        {
            await TestAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { using ( [|Foo|] f = bar ( ) ) { } } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { using ( Foo f = bar ( ) ) { } } } internal class Foo { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInDelegateConstructor()
        {
            await TestMissingAsync(
@"delegate void D ( int x ) ; class C { void M ( ) { D d = new D ( [|Test|] ) ; } } ");
        }

        [WorkItem(539754)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnVar()
        {
            await TestMissingAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { [|var|] x = new Program ( ) ; } } ");
        }

        [WorkItem(539765)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestElideDefaultConstructor()
        {
            await TestAsync(
@"class A { void M ( ) { C test = new [|B|] ( ) ; } } internal class C { } ",
@"class A { void M ( ) { C test = new B ( ) ; } } internal class B : C { } internal class C { } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        [WorkItem(539783)]
        public async Task RegressionFor5867ErrorToleranceTopLevel()
        {
            await TestMissingAsync(
@"[|this|] . f = f ; ",
GetScriptOptions());
        }

        [WorkItem(539799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestOnInaccessibleType()
        {
            await TestMissingAsync(
@"class C { private class D { } } class A { void M ( ) { C . [|D|] d = new C . D ( ) ; } } ");
        }

        [WorkItem(539794)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDefaultConstructorInTypeDerivingFromInterface()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { I obj = new [|A|] ( ) ; } } interface I { } ",
@"class Program { static void Main ( string [ ] args ) { I obj = new A ( ) ; } } internal class A : I { } interface I { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateWithThrow()
        {
            await TestAsync(
@"using System ; class C { void M ( ) { throw new [|NotFoundException|] ( ) ; } } ",
@"using System ; using System . Runtime . Serialization ; class C { void M ( ) { throw new NotFoundException ( ) ; } } [ Serializable ] internal class NotFoundException : Exception { public NotFoundException ( ) { } public NotFoundException ( string message ) : base ( message ) { } public NotFoundException ( string message , Exception innerException ) : base ( message , innerException ) { } protected NotFoundException ( SerializationInfo info , StreamingContext context ) : base ( info , context ) { } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInTryCatch()
        {
            await TestAsync(
@"using System ; class C { void M ( ) { try { } catch ( [|NotFoundException|] ex ) { } } } ",
@"using System ; using System . Runtime . Serialization ; class C { void M ( ) { try { } catch ( NotFoundException ex ) { } } } [ Serializable ] internal class NotFoundException : Exception { public NotFoundException ( ) { } public NotFoundException ( string message ) : base ( message ) { } public NotFoundException ( string message , Exception innerException ) : base ( message , innerException ) { } protected NotFoundException ( SerializationInfo info , StreamingContext context ) : base ( info , context ) { } } ",
index: 1);
        }

        [WorkItem(539739)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNotGenerateInDelegateConstructor()
        {
            await TestMissingAsync(
@"using System ; delegate void D ( int x ) ; class C { void M ( ) { D d = new D ( [|Test|] ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestInStructBaseList()
        {
            await TestAsync(
@"struct S : [|A|] { } ",
@"struct S : A { } internal interface A { } ",
index: 1);
        }

        [WorkItem(539870)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenericWhenNonGenericExists()
        {
            await TestAsync(
@"class C { void Foo() { [|A<T>|] a; } } class A { }  ",
@"class C { void Foo() { A<T> a; } } internal class A<T> { } class A { }  ",
index: 1);
        }

        [WorkItem(539930)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestInheritedTypeParameters()
        {
            await TestAsync(
@"class C < T , R > { void M ( ) { I < T , R > i = new [|D < T , R >|] ( ) ; } } interface I < T , R > { } ",
@"class C < T , R > { void M ( ) { I < T , R > i = new D < T , R > ( ) ; } } internal class D < T , R > : I < T , R > { } interface I < T , R > { } ",
index: 1);
        }

        [WorkItem(539971)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDoNotUseOuterTypeParameters()
        {
            await TestAsync(
@"class C < T1 , T2 > { public void Foo ( ) { [|D < int , string >|] d ; } } ",
@"class C < T1 , T2 > { public void Foo ( ) { D < int , string > d ; } private class D < T3 , T4 > { } } ",
index: 2);
        }

        [WorkItem(539970)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters1()
        {
            await TestAsync(
@"class M < T , R > { public void Foo ( ) { I < T , R > i = new [|C < int , string >|] ( ) ; } } interface I < T , R > { } ",
@"class M < T , R > { public void Foo ( ) { I < T , R > i = new C < int , string > ( ) ; } } internal class C < T1 , T2 > : I < object , object > { } interface I < T , R > { } ",
index: 1);
        }

        [WorkItem(539970)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters2()
        {
            await TestAsync(
@"class M < T , R > { public void Foo ( ) { I < T , R > i = new [|C < int , string >|] ( ) ; } } interface I < T , R > { } ",
@"class M < T , R > { public void Foo ( ) { I < T , R > i = new C < int , string > ( ) ; } private class C < T1 , T2 > : I < T , R > { } } interface I < T , R > { } ",
index: 2);
        }

        [WorkItem(539972)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters3()
        {
            await TestAsync(
@"class C < T1 , T2 > { public void Foo ( T1 t1 , T2 t2 ) { A a = new [|A|] ( t1 , t2 ) ; } } ",
@"class C < T1 , T2 > { public void Foo ( T1 t1 , T2 t2 ) { A a = new A ( t1 , t2 ) ; } } internal class A { private object t1 ; private object t2 ; public A ( object t1 , object t2 ) { this . t1 = t1 ; this . t2 = t2 ; } } ",
index: 1);
        }

        [WorkItem(539972)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters4()
        {
            await TestAsync(
@"class C < T1 , T2 > { public void Foo ( T1 t1 , T2 t2 ) { A a = new [|A|] ( t1 , t2 ) ; } } ",
@"class C < T1 , T2 > { public void Foo ( T1 t1 , T2 t2 ) { A a = new A ( t1 , t2 ) ; } private class A { private T1 t1 ; private T2 t2 ; public A ( T1 t1 , T2 t2 ) { this . t1 = t1 ; this . t2 = t2 ; } } } ",
index: 2);
        }

        [WorkItem(539992)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotPassingEmptyIssueListToCtor()
        {
            await TestMissingAsync(
@"using System . Linq ; class Program { void Main ( ) { Enumerable . [|T|] Enumerable . Select ( Enumerable . Range ( 0 , 9 ) , i => char . Parse ( i . ToString ( ) ) ) } } ");
        }

        [WorkItem(540644)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateWithVoidArg()
        {
            await TestAsync(
@"class Program { void M ( ) { C c = new [|C|] ( M ( ) ) ; } } ",
@"class Program { void M ( ) { C c = new C ( M ( ) ) ; } } internal class C { private object v ; public C ( object v ) { this . v = v ; } } ",
index: 1);
        }

        [WorkItem(540989)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnInaccessibleType()
        {
            await TestMissingAsync(
@"class Outer { class Inner { } } class A { Outer . [|Inner|] inner ; } ");
        }

        [WorkItem(540766)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnInvalidGlobalCode()
        {
            await TestMissingAsync(
@"[|a|] test ",
parseOptions: null);
        }

        [WorkItem(539985)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDoNotInferTypeWithWrongArity()
        {
            await TestAsync(
@"class C < T1 > { public void Test ( ) { C c = new [|C|] ( ) ; } } ",
@"class C < T1 > { public void Test ( ) { C c = new C ( ) ; } } internal class C { public C ( ) { } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnInvalidConstructorToExistingType()
        {
            await TestMissingAsync(
@"class Program { static void Main ( ) { new [|Program|] ( 1 ) ; } } ");
        }

        [WorkItem(541263)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityConstraint()
        {
            await TestAsync(
@"public static class MyExtension { public static int ExtensionMethod ( this String s , [|D|] d ) { return 10 ; } } ",
@"public static class MyExtension { public static int ExtensionMethod ( this String s , D d ) { return 10 ; } } public class D { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestBaseTypeAccessibilityConstraint()
        {
            await TestAsync(
@"public class C : [|D|] { } ",
@"public class C : D { } public class D { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestBaseInterfaceAccessibilityConstraint1()
        {
            await TestAsync(
@"public class C : X , [|IFoo|] { } ",
@"public class C : X , IFoo { } internal interface IFoo { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityConstraint2()
        {
            await TestAsync(
@"public interface C : [|IBar|] , IFoo { } ",
@"public interface C : IBar , IFoo { } public interface IBar { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityConstraint3()
        {
            await TestAsync(
@"public interface C : IBar , [|IFoo|] { } ",
@"public interface C : IBar , IFoo { } public interface IFoo { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDelegateReturnTypeAccessibilityConstraint()
        {
            await TestAsync(
@"public delegate [|D|] Foo ( ) ; ",
@"public delegate D Foo ( ) ; public class D { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDelegateParameterAccessibilityConstraint()
        {
            await TestAsync(
@"public delegate D Foo ( [|S|] d ) ; ",
@"public delegate D Foo ( S d ) ; public class S { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMethodParameterAccessibilityConstraint()
        {
            await TestAsync(
@"public class C { public void Foo ( [|F|] f ) ; } ",
@"public class C { public void Foo ( F f ) ; } public class F { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMethodReturnTypeAccessibilityConstraint()
        {
            await TestAsync(
@"public class C { public [|F|] Foo ( Bar f ) ; } ",
@"public class C { public F Foo ( Bar f ) ; public class F { } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestPropertyTypeAccessibilityConstraint()
        {
            await TestAsync(
@"public class C { public [|F|] Foo { get ; } } ",
@"public class C { public F Foo { get ; } public class F { } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestFieldEventTypeAccessibilityConstraint()
        {
            await TestAsync(
@"public class C { public event [|F|] E ; } ",
@"public class C { public event F E ; public class F { } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestEventTypeAccessibilityConstraint()
        {
            await TestAsync(
@"public class C { public event [|F|] E { add { } remove { } } } ",
@"public class C { public event F E { add { } remove { } } } public class F { } ",
index: 1);
        }

        [WorkItem(541654)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateVarType()
        {
            await TestAsync(
@"class C { public static void Main ( ) { [|@var|] v ; } } ",
@"class C { public static void Main ( ) { @var v ; } } internal class var { } ",
index: 1);
        }

        [WorkItem(541641)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestOnBadAttribute()
        {
            await TestAsync(
@"[ [|AttClass|] ( ) ] class C { } internal class AttClassAttribute { } ",
@"using System;

[ AttClass ( ) ] class C { }

internal class AttClassAttribute : Attribute
{
}

internal class AttClassAttribute { }",
index: 1);
        }

        [WorkItem(542528)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateStruct1()
        {
            await TestAsync(
@"using System ; class A < T > where T : struct { } class Program { static void Main ( ) { new A < [|S|] > ( ) ; } } ",
@"using System ; class A < T > where T : struct { } class Program { static void Main ( ) { new A < S > ( ) ; } } internal struct S { } ",
index: 1);
        }

        [WorkItem(542480)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestCopyConstraints1()
        {
            await TestAsync(
@"class A < T > where T : class { } class Program { static void Foo < T > ( ) where T : class { A < T > a = new [|B < T >|] ( ) ; } } ",
@"class A < T > where T : class { } class Program { static void Foo < T > ( ) where T : class { A < T > a = new B < T > ( ) ; } } internal class B < T > : A < T > where T : class { } ",
index: 1);
        }

        [WorkItem(542528)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateStruct2()
        {
            await TestAsync(
@"using System ; class A < T > where T : struct { } class Program { static void Main ( ) { new A < Program . [|S|] > ( ) ; } } ",
@"using System ; class A < T > where T : struct { } class Program { static void Main ( ) { new A < Program . S > ( ) ; } private struct S { } } ",
index: 0);
        }

        [WorkItem(542528)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateStruct3()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( ) { Foo < Program . [|S|] > ( ) ; } static void Foo < T > ( ) where T : struct { } } ",
@"using System ; class Program { static void Main ( ) { Foo < Program . S > ( ) ; } static void Foo < T > ( ) where T : struct { } private struct S { } } ",
index: 0);
        }

        [WorkItem(542761)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateOpenType1()
        {
            await TestAsync(
@"class Program { static void Main ( ) { var x = typeof ( [|C < , >|] ) ; } } ",
@"class Program { static void Main ( ) { var x = typeof ( C < , > ) ; } }  internal class C < T1 , T2 > { } ",
index: 1);
        }

        [WorkItem(542766)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateAttributeInGenericType()
        {
            await TestActionCountAsync(
@"using System;
 
class A<T>
{
    [[|C|]]
    void Foo() { }
}",
count: 3);
        }

        [WorkItem(543061)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNestedGenericAccessibility()
        {
            await TestAsync(
@"using System . Collections . Generic ; public class C { public void Foo ( List < [|NewClass|] > x ) { } } ",
@"using System . Collections . Generic ; public class C { public void Foo ( List < NewClass > x ) { } } public class NewClass { } ",
index: 1);
        }

        [WorkItem(543493)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task MissingIfNotInTypeStatementOrExpressionContext()
        {
            await TestMissingAsync(@"class C { void M ( ) { a [|b|] c d } } ");
            await TestMissingAsync(@"class C { void M ( ) { a b [|c|] d } } ");
            await TestMissingAsync(@"class C { void M ( ) { a b c [|d|] } } ");
        }

        [WorkItem(542641)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAttributeSuffixOnAttributeSubclasses()
        {
            await TestAsync(
@"using System . Runtime . CompilerServices ; class Program { static void Main ( string [ ] args ) { CustomConstantAttribute a = new [|FooAttribute|] ( ) ; } } ",
@"using System . Runtime . CompilerServices ; class Program { static void Main ( string [ ] args ) { CustomConstantAttribute a = new FooAttribute ( ) ; } } internal class FooAttribute : CustomConstantAttribute { } ",
index: 1);
        }

        [WorkItem(543853)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDisplayStringForGlobalNamespace()
        {
            await TestSmartTagTextAsync(
@"class C : [|Foo|]",
string.Format(FeaturesResources.Generate_0_1_in_new_file, "class", "Foo", FeaturesResources.GlobalNamespace));
        }

        [WorkItem(543853)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAddDocumentForGlobalNamespace()
        {
            await TestAddDocument(
@"class C : [|Foo|]",
"internal class Foo { }",
Array.Empty<string>(),
"Foo.cs").ConfigureAwait(true);
        }

        [WorkItem(543886)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestVerbatimAttribute()
        {
            await TestAsync(
@"[ [|@X|] ] class Class3 { } ",
@"using System; [ @X ] class Class3 { } internal class X : Attribute { } ",
index: 1);
        }

        [WorkItem(531220)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task CompareIncompleteMembersToEqual()
        {
            await TestAsync(
@"class C{X.X,X class X{X}
X void X<X void X
x,[|x|])",
@"class C{X.X,X class X{X}
X void X<X void X
x,x)private class x
    {
    }
}",
index: 2);
        }

        [WorkItem(544168)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotOnAbstractClassCreation()
        {
            await TestMissingAsync(
@"abstract class Foo { } class SomeClass { void foo ( ) { var q = new [|Foo|] ( ) ; } } ");
        }

        [WorkItem(545362)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInVenus1()
        {
            var code = @"
#line hidden
#line 1 ""Default.aspx""
class Program
{
    static void Main(string[] args)
    {
        [|Foo|] f;
#line hidden
#line 2 ""Default.aspx""
    }
}
";

            await TestExactActionSetOfferedAsync(code,
                new[]
                {
                    string.Format(FeaturesResources.Generate_0_1_in_new_file, "class", "Foo", FeaturesResources.GlobalNamespace),
                    string.Format(FeaturesResources.Generate_nested_0_1, "class", "Foo", "Program"),
                    FeaturesResources.GenerateNewType
                });

            await TestAsync(code,
@"
#line hidden
#line 1 ""Default.aspx""
class Program
{
    static void Main(string[] args)
    {
        [|Foo|] f;
#line hidden
#line 2 ""Default.aspx""
    }

    private class Foo
    {
    }
}
", index: 1, compareTokens: false);
        }

        [WorkItem(869506)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeOutsideCurrentProject()
        {
            var code = @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <ProjectReference>Assembly2</ProjectReference>
                        <Document FilePath=""Test1.cs"">
class Program
{
    static void Main(string[] args)
    {
        [|A.B.C$$|].D f;
    }
}

namespace A
{

}
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document FilePath=""Test2.cs"">
namespace A
{
    public class B
    {
    }
}
</Document>
                    </Project>
                </Workspace>";

            var expected = @"
namespace A
{
    public class B
    {
        public class C
        {
        }
    }
}
";

            await TestAsync(code, expected, compareTokens: false);
        }

        [WorkItem(932602)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeInFolderNotDefaultNamespace_0()
        {
            var code = @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DefaultNamespace = ""Namespace1.Namespace2"">
                        <Document FilePath=""Test1.cs"">
namespace Namespace1.Namespace2
{
    public class ClassA : [|$$ClassB|]
    {
    }
}
                        </Document>
                    </Project>
                </Workspace>";

            var expected = @"namespace Namespace1.Namespace2
{
    public class ClassB
    {
    }
}";

            await TestAddDocument(code,
                expected,
                expectedContainers: Array.Empty<string>(),
                expectedDocumentName: "ClassB.cs",
                compareTokens: false,
                isLine: false).ConfigureAwait(true);
        }

        [WorkItem(932602)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeInFolderNotDefaultNamespace_1()
        {
            var code = @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DefaultNamespace = ""Namespace1.Namespace2"" >
                        <Document FilePath=""Test1.cs"" Folders=""Namespace1\Namespace2"">
namespace Namespace1.Namespace2.Namespace3
{
    public class ClassA : [|$$ClassB|]
    {
    }
}
                        </Document>
                    </Project>
                </Workspace>";

            var expected = @"namespace Namespace1.Namespace2.Namespace3
{
    public class ClassB
    {
    }
}";

            await TestAddDocument(code,
                expected,
                expectedContainers: new List<string> { "Namespace1", "Namespace2" },
                expectedDocumentName: "ClassB.cs",
                compareTokens: false,
                isLine: false).ConfigureAwait(true);
        }

        [WorkItem(612700)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeWithNoBraces()
        {
            var code = @"class Test : [|Base|]";

            var expected = @"class Test : Base
internal class Base
{
}";

            await TestAsync(code, expected, index: 1);
        }

        [WorkItem(940003)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties1()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|](x: 1, y: ""Hello"") {Name = ""John"",�Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer(x: 1, y: ""Hello"") {Name = ""John"",�Age = DateTime.Today};
    }
}

internal class Customer
{
    private int x;
    private string y;

    public Customer(int x, string y)
    {
        this.x = x;
        this.y = y;
    }

    public DateTime Age { get; set; }
    public string Name { get; set; }
}";

            await TestAsync(code, expected, index: 1);
        }

        [WorkItem(940003)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties2()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|](x: 1, y: ""Hello"") {Name = null,�Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer(x: 1, y: ""Hello"") {Name = null,�Age = DateTime.Today};
    }
}

internal class Customer
{
    private int x;
    private string y;

    public Customer(int x, string y)
    {
        this.x = x;
        this.y = y;
    }

    public DateTime Age { get; set; }
    public object Name { get; set; }
}";

            await TestAsync(code, expected, index: 1);
        }

        [WorkItem(940003)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties3()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|](x: 1, y: ""Hello"") {Name = Foo,�Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer(x: 1, y: ""Hello"") {Name = Foo,�Age = DateTime.Today};
    }
}

internal class Customer
{
    private int x;
    private string y;

    public Customer(int x, string y)
    {
        this.x = x;
        this.y = y;
    }

    public DateTime Age { get; set; }
    public object Name { get; set; }
}";

            await TestAsync(code, expected, index: 1);
        }

        [WorkItem(1082031)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties4()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|] {Name = ""John"",�Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer {Name = ""John"",�Age = DateTime.Today};
    }
}

internal class Customer
{
    public DateTime Age { get; set; }
    public string Name { get; set; }
}";

            await TestAsync(code, expected, index: 1);
        }

        [WorkItem(1032176), WorkItem(1073099)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithNameOf()
        {
            var code = @"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}
";

            var expected = @"class C
{
    void M()
    {
        var x = nameof(Z);
    }
}

internal class Z
{
}";

            await TestAsync(code, expected, index: 1);
        }

        [WorkItem(1032176), WorkItem(1073099)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithNameOf2()
        {
            var code = @"class C
{
    void M()
    {
        var x = nameof([|C.Test|]);
    }
}";

            var expected = @"class C
{
    void M()
    {
        var x = nameof(C.Test);
    }

    private class Test
    {
    }
}";

            await TestAsync(code, expected, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithUsingStatic()
        {
            await TestAsync(
@"using static [|Sample|] ; ",
@"using static Sample ; internal class Sample { } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithUsingStatic2()
        {
            await TestMissingAsync(@"using [|Sample|] ; ");
        }

        [WorkItem(1107929)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields()
        {
            await TestAsync(
@"class A { public B b = new [|B|](); }",
@"public class B { public B() { } }",
index: 0);
        }

        [WorkItem(1107929)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields2()
        {
            await TestAsync(
@"class A { public B b = new [|B|](); }",
@"class A { public B b = new B(); } public class B { public B() {}}",
index: 1);
        }

        [WorkItem(1107929)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields3()
        {
            await TestAsync(
@"class A { public B b = new [|B|](); }",
@"class A { public B b = new B(); public class B { public B() {}}}",
index: 2);
        }

        [WorkItem(1107929)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields4()
        {
            await TestAsync(
@"class A { public B<int> b = new [|B|]<int>(); }",
@"public class B<T> { public B() {}}",
index: 0);
        }

        [WorkItem(1107929)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields5()
        {
            await TestAsync(
@"class A { public B<int> b = new [|B|]<int>(); }",
@"class A { public B<int> b = new B<int>(); } public class B<T>{ public B(){}}",
index: 1);
        }

        [WorkItem(1107929)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields6()
        {
            await TestAsync(
@"class A { public B<int> b = new [|B|]<int>(); }",
@"class A { public B<int> b = new B<int>(); public class B<T>{ public B(){}}}",
index: 2);
        }
    }
}

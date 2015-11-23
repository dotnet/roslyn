// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class SemanticClassifierTests : AbstractCSharpClassifierTests
    {
        internal override IEnumerable<ClassifiedSpan> GetClassificationSpans(string code, TextSpan textSpan, CSharpParseOptions options)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code, options))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

                var syntaxTree = document.GetSyntaxTreeAsync().Result;

                var service = document.GetLanguageService<IClassificationService>();
                var classifiers = service.GetDefaultSyntaxClassifiers();
                var extensionManager = workspace.Services.GetService<IExtensionManager>();

                var results = new List<ClassifiedSpan>();
                service.AddSemanticClassificationsAsync(document, textSpan,
                    extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes),
                    extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds),
                    results, CancellationToken.None).Wait();

                return results;
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void GenericClassDeclaration()
        {
            TestInMethod(
                className: "Class<T>",
                methodName: "M",
                code: @"new Class<int>();",
                expected: Class("Class"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void UsingAlias1()
        {
            Test(@"using M = System.Math;",
                Class("M"),
                Class("Math"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsTypeArgument()
        {
            TestInMethod(
                className: "Class<T>",
                methodName: "M",
                code: @"new Class<dynamic>();",
                expected: Classifications(Class("Class"), Keyword("dynamic")));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void UsingTypeAliases()
        {
            var code = @"using Alias = Test; 
class Test { void M() { Test a = new Test(); Alias b = new Alias(); } }";

            Test(code,
                code,
                Class("Alias"),
                Class("Test"),
                Class("Test"),
                Class("Test"),
                Class("Alias"),
                Class("Alias"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicTypeAlias()
        {
            Test(@"using dynamic = System.EventArgs; class C { dynamic d = new dynamic(); }",
                Class("dynamic"),
                Class("EventArgs"),
                Class("dynamic"),
                Class("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsDelegateName()
        {
            Test(@"delegate void dynamic(); class C { void M() { dynamic d; } }",
                Delegate("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsInterfaceName()
        {
            Test(@"interface dynamic { } class C { dynamic d; }",
                Interface("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsEnumName()
        {
            Test(@"enum dynamic { } class C { dynamic d; }",
                Enum("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsClassName()
        {
            Test(@"class dynamic { } class C { dynamic d; }",
                Class("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsClassNameAndLocalVariableName()
        {
            Test(@"class dynamic { dynamic() { dynamic dynamic; } }",
                Class("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsStructName()
        {
            Test(@"struct dynamic { } class C { dynamic d; }",
                Struct("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGenericClassName()
        {
            Test(@"class dynamic<T> { } class C { dynamic<int> d; }",
                Class("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGenericClassNameButOtherArity()
        {
            Test(@"class dynamic<T> { } class C { dynamic d; }",
                Keyword("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsUndefinedGenericType()
        {
            Test(@"class dynamic { } class C { dynamic<int> d; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsExternAlias()
        {
            Test(@"extern alias dynamic;
class C { dynamic::Foo a; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void GenericClassNameButOtherArity()
        {
            Test(@"class A<T> { } class C { A d; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void GenericTypeParameter()
        {
            Test(@"class C<T> { void M() { default(T) } }",
                TypeParameter("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void GenericMethodTypeParameter()
        {
            Test(@"class C { T M<T>(T t) { return default(T); } }",
                TypeParameter("T"),
                TypeParameter("T"),
                TypeParameter("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void GenericMethodTypeParameterInLocalVariableDeclaration()
        {
            Test(@"class C { void M<T>() { T t; } }",
                TypeParameter("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ParameterOfLambda1()
        {
            Test(@"class C { C() { Action a = (C p) => { }; } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ParameterOfAnonymousMethod()
        {
            Test(@"class C { C() { Action a = delegate (C p) { }; } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void GenericTypeParameterAfterWhere()
        {
            Test(@"class C<A, B> where A : B { }",
                TypeParameter("A"),
                TypeParameter("B"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void BaseClass()
        {
            Test(@"class C { } class C2 : C { }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void BaseInterfaceOnInterface()
        {
            Test(@"interface T { } interface T2 : T { }",
                Interface("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void BaseInterfaceOnClass()
        {
            Test(@"interface T { } class T2 : T { }",
                Interface("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void InterfaceColorColor()
        {
            Test(@"interface T { } class T2 : T { T T; }",
                Interface("T"),
                Interface("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DelegateColorColor()
        {
            Test(@"delegate void T(); class T2 { T T; }",
                Delegate("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DelegateReturnsItself()
        {
            Test(@"delegate T T(); class C { T T(T t); }",
                Delegate("T"),
                Delegate("T"),
                Delegate("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void StructColorColor()
        {
            Test(@"struct T { T T; }",
                Struct("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void EnumColorColor()
        {
            Test(@"enum T { T, T } class C { T T; }",
                Enum("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGenericTypeParameter()
        {
            Test(@"class C<dynamic> { dynamic d; }",
                TypeParameter("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGenericFieldName()
        {
            Test(@"class A<T> { T dynamic; }",
                TypeParameter("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PropertySameNameAsClass()
        {
            Test(@"class N { N N { get; set; } void M() { N n = N; N = n; N = N; } }",
                Class("N"),
                Class("N"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AttributeWithoutAttributeSuffix()
        {
            Test(@"using System; [Obsolete] class C { }",
                Class("Obsolete"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AttributeOnNonExistingMember()
        {
            Test(@"using System;
class A { [Obsolete] }",
                Class("Obsolete"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AttributeWithoutAttributeSuffixOnAssembly()
        {
            Test(@"using System;
[assembly: My]
class MyAttribute : Attribute { }",
                Class("My"),
                Class("Attribute"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AttributeViaNestedClassOrDerivedClass()
        {
            Test(@"using System;
[Base.My]
[Derived.My]
class Base
{
    public class MyAttribute : Attribute { }
}
class Derived : Base { }",
                Class("Base"),
                Class("My"),
                Class("Derived"),
                Class("My"),
                Class("Attribute"),
                Class("Base"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NamedAndOptional()
        {
            Test(@"class C { void B(C C = null) { } void M() { B(C: null); } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PartiallyWrittenGenericName1()
        {
            TestInMethod(
                className: "Class<T>",
                methodName: "M",
                code: @"Class<int",
                expected: Class("Class"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PartiallyWrittenGenericName2()
        {
            TestInMethod(
                className: "Class<T1, T2>",
                methodName: "M",
                code: @"Class<int, b",
                expected: Class("Class"));
        }

        // The "Color Color" problem is the C# IDE folklore for when
        // a property name is the same as a type name
        // and the resulting ambiguities that the spec
        // resolves in favor of properties
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor()
        {
            Test(@"class Color { Color Color; }",
                Class("Color"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor2()
        {
            Test(@"class T { T T = new T(); T() { this.T = new T(); } }",
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor3()
        {
            Test(@"class T { T T = new T(); void M(); T() { T.M(); } }",
                Class("T"),
                Class("T"));
        }

        /// <summary>
        /// Instance field should be preferred to type
        /// 7.5.4.1
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor4()
        {
            Test(@"class T { T T; void M() { T.T = null; } }",
                Class("T"));
        }

        /// <summary>
        /// Type should be preferred to a static field
        /// 7.5.4.1
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor5()
        {
            Test(@"class T { static T T; void M() { T.T = null; } }",
                Class("T"),
                Class("T"));
        }

        /// <summary>
        /// Needs to prefer the local
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor6()
        {
            Test(@"class T { int field; void M() { T T = new T(); T.field = 0; } }",
                Class("T"),
                Class("T"));
        }

        /// <summary>
        /// Needs to prefer the type
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor7()
        {
            Test(@"class T { static int field; void M() { T T = new T(); T.field = 0; } }",
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor8()
        {
            Test(@"class T { void M(T T) { } void M2() { T T = new T(); M(T); } }",
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor9()
        {
            Test(@"class T { T M(T T) { T = new T(); return T; } }",
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor10()
        {
            // note: 'var' now binds to the type of the local.
            Test(@"class T { void M() { var T = new object(); T temp = T as T; } }",
                Keyword("var"),
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor11()
        {
            Test(@"class T { void M() { var T = new object(); bool b = T is T; } }",
                Keyword("var"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor12()
        {
            Test(@"class T { void M() { T T = new T(); var t = typeof(T); } }",
                Class("T"),
                Class("T"),
                Keyword("var"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor13()
        {
            Test(@"class T { void M() { T T = new T(); T t = default(T); } }",
                Class("T"),
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ColorColor14()
        {
            Test(@"class T { void M() { object T = new T(); T t = (T)T; } }",
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NamespaceNameSameAsTypeName1()
        {
            Test(@"namespace T { class T { void M() { T.T T = new T.T(); } } }",
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NamespaceNameSameAsTypeNameWithGlobal()
        {
            Test(@"namespace T { class T { void M() { global::T.T T = new global::T.T(); } } }",
                Class("T"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AmbiguityTypeAsGenericMethodArgumentVsLocal()
        {
            Test(@"class T { void M<T>() { T T; M<T>(); } }",
                TypeParameter("T"),
                TypeParameter("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AmbiguityTypeAsGenericArgumentVsLocal()
        {
            Test(@"class T { class G<T> { } void M() { T T; G<T> g = new G<T>(); } }",
                Class("T"),
                Class("G"),
                Class("T"),
                Class("G"),
                Class("T"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AmbiguityTypeAsGenericArgumentVsField()
        {
            Test(@"class T { class H<T> { public static int f; } void M() { T T; int i = H<T>.f; } }",
                Class("T"),
                Class("H"),
                Class("T"));
        }

        /// <summary>
        /// 7.5.4.2
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void GrammarAmbiguity_7_5_4_2()
        {
            Test(@"class M
{
    void m()
    {
        int A = 2;
        int B = 3;
        F(G<A, B>(7));
    }
    void F(bool b) { }
    bool G<t, f>(int a) { return true; }
    class A { }
    class B { }
}",
                Class("A"),
                Class("B"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AnonymousTypePropertyName()
        {
            Test(@"using System; class C { void M() { var x = new { String = "" }; } }",
                Keyword("var"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void YieldAsATypeName()
        {
            Test(@"using System.Collections.Generic;
class yield { 
    IEnumerable<yield> M() { 
        yield yield = new yield(); 
        yield return yield; } }",
                Interface("IEnumerable"),
                Class("yield"),
                Class("yield"),
                Class("yield"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TypeNameDottedNames()
        {
            Test(@"class C { class Nested { } C.Nested f; }",
                Class("C"),
                Class("Nested"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void BindingTypeNameFromBCLViaGlobalAlias()
        {
            Test(@"using System; class C { global::System.String f; }",
                Class("String"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void BindingTypeNames()
        {
            string code = @"using System;
using Str = System.String;
class C
{
    class Nested { }
    Str UsingAlias;
    Nested NestedClass;
    String BCL;
    C ClassDeclaration;
    C.Nested FCNested;
    global::C FCN;
    global::System.String FCNBCL;
    global::Str GlobalUsingAlias;
}";
            Test(code,
                code,
                Options.Regular,
                Class("Str"),
                Class("String"),
                Class("Str"),
                Class("Nested"),
                Class("String"),
                Class("C"),
                Class("C"),
                Class("Nested"),
                Class("C"),
                Class("String"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TypesOfClassMembers()
        {
            Test(@"class Type
{
    public Type() { }
    static Type() { }
    ~Type() { }
    Type Property { get; set; }
    Type Method() { }
    event Type Event;
    Type this[Type index] { get; set; }
    Type field;
    const Type constant = null;
    static operator Type(Type other) { }
    static operator +(Type other) { }
    static operator int(Type other) { }
    static operator Type(int other) { }
}",
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"),
                Class("Type"));
        }

        /// <summary>
        /// NAQ = Namespace Alias Qualifier (?)
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQTypeNameCtor()
        {
            TestInMethod(@"System.IO.BufferedStream b = new global::System.IO.BufferedStream();",
                Class("BufferedStream"),
                Class("BufferedStream"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQEnum()
        {
            Test(@"class C { void M() { global::System.IO.DriveType d; } }",
                Enum("DriveType"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQDelegate()
        {
            Test(@"class C { void M() { global::System.AssemblyLoadEventHandler d; } }",
                Delegate("AssemblyLoadEventHandler"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQTypeNameMethodCall()
        {
            TestInMethod(@"global::System.String.Clone("");",
                Class("String"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQEventSubscription()
        {
            TestInMethod(@"global::System.AppDomain.CurrentDomain.AssemblyLoad += 
            delegate(object sender, System.AssemblyLoadEventArgs args) {};",
                Class("AppDomain"),
                Class("AssemblyLoadEventArgs"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AnonymousDelegateParameterType()
        {
            Test(@"class C { void M() { System.Action<System.EventArgs> a = delegate(System.EventArgs e) { }; } }",
                Delegate("Action"),
                Class("EventArgs"),
                Class("EventArgs"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQCtor()
        {
            TestInMethod(@"global::System.Collections.DictionaryEntry de = new global::System.Collections.DictionaryEntry();",
                Struct("DictionaryEntry"),
                Struct("DictionaryEntry"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQSameFileClass()
        {
            var code = @"class C { static void M() { global::C.M(); } }";

            Test(code,
                code,
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void InteractiveNAQSameFileClass()
        {
            var code = @"class C { static void M() { global::Script.C.M(); } }";

            Test(code,
                code,
                Options.Script,
                Class("Script"),
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQSameFileClassWithNamespace()
        {
            Test(@"using @global = N;
namespace N { class C { static void M() { global::N.C.M(); } } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQSameFileClassWithNamespaceAndEscapedKeyword()
        {
            Test(@"using @global = N;
namespace N { class C { static void M() { @global.C.M(); } } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQGlobalWarning()
        {
            Test(@"using global = N;
namespace N { class C { static void M() { global.C.M(); } } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQUserDefinedNAQNamespace()
        {
            Test(@"using foo = N;
namespace N { class C { static void M() { foo.C.M(); } } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQUserDefinedNAQNamespaceDoubleColon()
        {
            Test(@"using foo = N;
namespace N { class C { static void M() { foo::C.M(); } } }",
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQUserDefinedNamespace1()
        {
            Test(@"class C { void M() { A.B.D d; } }
namespace A { namespace B { class D { } } }",
                Class("D"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQUserDefinedNamespaceWithGlobal()
        {
            Test(@"class C { void M() { global::A.B.D d; } }
namespace A { namespace B { class D { } } }",
                Class("D"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQUserDefinedNAQForClass()
        {
            Test(@"using IO = global::System.IO;
class C { void M() { IO::BinaryReader b; } }",
                Class("BinaryReader"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NAQUserDefinedTypes()
        {
            Test(@"using rabbit = MyNameSpace;
class C { void M() {
        rabbit::MyClass2.method();
        new rabbit::MyClass2().myEvent += null;
        rabbit::MyEnum Enum;
        rabbit::MyStruct strUct;
        object o2 = rabbit::MyClass2.MyProp;
        object o3 = rabbit::MyClass2.myField;
        rabbit::MyClass2.MyDelegate del = null; } }
namespace MyNameSpace {
    namespace OtherNamespace { class A { } }
    public class MyClass2 {
        public static int myField;
        public delegate void MyDelegate();
        public event MyDelegate myEvent;
        public static void method() { }
        public static int MyProp { get { return 0; } } }
    struct MyStruct { }
    enum MyEnum { } }",
                Class("MyClass2"),
                Class("MyClass2"),
                Enum("MyEnum"),
                Struct("MyStruct"),
                Class("MyClass2"),
                Class("MyClass2"),
                Class("MyClass2"),
                Delegate("MyDelegate"),
                Delegate("MyDelegate"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PreferPropertyOverNestedClass()
        {
            Test(@"class Outer
{
    class A
    {
        public int B;
    }
    class B
    {
        void M()
        {
            A a = new A();
            a.B = 10;
        }
    }
}",
                Class("A"),
                Class("A"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TypeNameInsideNestedClass()
        {
            Test(@"using System;
class Outer
{
    class C
    {
        void M()
        {
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}",
                Class("Console"),
                Class("Console"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void StructEnumTypeNames()
        {
            Test(@"using System;
class C
{
    enum MyEnum { }
    struct MyStruct { }
    static void Main()
    {
        ConsoleColor c;
        Int32 i;
    }
}",
                Enum("ConsoleColor"),
                Struct("Int32"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PreferFieldOverClassWithSameName()
        {
            Test(@"class C
{
    public int C;
    void M()
    {
        C = 0;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void AttributeBinding()
        {
            Test(@"using System;
[Serializable]            // Binds to System.SerializableAttribute; colorized
class Serializable { }
[SerializableAttribute]   // Binds to System.SerializableAttribute; colorized
class Serializable { }
[NonSerialized]           // Binds to global::NonSerializedAttribute; not colorized
class NonSerializedAttribute { }
[NonSerializedAttribute]  // Binds to global::NonSerializedAttribute; not colorized
class NonSerializedAttribute { }
[Obsolete]                // Binds to global::Obsolete; colorized
class Obsolete : Attribute { }
[ObsoleteAttribute]       // Binds to global::Obsolete; colorized
class ObsoleteAttribute : Attribute { }",
                Class("Serializable"),
                Class("SerializableAttribute"),
                Class("Obsolete"),
                Class("Attribute"),
                Class("ObsoleteAttribute"),
                Class("Attribute"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ShouldNotClassifyNamespacesAsTypes()
        {
            Test(@"using System; namespace Roslyn.Compilers.Internal { }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NestedTypeCantHaveSameNameAsParentType()
        {
            Test(@"class Program
{
    class Program { }
    static void Main(Program p) { }
    Program.Program p2;
}",
                Class("Program"),
                Class("Program"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias()
        {
            var code = @"class Program
{
    class Program { }
    static void Main(Program p) { }
    global::Program.Program p;
}";

            Test(code,
                code,
                Class("Program"),
                Class("Program"),
                Class("Program"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void InteractiveNestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias()
        {
            var code = @"class Program
{
    class Program { }
    static void Main(Program p) { }
    global::Script.Program.Program p;
}";

            Test(code,
                code,
                Options.Script,
                Class("Program"),
                Class("Script"),
                Class("Program"),
                Class("Program"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void EnumFieldWithSameNameShouldBePreferredToType()
        {
            Test(@"enum E { E, F = E }");
        }

        [WorkItem(541150)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestGenericVarClassification()
        {
            Test(@"using System;
 
static class Program
{
    static void Main()
    {
        var x = 1;
    }
}
 
class var<T> { }
", Keyword("var"));
        }

        [WorkItem(541154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestInaccessibleVarClassification()
        {
            Test(@"using System;
 
class A
{
    private class var { }
}
 
class B : A
{
    static void Main()
    {
        var x = 1;
    }
}
",
                Class("A"),
                Keyword("var"));
        }

        [WorkItem(541154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestVarNamedTypeClassification()
        {
            Test(@"
class var
{
    static void Main()
    {
        var x;
    }
}",
                Class("var"));
        }

        [WorkItem(9513, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void RegressionFor9513()
        {
            Test(@"enum E { A, B }
class C
{
    void M()
    {
        switch (new E())
        {
            case E.A:
                goto case E.B;
            case E.B:
                goto default;
            default:
                goto case E.A;
        }
    }
}

",
                Enum("E"),
                Enum("E"),
                Enum("E"),
                Enum("E"),
                Enum("E"));
        }

        [WorkItem(542368)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void RegressionFor9572()
        {
            Test(@"
class A<T,S> where T : A<T,S>.I, A<T,T>.I
{
    public interface I { }
}
",
                TypeParameter("T"),
                Class("A"),
                TypeParameter("T"),
                TypeParameter("S"),
                Interface("I"),
                Class("A"),
                TypeParameter("T"),
                TypeParameter("T"),
                Interface("I"));
        }

        [WorkItem(542368)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void RegressionFor9831()
        {
            Test(@"F : A",
                @"
public class B<T>
{
    public class A
    {
    }
}
 
public class X : B<X>
{
    public class F : A
    {
    }
}
",
                Class("A"));
        }

        [WorkItem(542432)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestVar()
        {
            Test(@"class Program
{
    class var<T> { }
    static var<int> GetVarT() { return null; }
    static void Main()
    {
        var x = GetVarT();
        var y = new var<int>();
    }
}",
                Class("var"),
                Keyword("var"),
                Keyword("var"),
                Class("var"));
        }

        [WorkItem(543123)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestVar2()
        {
            Test(@"class Program
{
    void Main(string[] args)
    {
        foreach (var v in args) { }
    }
}
",
                Keyword("var"));
        }

        [WorkItem(542778)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestDuplicateTypeParamWithConstraint()
        {
            Test(@"where U : IEnumerable<S>", @"
using System.Collections.Generic;

class C<T>
{
    public void Foo<U, U>(U arg) where S : T where U : IEnumerable<S>
    {
    }
}
",
                TypeParameter("U"),
                Interface("IEnumerable"));
        }

        [WorkItem(542685)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void OptimisticallyColorFromInDeclaration()
        {
            TestInExpression("from ",
                Keyword("from"));
        }

        [WorkItem(542685)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void OptimisticallyColorFromInAssignment()
        {
            TestInMethod(@"var q = 3; q = from",
                Keyword("var"),
                Keyword("from"));
        }

        [WorkItem(542685)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DontColorThingsOtherThanFromInDeclaration()
        {
            TestInExpression("fro ");
        }

        [WorkItem(542685)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DontColorThingsOtherThanFromInAssignment()
        {
            TestInMethod("var q = 3; q = fro ",
                Keyword("var"));
        }

        [WorkItem(542685)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DontColorFromWhenBoundInDeclaration()
        {
            TestInMethod(@"
var from = 3;
var q = from ",
                Keyword("var"),
                Keyword("var"));
        }

        [WorkItem(542685)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DontColorFromWhenBoundInAssignment()
        {
            TestInMethod(@"
var q = 3;
var from = 3;
q = from ",
                Keyword("var"),
                Keyword("var"));
        }

        [WorkItem(543404)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NewOfClassWithOnlyPrivateConstructor()
        {
            Test(@"class X
{
    private X() { }
}

class Program
{
    static void Main(string[] args)
    {
        new X();
    }
}",
                Class("X"));
        }

        [WorkItem(544179)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestNullableVersusConditionalAmbiguity1()
        {
            Test(@"class Program
{
    static void Main(string[] args)
    {
        C1 ?
    }
}

public class C1
{
}
",
                Class("C1"));
        }

        [WorkItem(544179)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestPointerVersusMultiplyAmbiguity1()
        {
            Test(@"class Program
{
    static void Main(string[] args)
    {
        C1 *
    }
}

public class C1
{
}
",
                Class("C1"));
        }

        [WorkItem(544302)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void EnumTypeAssignedToNamedPropertyOfSameNameInAttributeCtor()
        {
            Test(@"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""abc"", CallingConvention = CallingConvention)]
    static extern void M();
}
",
                Class("DllImport"),
                Enum("CallingConvention"));
        }

        [WorkItem(531119)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void OnlyClassifyGenericNameOnce()
        {
            Test(@"
enum Type { }
struct Type<T>
{
    Type<int> f;
}
",
                Struct("Type"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NameOf1()
        {
            Test(@"
class C
{
    void foo()
    {
        var x = nameof
    }
}
",
                Keyword("var"),
                Keyword("nameof"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NameOf2()
        {
            Test(@"
class C
{
    void foo()
    {
        var x = nameof(C);
    }
}
",
                Keyword("var"),
                Keyword("nameof"),
                Class("C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void MethodCalledNameOfInScope()
        {
            Test(@"
class C
{
    void nameof(int i){ }

    void foo()
    {
        int y = 3;
        var x = nameof();
    }

}
",
                Keyword("var"));
        }

        [WorkItem(744813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestCreateWithBufferNotInWorkspace()
        {
            // don't crash
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(""))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

                var contentTypeService = document.GetLanguageService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();
                var extraBuffer = workspace.ExportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer("", contentType);
                var textView = workspace.ExportProvider.GetExportedValue<ITextEditorFactoryService>().CreateTextView(extraBuffer);

                var waiter = new Waiter();
                var provider = new SemanticClassificationViewTaggerProvider(
                    workspace.ExportProvider.GetExportedValue<IForegroundNotificationService>(),
                    workspace.ExportProvider.GetExportedValue<ISemanticChangeNotificationService>(),
                    workspace.ExportProvider.GetExportedValue<ClassificationTypeMap>(),
                    SpecializedCollections.SingletonEnumerable(
                        new Lazy<IAsynchronousOperationListener, FeatureMetadata>(
                        () => waiter, new FeatureMetadata(new Dictionary<string, object>() { { "FeatureName", FeatureAttribute.Classification } }))));

                using (var tagger = (IDisposable)provider.CreateTagger<IClassificationTag>(textView, extraBuffer))
                {
                    using (var edit = extraBuffer.CreateEdit())
                    {
                        edit.Insert(0, "class A { }");
                        edit.Apply();
                    }

                    await waiter.CreateWaitTask().ConfigureAwait(true);
                }
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestGetTagsOnBufferTagger()
        {
            // don't crash
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile("class C { C c; }"))
            {
                var document = workspace.Documents.First();

                var waiter = new Waiter();
                var provider = new SemanticClassificationBufferTaggerProvider(
                    workspace.ExportProvider.GetExportedValue<IForegroundNotificationService>(),
                    workspace.ExportProvider.GetExportedValue<ISemanticChangeNotificationService>(),
                    workspace.ExportProvider.GetExportedValue<ClassificationTypeMap>(),
                    SpecializedCollections.SingletonEnumerable(
                        new Lazy<IAsynchronousOperationListener, FeatureMetadata>(
                        () => waiter, new FeatureMetadata(new Dictionary<string, object>() { { "FeatureName", FeatureAttribute.Classification } }))));

                var tagger = provider.CreateTagger<IClassificationTag>(document.TextBuffer);
                using (var disposable = (IDisposable)tagger)
                {
                    await waiter.CreateWaitTask().ConfigureAwait(true);

                    var tags = tagger.GetTags(document.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection());
                    var allTags = tagger.GetAllTags(document.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection(), CancellationToken.None);

                    Assert.Empty(tags);
                    Assert.NotEmpty(allTags);

                    Assert.Equal(allTags.Count(), 1);
                }
            }
        }

        private class Waiter : AsynchronousOperationListener { }
    }
}

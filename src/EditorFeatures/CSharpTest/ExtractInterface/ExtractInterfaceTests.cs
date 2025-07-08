// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ExtractInterface;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractInterface;

[Trait(Traits.Feature, Traits.Features.ExtractInterface)]
public sealed class ExtractInterfaceTests : AbstractExtractInterfaceTests
{
    [WpfFact]
    public async Task ExtractInterface_Invocation_CaretInMethod()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                public void Goo()
                {
                    $$
                }
            }
            """, expectedSuccess: true);
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_CaretAfterClassClosingBrace()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                public void Goo()
                {

                }
            }$$
            """, expectedSuccess: true);
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_CaretBeforeClassKeyword()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            $$class MyClass
            {
                public void Goo()
                {

                }
            }
            """, expectedSuccess: true);
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromInnerClass1()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                public void Goo()
                {

                }

                class AnotherClass
                {
                    $$public void Bar()
                    {
                    }
                }
            }
            """, expectedSuccess: true, expectedMemberName: "Bar");
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromInnerClass2()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                public void Goo()
                {

                }

                $$class AnotherClass
                {
                    public async Task Bar()
                    {
                    }
                }
            }
            """, expectedSuccess: true, expectedMemberName: "Bar");
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromOuterClass()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                public void Goo()
                {

                }$$

                class AnotherClass
                {
                    public async Task Bar()
                    {
                    }
                }
            }
            """, expectedSuccess: true, expectedMemberName: "Goo");
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromInterface_01()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            interface IMyInterface
            {
                $$void Goo();
            }
            """, expectedSuccess: true, expectedMemberName: "Goo", expectedInterfaceName: "IMyInterface1");
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromInterface_02()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            interface IMyInterface()
            {
                $$void Goo();
            }
            """, expectedSuccess: true, expectedMemberName: "Goo", expectedInterfaceName: "IMyInterface1");
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromStruct()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            struct SomeStruct
            {
                $$public void Goo() { }
            }
            """, expectedSuccess: true, expectedMemberName: "Goo", expectedInterfaceName: "ISomeStruct");
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromNamespace()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            namespace Ns$$
            {
                class MyClass
                {
                    public async Task Goo() { }
                }
            }
            """, expectedSuccess: false);
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_DoesNotIncludeFields()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public int x;

                public void Goo()
                {
                }
            }
            """, expectedSuccess: true, expectedMemberName: "Goo");
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndSet()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public int Prop { get; set; }
            }
            """, expectedSuccess: true, expectedMemberName: "Prop");
    }

    [WpfFact]
    public async Task ExtractInterfaceAction_ExtractableMembers_IncludesPublicProperty_WithGetAndSet()
    {
        await TestExtractInterfaceCodeActionCSharpAsync("""
            class MyClass$$
            {
                public int Prop { get; set; }
            }
            """, """
            interface IMyClass
            {
                int Prop { get; set; }
            }

            class MyClass : IMyClass
            {
                public int Prop { get; set; }
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndPrivateSet()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public int Prop { get; private set; }
            }
            """, expectedSuccess: true, expectedMemberName: "Prop");
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGet()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public int Prop { get; }
            }
            """, expectedSuccess: true, expectedMemberName: "Prop");
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_ExcludesPublicProperty_WithPrivateGetAndPrivateSet()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public int Prop { private get; private set; }
            }
            """, expectedSuccess: false);
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_IncludesPublicIndexer()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public int this[int x] { get { return 5; } set { } }
            }
            """, expectedSuccess: true, expectedMemberName: "this[]");
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_ExcludesInternalIndexer()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$internal int this[int x] { get { return 5; } set { } }
            }
            """, expectedSuccess: false);
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_IncludesPublicMethod()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public void M()
                {
                }
            }
            """, expectedSuccess: true, expectedMemberName: "M");
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_ExcludesInternalMethod()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$internal void M()
                {
                }
            }
            """, expectedSuccess: false);
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_IncludesAbstractMethod()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            abstract class MyClass
            {
                $$public abstract void M();
            }
            """, expectedSuccess: true, expectedMemberName: "M");
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_IncludesPublicEvent()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public event Action MyEvent;
            }
            """, expectedSuccess: true, expectedMemberName: "MyEvent");
    }

    [WpfFact]
    public async Task ExtractInterface_ExtractableMembers_ExcludesPrivateEvent()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$private event Action MyEvent;
            }
            """, expectedSuccess: false);
    }

    [WpfFact]
    public async Task ExtractInterface_DefaultInterfaceName_DoesNotConflictWithOtherTypeNames()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public void Goo() { }
            }

            interface IMyClass { }
            struct IMyClass1 { }
            class IMyClass2 { }
            """, expectedSuccess: true, expectedInterfaceName: "IMyClass3");
    }

    [WpfFact]
    public async Task ExtractInterface_NamespaceName_NoNamespace()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class MyClass
            {
                $$public void Goo() { }
            }
            """, expectedSuccess: true, expectedNamespaceName: "");
    }

    [WpfFact]
    public async Task ExtractInterface_NamespaceName_SingleNamespace()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            namespace MyNamespace
            {
                class MyClass
                {
                    $$public void Goo() { }
                }
            }
            """, expectedSuccess: true, expectedNamespaceName: "MyNamespace");
    }

    [WpfFact]
    public async Task ExtractInterface_NamespaceName_NestedNamespaces()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            namespace OuterNamespace
            {
                namespace InnerNamespace
                {
                    class MyClass
                    {
                        $$public void Goo() { }
                    }
                }
            }
            """, expectedSuccess: true, expectedNamespaceName: "OuterNamespace.InnerNamespace");
    }

    [WpfFact]
    public async Task ExtractInterface_NamespaceName_NestedNamespaces_FileScopedNamespace1()
    {
        var markup = """
            using System;
            namespace OuterNamespace
            {
                namespace InnerNamespace
                {
                    class MyClass
                    {
                        $$public void Goo() { }
                    }
                }
            }
            """;

        using var testState = ExtractInterfaceTestState.Create(
            markup, LanguageNames.CSharp,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10),
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Silent }
            });

        var result = await testState.ExtractViaCommandAsync();

        var interfaceDocument = result.UpdatedSolution.GetRequiredDocument(result.NavigationDocumentId);
        var interfaceCode = (await interfaceDocument.GetTextAsync()).ToString();

        Assert.Equal("""
            namespace OuterNamespace.InnerNamespace;

            internal interface IMyClass
            {
                void Goo();
            }
            """, interfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_NamespaceName_NestedNamespaces_FileScopedNamespace2()
    {
        var markup = """
            using System;
            namespace OuterNamespace
            {
                namespace InnerNamespace
                {
                    class MyClass
                    {
                        $$public void Goo() { }
                    }
                }
            }
            """;

        using var testState = ExtractInterfaceTestState.Create(
            markup, LanguageNames.CSharp,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9),
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Silent }
            });

        var result = await testState.ExtractViaCommandAsync();

        var interfaceDocument = result.UpdatedSolution.GetRequiredDocument(result.NavigationDocumentId);
        var interfaceCode = (await interfaceDocument.GetTextAsync()).ToString();

        Assert.Equal("""
            namespace OuterNamespace.InnerNamespace
            {
                internal interface IMyClass
                {
                    void Goo();
                }
            }
            """, interfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_NamespaceName_NestedNamespaces_FileScopedNamespace3()
    {
        var markup = """
            using System;
            namespace OuterNamespace
            {
                namespace InnerNamespace
                {
                    class MyClass
                    {
                        $$public void Goo() { }
                    }
                }
            }
            """;

        using var testState = ExtractInterfaceTestState.Create(
            markup, LanguageNames.CSharp,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10),
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Silent }
            });

        var result = await testState.ExtractViaCommandAsync();

        var interfaceDocument = result.UpdatedSolution.GetRequiredDocument(result.NavigationDocumentId);
        var interfaceCode = (await interfaceDocument.GetTextAsync()).ToString();

        Assert.Equal("""
            namespace OuterNamespace.InnerNamespace
            {
                internal interface IMyClass
                {
                    void Goo();
                }
            }
            """, interfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_ClassesImplementExtractedInterface()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            class MyClass
            {
                $$public void Goo() { }
            }
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            using System;

            class MyClass : IMyClass
            {
                public void Goo() { }
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_StructsImplementExtractedInterface()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            struct MyStruct
            {
                $$public void Goo() { }
            }
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            using System;

            struct MyStruct : IMyStruct
            {
                public void Goo() { }
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_InterfacesDoNotImplementExtractedInterface()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            interface MyInterface
            {
                $$void Goo();
            }
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            using System;

            interface MyInterface
            {
                void Goo();
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_Methods()
    {
        var expectedInterfaceCode = """
            using System.Diagnostics;

            interface IMyClass
            {
                int RequiredProperty { get; set; }

                void ExtractableMethod_Abstract();
                void ExtractableMethod_Normal();
                void ExtractableMethod_ParameterTypes(CorrelationManager x, int? y = 7, string z = "42");
                void NotActuallyUnsafeMethod(int p);
                unsafe void UnsafeMethod(int* p);
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            abstract class MyClass$$
            {
                public required int RequiredProperty { get; set; }
                public void ExtractableMethod_Normal() { }
                public void ExtractableMethod_ParameterTypes(System.Diagnostics.CorrelationManager x, Nullable<Int32> y = 7, string z = "42") { }
                public abstract void ExtractableMethod_Abstract();
                unsafe public void NotActuallyUnsafeMethod(int p) { }
                unsafe public void UnsafeMethod(int *p) { }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_MethodsInRecord()
    {
        var expectedInterfaceCode = """
            interface IR
            {
                bool Equals(object obj);
                bool Equals(R other);
                int GetHashCode();
                void M();
                string ToString();
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            abstract record R$$
            {
                public void M() { }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_Events()
    {
        var expectedInterfaceCode = """
            using System;

            internal interface IMyClass
            {
                event Action ExtractableEvent1;
                event Action<int?> ExtractableEvent2;
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            abstract internal class MyClass$$
            {
                public event Action ExtractableEvent1;
                public event Action<Nullable<Int32>> ExtractableEvent2;
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_Properties()
    {
        var expectedInterfaceCode = """
            interface IMyClass
            {
                int ExtractableProp { get; set; }
                int ExtractableProp_GetOnly { get; }
                int ExtractableProp_SetOnly { set; }
                int ExtractableProp_SetPrivate { get; }
                int ExtractableProp_GetPrivate { set; }
                int ExtractableProp_SetInternal { get; }
                int ExtractableProp_GetInternal { set; }
                int NotActuallyUnsafeProp { get; set; }
                unsafe int* UnsafeProp { get; set; }
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            abstract class MyClass$$
            {
                public int ExtractableProp { get; set; }
                public int ExtractableProp_GetOnly { get { return 1; } }
                public int ExtractableProp_SetOnly { set { } }
                public int ExtractableProp_SetPrivate { get; private set; }
                public int ExtractableProp_GetPrivate { private get; set; }
                public int ExtractableProp_SetInternal { get; internal set; }
                public int ExtractableProp_GetInternal { internal get; set; }
                unsafe public int NotActuallyUnsafeProp { get; set; }
                unsafe public int* UnsafeProp { get; set; }

            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_Indexers()
    {
        var expectedInterfaceCode = """
            interface IMyClass
            {
                int this[int x] { set; }
                int this[string x] { get; }
                int this[double x] { get; set; }
                int this[int? x, string y = "42"] { get; set; }
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            using System;

            abstract class MyClass$$
            {
                public int this[int x] { set { } }
                public int this[string x] { get { return 1; } }
                public int this[double x] { get { return 1; } set { } }
                public int this[Nullable<Int32> x, string y = "42"] { get { return 1; } set { } }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_Imports()
    {
        var expectedInterfaceCode = """
            using System.Collections.Generic;
            using System.Diagnostics;
            using System.Globalization;
            using System.IO;
            using System.Net;

            public interface IClass
            {
                BooleanSwitch M1(Calendar x);
                void M2(List<BinaryWriter> x);
                void M3<T>() where T : WebProxy;
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            public class Class
            {
                $$public System.Diagnostics.BooleanSwitch M1(System.Globalization.Calendar x) { return null; }
                public void M2(System.Collections.Generic.List<System.IO.BinaryWriter> x) { }
                public void M3<T>() where T : System.Net.WebProxy { }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_ImportsInsideNamespace()
    {
        var markup = """
            namespace N
            {
                public class Class
                {
                    $$public System.Diagnostics.BooleanSwitch M1(System.Globalization.Calendar x) { return null; }
                    public void M2(System.Collections.Generic.List<System.IO.BinaryWriter> x) { }
                    public void M3<T>() where T : System.Net.WebProxy { }
                }
            }
            """;
        using var testState = ExtractInterfaceTestState.Create(
            markup, LanguageNames.CSharp,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10),
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace }
            });

        var result = await testState.ExtractViaCommandAsync();

        var interfaceDocument = result.UpdatedSolution.GetRequiredDocument(result.NavigationDocumentId);
        var interfaceCode = (await interfaceDocument.GetTextAsync()).ToString();

        Assert.Equal("""
            namespace N
            {
                using System.Collections.Generic;
                using System.Diagnostics;
                using System.Globalization;
                using System.IO;
                using System.Net;

                public interface IClass
                {
                    BooleanSwitch M1(Calendar x);
                    void M2(List<BinaryWriter> x);
                    void M3<T>() where T : WebProxy;
                }
            }
            """, interfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_TypeParameters1()
    {
        var expectedInterfaceCode = """
            public interface IClass<A, B, C, D, E, F, G, H> where E : F
            {
                List<G> this[List<List<H>> list] { set; }

                List<E> Prop { set; }

                event Func<D> Goo4;

                void Bar1();
                void Goo1(A a);
                B Goo2();
                void Goo3(List<C> list);
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            public class Class<A, B, C, D, E, F, G, H, NO1> where E : F
            {
            	$$public void Goo1(A a) { }
            	public B Goo2() { return default(B); }
            	public void Goo3(List<C> list) { }

            	public event Func<D> Goo4;

            	public List<E> Prop { set { } }
            	public List<G> this[List<List<H>> list] { set { } }

            	public void Bar1() { var x = default(NO1); }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
    [WpfFact]
    public async Task ExtractInterface_CodeGen_TypeParameters2()
    {
        var expectedInterfaceCode = """
            using System.Collections.Generic;

            interface IProgram<A, B, D, E>
                where A : List<B>
                where B : Dictionary<List<D>, List<E>>
            {
                void Goo<T>(T t) where T : List<A>;
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            using System.Collections.Generic;

            class Program<A, B, C, D, E> where A : List<B> where B : Dictionary<List<D>, List<E>>
            {
                $$public void Goo<T>(T t) where T : List<A> { }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_TypeParameters3()
    {
        var expectedInterfaceCode = """
            interface IClass1<A, B>
            {
                void method(A P1, Class1<A, B>.Class2 P2);
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            class $$Class1<A, B>
            {
                public void method(A P1, Class2 P2)
                {
                }
                public class Class2
                {
                }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
    [WpfFact]
    public async Task ExtractInterface_CodeGen_TypeParameters4()
    {
        var expectedInterfaceCode = """
            using System.Collections;

            public interface IC4<A, B, C>
                where B : new()
                where C : ICollection
            {
                C this[int i] { get; }

                B property { set; }

                A method();
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync("""
            class C1<A>
            {
                public class C2<B> where B : new()
                {
                    public class C3<C> where C : System.Collections.ICollection
                    {
                        public class C4
                        {$$
                            public A method() { return default(A); }
                            public B property { set { } }
                            public C this[int i] { get { return default(C); } }
                        }
                    }
                }
            }
            """, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_AccessibilityModifiers()
    {
        var markup = """
            using System;

            abstract class MyClass$$
            {
                public void Goo() { }
            }
            """;

        using var testState = ExtractInterfaceTestState.Create(
            markup, LanguageNames.CSharp,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Always, NotificationOption2.Silent }
            });

        var result = await testState.ExtractViaCommandAsync();

        var interfaceDocument = result.UpdatedSolution.GetRequiredDocument(result.NavigationDocumentId);
        var interfaceCode = (await interfaceDocument.GetTextAsync()).ToString();

        Assert.Equal("""
            internal interface IMyClass
            {
                void Goo();
            }
            """, interfaceCode);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_BaseList_NewBaseListNonGeneric()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class Program
            {
                $$public void Goo() { }
            }
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            class Program : IProgram
            {
                public void Goo() { }
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_BaseList_NewBaseListGeneric()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class Program<T>
            {
                $$public void Goo(T t) { }
            }
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            class Program<T> : IProgram<T>
            {
                public void Goo(T t) { }
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_BaseList_NewBaseListWithWhereClause()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class Program<T, U> where T : U
            {
                $$public void Goo(T t, U u) { }
            }
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            class Program<T, U> : IProgram<T, U> where T : U
            {
                public void Goo(T t, U u) { }
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList1()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class Program : ISomeInterface
            {
                $$public void Goo() { }
            }

            interface ISomeInterface {}
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            class Program : ISomeInterface, IProgram
            {
                public void Goo() { }
            }

            interface ISomeInterface {}
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList2()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class Program<T, U> : ISomeInterface<T>
            {
                $$public void Goo(T t, U u) { }
            }

            interface ISomeInterface<T> {}
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            class Program<T, U> : ISomeInterface<T>, IProgram<T, U>
            {
                public void Goo(T t, U u) { }
            }

            interface ISomeInterface<T> {}
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList3()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>
            {
                $$public void Goo(T t, U u) { }
            }

            interface ISomeInterface<T> {}
            interface ISomeInterface2<T, U> {}
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>, IProgram<T, U>
            {
                public void Goo(T t, U u) { }
            }

            interface ISomeInterface<T> {}
            interface ISomeInterface2<T, U> {}
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList4()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U> where T : U
            {
                $$public void Goo(T t, U u) { }
            }

            interface ISomeInterface<T> {}
            interface ISomeInterface2<T, U> {}
            """, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: """
            class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>, IProgram<T, U> where T : U
            {
                public void Goo(T t, U u) { }
            }

            interface ISomeInterface<T> {}
            interface ISomeInterface2<T, U> {}
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly1()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class Program<T, U> : ISomeInterface<T> where T : U
            {
                $$public void Goo(T t, U u) { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: false);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly2()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class Program<T, U> $$: ISomeInterface<T> where T : U
            {
                public void Goo(T t, U u) { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly3()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class$$ Program<T, U> : ISomeInterface<T> where T : U
            {
                public void Goo(T t, U u) { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly4()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class Program<T, U>$$ : ISomeInterface<T> where T : U
            {
                public void Goo(T t, U u) { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly5()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class Program  $$ <T, U> : ISomeInterface<T> where T : U
            {
                public void Goo(T t, U u) { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly6()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class $$Program   <T, U> : ISomeInterface<T> where T : U
            {
                public void Goo(T t, U u) { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly7()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class $$Program : ISomeInterface<object>
            {
                public void Goo() { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly8()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class Program$$ : ISomeInterface<object>
            {
                public void Goo() { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly9()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class$$ Program : ISomeInterface<object>
            {
                public void Goo() { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly10()
    {
        await TestTypeDiscoveryAsync("""
            interface ISomeInterface<T> {}
            class Program $$: ISomeInterface<object>
            {
                public void Goo() { }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    [WpfFact]
    public async Task ExtractInterface_TypeDiscovery_NameOnly11()
    {
        await TestTypeDiscoveryAsync("""
            namespace N
            {
            $$    class Program
                {
                    public void Goo() { }
                }
            }
            """, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
    }

    private static async Task TestTypeDiscoveryAsync(
        string markup,
        TypeDiscoveryRule typeDiscoveryRule,
        bool expectedExtractable)
    {
        using var testState = ExtractInterfaceTestState.Create(markup, LanguageNames.CSharp, compilationOptions: null);
        var result = await testState.GetTypeAnalysisResultAsync(typeDiscoveryRule);
        Assert.Equal(expectedExtractable, result.CanExtractInterface);
    }

    [WpfFact]
    public async Task ExtractInterface_GeneratedNameTypeParameterSuffix1()
    {
        var expectedTypeParameterSuffix = @"<T>";
        await TestExtractInterfaceCommandCSharpAsync("""
            class $$Test<T>
            {
                public void M(T a) { }
            }
            """, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
    }

    [WpfFact]
    public async Task ExtractInterface_GeneratedNameTypeParameterSuffix2()
    {
        var expectedTypeParameterSuffix = @"<T>";
        await TestExtractInterfaceCommandCSharpAsync("""
            class $$Test<T, U>
            {
                public void M(T a) { }
            }
            """, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
    }

    [WpfFact]
    public async Task ExtractInterface_GeneratedNameTypeParameterSuffix3()
    {
        var expectedTypeParameterSuffix = @"<T, U>";
        await TestExtractInterfaceCommandCSharpAsync("""
            class $$Test<T, U>
            {
                public void M(T a, U b) { }
            }
            """, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
    }

    [WpfFact, Trait(Traits.Feature, Traits.Features.Interactive)]
    public void ExtractInterfaceCommandDisabledInSubmission()
    {
        using var workspace = EditorTestWorkspace.Create(XElement.Parse("""
            <Workspace>
                <Submission Language="C#" CommonReferences="true">  
                    public class $$C
                    {
                        public void M() { }
                    }
                </Submission>
            </Workspace>
            """),
            workspaceKind: WorkspaceKind.Interactive,
            composition: EditorTestCompositions.EditorFeatures);
        // Force initialization.
        workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id)!.GetTextView()).ToList();

        var textView = workspace.Documents.Single().GetTextView();

        var handler = workspace.ExportProvider.GetCommandHandler<ExtractInterfaceCommandHandler>(PredefinedCommandHandlerNames.ExtractInterface, ContentTypeNames.CSharpContentType);

        var state = handler.GetCommandState(new ExtractInterfaceCommandArgs(textView, textView.TextBuffer));
        Assert.True(state.IsUnspecified);
    }

    [WpfFact]
    public async Task TestInWithMethod_Parameters()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class $$TestClass
            {
                public void Method(in int p1)
                {
                }
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass
            {
                void Method(in int p1);
            }
            """);
    }

    [WpfFact]
    public async Task TestRefReadOnlyWithMethod_ReturnType()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class $$TestClass
            {
                public ref readonly int Method() => throw null;
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass
            {
                ref readonly int Method();
            }
            """);
    }

    [WpfFact]
    public async Task TestRefReadOnlyWithProperty()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class $$TestClass
            {
                public ref readonly int Property => throw null;
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass
            {
                ref readonly int Property { get; }
            }
            """);
    }

    [WpfFact]
    public async Task TestInWithIndexer_Parameters()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class $$TestClass
            {
                public int this[in int p1] { set { } }
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass
            {
                int this[in int p1] { set; }
            }
            """);
    }

    [WpfFact]
    public async Task TestRefReadOnlyWithIndexer_ReturnType()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            using System;
            class $$TestClass
            {
                public ref readonly int this[int p1] => throw null;
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass
            {
                ref readonly int this[int p1] { get; }
            }
            """);
    }

    [WpfFact]
    public async Task TestUnmanagedConstraint_Type()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class $$TestClass<T> where T : unmanaged
            {
                public void M(T arg) => throw null;
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass<T> where T : unmanaged
            {
                void M(T arg);
            }
            """);
    }

    [WpfFact]
    public async Task TestUnmanagedConstraint_Method()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class $$TestClass
            {
                public void M<T>() where T : unmanaged => throw null;
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass
            {
                void M<T>() where T : unmanaged;
            }
            """);
    }

    [WpfFact]
    public async Task TestNotNullConstraint_Type()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class $$TestClass<T> where T : notnull
            {
                public void M(T arg) => throw null;
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass<T> where T : notnull
            {
                void M(T arg);
            }
            """);
    }

    [WpfFact]
    public async Task TestNotNullConstraint_Method()
    {
        await TestExtractInterfaceCommandCSharpAsync("""
            class $$TestClass
            {
                public void M<T>() where T : notnull => throw null;
            }
            """, expectedSuccess: true, expectedInterfaceCode:
            """
            interface ITestClass
            {
                void M<T>() where T : notnull;
            }
            """);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/23855")]
    [WpfFact]
    public async Task TestExtractInterface_WithCopyright1()
    {
        var expectedInterfaceCode =
            """
            // Copyright

            public interface IGoo
            {
                void Test();
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync(
            """
            // Copyright

            public class $$Goo
            {
                public void Test()
                {
                }
            }
            """,
            expectedSuccess: true,
            expectedUpdatedOriginalDocumentCode: """
            // Copyright

            public class Goo : IGoo
            {
                public void Test()
                {
                }
            }
            """,
            expectedInterfaceCode: expectedInterfaceCode);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/23855")]
    [WpfFact]
    public async Task TestExtractInterface_WithCopyright2()
    {
        var expectedInterfaceCode =
            """
            // Copyright

            public interface IA
            {
                void Test();
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync(
            """
            // Copyright

            public class Goo
            {
                public class $$A
                {
                    public void Test()
                    {
                    }
                }
            }
            """,
            expectedSuccess: true,
            expectedUpdatedOriginalDocumentCode: """
            // Copyright

            public class Goo
            {
                public class A : IA
                {
                    public void Test()
                    {
                    }
                }
            }
            """,
            expectedInterfaceCode: expectedInterfaceCode);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/49739")]
    [WpfFact]
    public async Task TestRecord1()
    {
        var expectedInterfaceCode =
            """
            namespace Test
            {
                interface IWhatever
                {
                    int X { get; init; }
                    string Y { get; init; }

                    void Deconstruct(out int X, out string Y);
                    bool Equals(object obj);
                    bool Equals(Whatever other);
                    int GetHashCode();
                    string ToString();
                }
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                record $$Whatever(int X, string Y);
            }
            """,
            expectedSuccess: true,
            expectedUpdatedOriginalDocumentCode: """
            namespace Test
            {
                record Whatever(int X, string Y) : IWhatever;
            }
            """,
            expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task TestClass1()
    {
        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                class $$Whatever(int X, string Y);
            }
            """,
            expectedSuccess: false);
    }

    [WpfFact]
    public async Task TestStruct1()
    {
        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                struct $$Whatever(int X, string Y);
            }
            """,
            expectedSuccess: false);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/49739")]
    [WpfFact]
    public async Task TestRecord2()
    {
        var expectedInterfaceCode =
            """
            namespace Test
            {
                interface IWhatever
                {
                    int X { get; init; }
                    string Y { get; init; }

                    void Deconstruct(out int X, out string Y);
                    bool Equals(object obj);
                    bool Equals(Whatever other);
                    int GetHashCode();
                    string ToString();
                }
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                record $$Whatever(int X, string Y) { }
            }
            """,
            expectedSuccess: true,
            expectedUpdatedOriginalDocumentCode: """
            namespace Test
            {
                record Whatever(int X, string Y) : IWhatever { }
            }
            """,
            expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task TestClass2()
    {
        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                class $$Whatever(int X, string Y) { }
            }
            """,
            expectedSuccess: false);
    }

    [WpfFact]
    public async Task TestStruct2()
    {
        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                struct $$Whatever(int X, string Y) { }
            }
            """,
            expectedSuccess: false);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/49739")]
    [WpfFact]
    public async Task TestRecord3()
    {
        var expectedInterfaceCode =
            """
            namespace Test
            {
                interface IWhatever
                {
                    int X { get; init; }
                    string Y { get; init; }

                    void Deconstruct(out int X, out string Y);
                    bool Equals(object obj);
                    bool Equals(Whatever other);
                    int GetHashCode();
                    string ToString();
                }
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                /// <summary></summary>
                record $$Whatever(int X, string Y);
            }
            """,
            expectedSuccess: true,
            expectedUpdatedOriginalDocumentCode: """
            namespace Test
            {
                /// <summary></summary>
                record Whatever(int X, string Y) : IWhatever;
            }
            """,
            expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact]
    public async Task TestClass3()
    {
        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                /// <summary></summary>
                class $$Whatever(int X, string Y);
            }
            """,
            expectedSuccess: false);
    }

    [WpfFact]
    public async Task TestStruct3()
    {
        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                /// <summary></summary>
                struct $$Whatever(int X, string Y);
            }
            """,
            expectedSuccess: false);
    }

    [WpfFact]
    public async Task TestStruct4()
    {
        var expectedInterfaceCode =
            """
            namespace Test
            {
                interface IWhatever
                {
                    int I { get; set; }
                }
            }
            """;

        await TestExtractInterfaceCommandCSharpAsync(
            """
            namespace Test
            {
                struct $$Whatever
                {
                    public int I { get; set; }
                }
            }
            """,
            expectedSuccess: true,
            expectedInterfaceCode: expectedInterfaceCode);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/71718")]
    public async Task RemoveEnumeratorCancellationAttribute()
    {
        await TestExtractInterfaceCodeActionCSharpAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferencesNet8="true">
                    <Document FilePath="file.cs"><![CDATA[using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            public class $$Class1(int count) 
            {
                public async IAsyncEnumerable<int> Foo([EnumeratorCancellation] CancellationToken token)
                {
                    for (int i = 0; i < count; i++)
                    {
                        await Task.Yield();
                        yield return i;
                    }
                }
            }]]></Document>
                </Project>
            </Workspace>
            """, """
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            public interface IClass1
            {
                IAsyncEnumerable<int> Foo(CancellationToken token);
            }

            public class Class1(int count) : IClass1
            {
                public async IAsyncEnumerable<int> Foo([EnumeratorCancellation] CancellationToken token)
                {
                    for (int i = 0; i < count; i++)
                    {
                        await Task.Yield();
                        yield return i;
                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54019")]
    public async Task TestStaticMember()
    {
        await TestExtractInterfaceCodeActionCSharpAsync("""
            using System;

            class MyClass$$
            {
                public static void M() { }
                public static int Prop { get; set; }
                public static event Action Event;
            }
            """, """
            using System;

            interface IMyClass
            {
                static abstract int Prop { get; set; }

                static abstract event Action Event;

                static abstract void M();
            }

            class MyClass : IMyClass
            {
                public static void M() { }
                public static int Prop { get; set; }
                public static event Action Event;
            }
            """);
    }

    [WpfFact]
    public async Task ExtractInterface_Invocation_FromExtension()
    {
        await TestExtractInterfaceCommandCSharpAsync(
            """
            using System;

            static class C
            {
                $$extension(string s)
                {
                    public void Goo() { }
                }
            }
            """, expectedSuccess: false,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersionExtensions.CSharpNext));
    }
}

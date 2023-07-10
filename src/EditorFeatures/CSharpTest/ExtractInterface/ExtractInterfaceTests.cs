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
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractInterface
{
    public class ExtractInterfaceTests : AbstractExtractInterfaceTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_CaretInMethod()
        {
            var markup = """
                using System;
                class MyClass
                {
                    public void Goo()
                    {
                        $$
                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_CaretAfterClassClosingBrace()
        {
            var markup = """
                using System;
                class MyClass
                {
                    public void Goo()
                    {

                    }
                }$$
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_CaretBeforeClassKeyword()
        {
            var markup = """
                using System;
                $$class MyClass
                {
                    public void Goo()
                    {

                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_FromInnerClass1()
        {
            var markup = """
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
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_FromInnerClass2()
        {
            var markup = """
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
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_FromOuterClass()
        {
            var markup = """
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
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Goo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_FromInterface_01()
        {
            var markup = """
                using System;
                interface IMyInterface
                {
                    $$void Goo();
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Goo", expectedInterfaceName: "IMyInterface1");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_FromInterface_02()
        {
            var markup = """
                using System;
                interface IMyInterface()
                {
                    $$void Goo();
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Goo", expectedInterfaceName: "IMyInterface1");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_FromStruct()
        {
            var markup = """
                using System;
                struct SomeStruct
                {
                    $$public void Goo() { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Goo", expectedInterfaceName: "ISomeStruct");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_Invocation_FromNamespace()
        {
            var markup = """
                using System;

                namespace Ns$$
                {
                    class MyClass
                    {
                        public async Task Goo() { }
                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_DoesNotIncludeFields()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public int x;

                    public void Goo()
                    {
                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Goo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndSet()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public int Prop { get; set; }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Prop");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterfaceAction_ExtractableMembers_IncludesPublicProperty_WithGetAndSet()
        {
            var markup = """
                class MyClass$$
                {
                    public int Prop { get; set; }
                }
                """;

            var expectedMarkup = """
                interface IMyClass
                {
                    int Prop { get; set; }
                }

                class MyClass : IMyClass
                {
                    public int Prop { get; set; }
                }
                """;

            await TestExtractInterfaceCodeActionCSharpAsync(markup, expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndPrivateSet()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public int Prop { get; private set; }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Prop");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGet()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public int Prop { get; }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "Prop");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_ExcludesPublicProperty_WithPrivateGetAndPrivateSet()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public int Prop { private get; private set; }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_IncludesPublicIndexer()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public int this[int x] { get { return 5; } set { } }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "this[]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_ExcludesInternalIndexer()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$internal int this[int x] { get { return 5; } set { } }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_IncludesPublicMethod()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public void M()
                    {
                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_ExcludesInternalMethod()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$internal void M()
                    {
                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_IncludesAbstractMethod()
        {
            var markup = """
                using System;
                abstract class MyClass
                {
                    $$public abstract void M();
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_IncludesPublicEvent()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public event Action MyEvent;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedMemberName: "MyEvent");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_ExtractableMembers_ExcludesPrivateEvent()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$private event Action MyEvent;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_DefaultInterfaceName_DoesNotConflictWithOtherTypeNames()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public void Goo() { }
                }

                interface IMyClass { }
                struct IMyClass1 { }
                class IMyClass2 { }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceName: "IMyClass3");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_NamespaceName_NoNamespace()
        {
            var markup = """
                using System;
                class MyClass
                {
                    $$public void Goo() { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedNamespaceName: "");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_NamespaceName_SingleNamespace()
        {
            var markup = """
                using System;
                namespace MyNamespace
                {
                    class MyClass
                    {
                        $$public void Goo() { }
                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedNamespaceName: "MyNamespace");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_NamespaceName_NestedNamespaces()
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

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedNamespaceName: "OuterNamespace.InnerNamespace");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_ClassesImplementExtractedInterface()
        {
            var markup = """
                using System;

                class MyClass
                {
                    $$public void Goo() { }
                }
                """;

            var expectedCode = """
                using System;

                class MyClass : IMyClass
                {
                    public void Goo() { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_StructsImplementExtractedInterface()
        {
            var markup = """
                using System;

                struct MyStruct
                {
                    $$public void Goo() { }
                }
                """;

            var expectedCode = """
                using System;

                struct MyStruct : IMyStruct
                {
                    public void Goo() { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_InterfacesDoNotImplementExtractedInterface()
        {
            var markup = """
                using System;

                interface MyInterface
                {
                    $$void Goo();
                }
                """;

            var expectedCode = """
                using System;

                interface MyInterface
                {
                    void Goo();
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_Methods()
        {
            var markup = """
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
                """;

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

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_MethodsInRecord()
        {
            var markup = """
                abstract record R$$
                {
                    public void M() { }
                }
                """;

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

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_Events()
        {
            var markup = """
                using System;

                abstract internal class MyClass$$
                {
                    public event Action ExtractableEvent1;
                    public event Action<Nullable<Int32>> ExtractableEvent2;
                }
                """;

            var expectedInterfaceCode = """
                using System;

                internal interface IMyClass
                {
                    event Action ExtractableEvent1;
                    event Action<int?> ExtractableEvent2;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_Properties()
        {
            var markup = """
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
                """;

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

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_Indexers()
        {
            var markup = """
                using System;

                abstract class MyClass$$
                {
                    public int this[int x] { set { } }
                    public int this[string x] { get { return 1; } }
                    public int this[double x] { get { return 1; } set { } }
                    public int this[Nullable<Int32> x, string y = "42"] { get { return 1; } set { } }
                }
                """;

            var expectedInterfaceCode = """
                interface IMyClass
                {
                    int this[int x] { set; }
                    int this[string x] { get; }
                    int this[double x] { get; set; }
                    int this[int? x, string y = "42"] { get; set; }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_Imports()
        {
            var markup = """
                public class Class
                {
                    $$public System.Diagnostics.BooleanSwitch M1(System.Globalization.Calendar x) { return null; }
                    public void M2(System.Collections.Generic.List<System.IO.BinaryWriter> x) { }
                    public void M3<T>() where T : System.Net.WebProxy { }
                }
                """;

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

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
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

            var expectedInterfaceCode = """
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

            Assert.Equal(expectedInterfaceCode, interfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_TypeParameters1()
        {
            var markup = """
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
                """;

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

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_TypeParameters2()
        {
            var markup = """
                using System.Collections.Generic;

                class Program<A, B, C, D, E> where A : List<B> where B : Dictionary<List<D>, List<E>>
                {
                    $$public void Goo<T>(T t) where T : List<A> { }
                }
                """;

            var expectedInterfaceCode = """
                using System.Collections.Generic;

                interface IProgram<A, B, D, E>
                    where A : List<B>
                    where B : Dictionary<List<D>, List<E>>
                {
                    void Goo<T>(T t) where T : List<A>;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_TypeParameters3()
        {
            var markup = """
                class $$Class1<A, B>
                {
                    public void method(A P1, Class2 P2)
                    {
                    }
                    public class Class2
                    {
                    }
                }
                """;

            var expectedInterfaceCode = """
                interface IClass1<A, B>
                {
                    void method(A P1, Class1<A, B>.Class2 P2);
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_TypeParameters4()
        {
            var markup = """
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
                """;

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

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_BaseList_NewBaseListNonGeneric()
        {
            var markup = """
                class Program
                {
                    $$public void Goo() { }
                }
                """;

            var expectedCode = """
                class Program : IProgram
                {
                    public void Goo() { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_BaseList_NewBaseListGeneric()
        {
            var markup = """
                class Program<T>
                {
                    $$public void Goo(T t) { }
                }
                """;

            var expectedCode = """
                class Program<T> : IProgram<T>
                {
                    public void Goo(T t) { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_BaseList_NewBaseListWithWhereClause()
        {
            var markup = """
                class Program<T, U> where T : U
                {
                    $$public void Goo(T t, U u) { }
                }
                """;

            var expectedCode = """
                class Program<T, U> : IProgram<T, U> where T : U
                {
                    public void Goo(T t, U u) { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList1()
        {
            var markup = """
                class Program : ISomeInterface
                {
                    $$public void Goo() { }
                }

                interface ISomeInterface {}
                """;

            var expectedCode = """
                class Program : ISomeInterface, IProgram
                {
                    public void Goo() { }
                }

                interface ISomeInterface {}
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList2()
        {
            var markup = """
                class Program<T, U> : ISomeInterface<T>
                {
                    $$public void Goo(T t, U u) { }
                }

                interface ISomeInterface<T> {}
                """;

            var expectedCode = """
                class Program<T, U> : ISomeInterface<T>, IProgram<T, U>
                {
                    public void Goo(T t, U u) { }
                }

                interface ISomeInterface<T> {}
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList3()
        {
            var markup = """
                class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>
                {
                    $$public void Goo(T t, U u) { }
                }

                interface ISomeInterface<T> {}
                interface ISomeInterface2<T, U> {}
                """;

            var expectedCode = """
                class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>, IProgram<T, U>
                {
                    public void Goo(T t, U u) { }
                }

                interface ISomeInterface<T> {}
                interface ISomeInterface2<T, U> {}
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_CodeGen_BaseList_LargerBaseList4()
        {
            var markup = """
                class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U> where T : U
                {
                    $$public void Goo(T t, U u) { }
                }

                interface ISomeInterface<T> {}
                interface ISomeInterface2<T, U> {}
                """;

            var expectedCode = """
                class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>, IProgram<T, U> where T : U
                {
                    public void Goo(T t, U u) { }
                }

                interface ISomeInterface<T> {}
                interface ISomeInterface2<T, U> {}
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly1()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class Program<T, U> : ISomeInterface<T> where T : U
                {
                    $$public void Goo(T t, U u) { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly2()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class Program<T, U> $$: ISomeInterface<T> where T : U
                {
                    public void Goo(T t, U u) { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly3()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class$$ Program<T, U> : ISomeInterface<T> where T : U
                {
                    public void Goo(T t, U u) { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly4()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class Program<T, U>$$ : ISomeInterface<T> where T : U
                {
                    public void Goo(T t, U u) { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly5()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class Program  $$ <T, U> : ISomeInterface<T> where T : U
                {
                    public void Goo(T t, U u) { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly6()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class $$Program   <T, U> : ISomeInterface<T> where T : U
                {
                    public void Goo(T t, U u) { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly7()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class $$Program : ISomeInterface<object>
                {
                    public void Goo() { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly8()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class Program$$ : ISomeInterface<object>
                {
                    public void Goo() { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly9()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class$$ Program : ISomeInterface<object>
                {
                    public void Goo() { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly10()
        {
            var markup = """
                interface ISomeInterface<T> {}
                class Program $$: ISomeInterface<object>
                {
                    public void Goo() { }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_TypeDiscovery_NameOnly11()
        {
            var markup = """
                namespace N
                {
                $$    class Program
                    {
                        public void Goo() { }
                    }
                }
                """;

            await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_GeneratedNameTypeParameterSuffix1()
        {
            var markup = """
                class $$Test<T>
                {
                    public void M(T a) { }
                }
                """;

            var expectedTypeParameterSuffix = @"<T>";
            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_GeneratedNameTypeParameterSuffix2()
        {
            var markup = """
                class $$Test<T, U>
                {
                    public void M(T a) { }
                }
                """;

            var expectedTypeParameterSuffix = @"<T>";
            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task ExtractInterface_GeneratedNameTypeParameterSuffix3()
        {
            var markup = """
                class $$Test<T, U>
                {
                    public void M(T a, U b) { }
                }
                """;

            var expectedTypeParameterSuffix = @"<T, U>";
            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void ExtractInterfaceCommandDisabledInSubmission()
        {
            using var workspace = TestWorkspace.Create(XElement.Parse("""
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
                composition: EditorTestCompositions.EditorFeaturesWpf);
            // Force initialization.
            workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

            var textView = workspace.Documents.Single().GetTextView();

            var handler = workspace.ExportProvider.GetCommandHandler<ExtractInterfaceCommandHandler>(PredefinedCommandHandlerNames.ExtractInterface, ContentTypeNames.CSharpContentType);

            var state = handler.GetCommandState(new ExtractInterfaceCommandArgs(textView, textView.TextBuffer));
            Assert.True(state.IsUnspecified);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestInWithMethod_Parameters()
        {
            var markup = """
                using System;
                class $$TestClass
                {
                    public void Method(in int p1)
                    {
                    }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass
                {
                    void Method(in int p1);
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestRefReadOnlyWithMethod_ReturnType()
        {
            var markup = """
                using System;
                class $$TestClass
                {
                    public ref readonly int Method() => throw null;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass
                {
                    ref readonly int Method();
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestRefReadOnlyWithProperty()
        {
            var markup = """
                using System;
                class $$TestClass
                {
                    public ref readonly int Property => throw null;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass
                {
                    ref readonly int Property { get; }
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestInWithIndexer_Parameters()
        {
            var markup = """
                using System;
                class $$TestClass
                {
                    public int this[in int p1] { set { } }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass
                {
                    int this[in int p1] { set; }
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestRefReadOnlyWithIndexer_ReturnType()
        {
            var markup = """
                using System;
                class $$TestClass
                {
                    public ref readonly int this[int p1] => throw null;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass
                {
                    ref readonly int this[int p1] { get; }
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestUnmanagedConstraint_Type()
        {
            var markup = """
                class $$TestClass<T> where T : unmanaged
                {
                    public void M(T arg) => throw null;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass<T> where T : unmanaged
                {
                    void M(T arg);
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestUnmanagedConstraint_Method()
        {
            var markup = """
                class $$TestClass
                {
                    public void M<T>() where T : unmanaged => throw null;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass
                {
                    void M<T>() where T : unmanaged;
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestNotNullConstraint_Type()
        {
            var markup = """
                class $$TestClass<T> where T : notnull
                {
                    public void M(T arg) => throw null;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass<T> where T : notnull
                {
                    void M(T arg);
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestNotNullConstraint_Method()
        {
            var markup = """
                class $$TestClass
                {
                    public void M<T>() where T : notnull => throw null;
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(markup, expectedSuccess: true, expectedInterfaceCode:
                """
                interface ITestClass
                {
                    void M<T>() where T : notnull;
                }
                """);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/23855")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestExtractInterface_WithCopyright1()
        {
            var markup =
                """
                // Copyright

                public class $$Goo
                {
                    public void Test()
                    {
                    }
                }
                """;

            var updatedMarkup =
                """
                // Copyright

                public class Goo : IGoo
                {
                    public void Test()
                    {
                    }
                }
                """;

            var expectedInterfaceCode =
                """
                // Copyright

                public interface IGoo
                {
                    void Test();
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedUpdatedOriginalDocumentCode: updatedMarkup,
                expectedInterfaceCode: expectedInterfaceCode);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/23855")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestExtractInterface_WithCopyright2()
        {
            var markup =
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
                """;

            var updatedMarkup =
                """
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
                """;

            var expectedInterfaceCode =
                """
                // Copyright

                public interface IA
                {
                    void Test();
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedUpdatedOriginalDocumentCode: updatedMarkup,
                expectedInterfaceCode: expectedInterfaceCode);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/49739")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestRecord1()
        {
            var markup =
                """
                namespace Test
                {
                    record $$Whatever(int X, string Y);
                }
                """;

            var updatedMarkup =
                """
                namespace Test
                {
                    record Whatever(int X, string Y) : IWhatever;
                }
                """;

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
                markup,
                expectedSuccess: true,
                expectedUpdatedOriginalDocumentCode: updatedMarkup,
                expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestClass1()
        {
            var markup =
                """
                namespace Test
                {
                    class $$Whatever(int X, string Y);
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestStruct1()
        {
            var markup =
                """
                namespace Test
                {
                    struct $$Whatever(int X, string Y);
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/49739")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestRecord2()
        {
            var markup =
                """
                namespace Test
                {
                    record $$Whatever(int X, string Y) { }
                }
                """;

            var updatedMarkup =
                """
                namespace Test
                {
                    record Whatever(int X, string Y) : IWhatever { }
                }
                """;

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
                markup,
                expectedSuccess: true,
                expectedUpdatedOriginalDocumentCode: updatedMarkup,
                expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestClass2()
        {
            var markup =
                """
                namespace Test
                {
                    class $$Whatever(int X, string Y) { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestStruct2()
        {
            var markup =
                """
                namespace Test
                {
                    struct $$Whatever(int X, string Y) { }
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/49739")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestRecord3()
        {
            var markup =
                """
                namespace Test
                {
                    /// <summary></summary>
                    record $$Whatever(int X, string Y);
                }
                """;

            var updatedMarkup =
                """
                namespace Test
                {
                    /// <summary></summary>
                    record Whatever(int X, string Y) : IWhatever;
                }
                """;

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
                markup,
                expectedSuccess: true,
                expectedUpdatedOriginalDocumentCode: updatedMarkup,
                expectedInterfaceCode: expectedInterfaceCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestClass3()
        {
            var markup =
                """
                namespace Test
                {
                    /// <summary></summary>
                    class $$Whatever(int X, string Y);
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public async Task TestStruct3()
        {
            var markup =
                """
                namespace Test
                {
                    /// <summary></summary>
                    struct $$Whatever(int X, string Y);
                }
                """;

            await TestExtractInterfaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }
    }
}

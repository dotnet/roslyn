// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

[Trait(Traits.Feature, Traits.Features.Classification)]
public sealed partial class SemanticClassifierTests : AbstractCSharpClassifierTests
{
    protected override async Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(
        string code, ImmutableArray<TextSpan> spans, ParseOptions? options, TestHost testHost)
    {
        using var workspace = CreateWorkspace(code, options, testHost);
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);

        return await GetSemanticClassificationsAsync(document, spans);
    }

    [Theory, CombinatorialData]
    public Task GenericClassDeclaration(TestHost testHost)
        => TestInMethodAsync(
            className: "Class<T>",
            methodName: "M",
            @"new Class<int>();",
            testHost,
            Class("Class"));

    [Theory, CombinatorialData]
    public Task RefVar(TestHost testHost)
        => TestInMethodAsync(
            @"int i = 0; ref var x = ref i;",
            testHost,
            Classifications(Keyword("var"), Local("i")));

    [Theory, CombinatorialData]
    public Task UsingAlias1(TestHost testHost)
        => TestAsync(
@"using M = System.Math;",
            testHost,
            Class("M"),
            Namespace("System"),
            Class("Math"),
            Static("Math"));

    [Theory, CombinatorialData]
    public Task DynamicAsTypeArgument(TestHost testHost)
        => TestInMethodAsync(
            className: "Class<T>",
            methodName: "M",
            @"new Class<dynamic>();",
            testHost,
            Classifications(Class("Class"), Keyword("dynamic")));

    [Theory, CombinatorialData]
    public async Task UsingTypeAliases(TestHost testHost)
    {
        var code = """
            using Alias = Test; 
            class Test { void M() { Test a = new Test(); Alias b = new Alias(); } }
            """;

        await TestAsync(code,
            code,
            testHost,
            Class("Alias"),
            Class("Test"),
            Class("Test"),
            Class("Test"),
            Class("Alias"),
            Class("Alias"));
    }

    [Theory, CombinatorialData]
    public Task DynamicTypeAlias(TestHost testHost)
        => TestAsync(
            """
            using dynamic = System.EventArgs;

            class C
            {
                dynamic d = new dynamic();
            }
            """,
            testHost,
            Class("dynamic"),
            Namespace("System"),
            Class("EventArgs"),
            Class("dynamic"),
            Class("dynamic"));

    [Theory, CombinatorialData]
    public Task ArrayTypeAlias(TestHost testHost)
        => TestAsync(
            """
            using IntArray = int[];

            class C
            {
                void M()
                {
                    IntArray a = new int[10];
                }
            }
            """,
            testHost,
            ArrayType("IntArray"),
            ArrayType("IntArray"));

    [Theory, CombinatorialData]
    public Task PointerTypeAlias(TestHost testHost)
        => TestAsync(
            """
            using IntPointer = int*;

            class C
            {
                unsafe void M()
                {
                    IntPointer p;
                }
            }
            """,
            testHost,
            PointerType("IntPointer"),
            PointerType("IntPointer"));

    [Theory, CombinatorialData]
    public Task FunctionPointerTypeAlias(TestHost testHost)
        => TestAsync(
            """
            using MethodPtr = delegate*<int, void>;

            class C
            {
                unsafe void M()
                {
                    MethodPtr ptr;
                }
            }
            """,
            testHost,
            FunctionPointer("MethodPtr"),
            FunctionPointer("MethodPtr"));

    [Theory, CombinatorialData]
    public Task DynamicAsDelegateName(TestHost testHost)
        => TestAsync(
            """
            delegate void dynamic();

            class C
            {
                void M()
                {
                    dynamic d;
                }
            }
            """,
            testHost,
            Delegate("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsInterfaceName(TestHost testHost)
        => TestAsync(
            """
            interface dynamic
            {
            }

            class C
            {
                dynamic d;
            }
            """,
            testHost,
            Interface("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsEnumName(TestHost testHost)
        => TestAsync(
            """
            enum dynamic
            {
            }

            class C
            {
                dynamic d;
            }
            """,
            testHost,
            Enum("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsClassName(TestHost testHost)
        => TestAsync(
            """
            class dynamic
            {
            }

            class C
            {
                dynamic d;
            }
            """,
            testHost,
            Class("dynamic"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46985")]
    public Task DynamicAsRecordName(TestHost testHost)
        => TestAsync(
            """
            record dynamic
            {
            }

            class C
            {
                dynamic d;
            }
            """,
            testHost,
            RecordClass("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsClassNameAndLocalVariableName(TestHost testHost)
        => TestAsync(
            """
            class dynamic
            {
                dynamic()
                {
                    dynamic dynamic;
                }
            }
            """,
            testHost,
            Class("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsStructName(TestHost testHost)
        => TestAsync(
            """
            struct dynamic
            {
            }

            class C
            {
                dynamic d;
            }
            """,
            testHost,
            Struct("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsGenericClassName(TestHost testHost)
        => TestAsync(
            """
            class dynamic<T>
            {
            }

            class C
            {
                dynamic<int> d;
            }
            """,
            testHost,
            Class("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsGenericClassNameButOtherArity(TestHost testHost)
        => TestAsync(
            """
            class dynamic<T>
            {
            }

            class C
            {
                dynamic d;
            }
            """,
            testHost,
            Keyword("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsUndefinedGenericType(TestHost testHost)
        => TestAsync(
            """
            class dynamic
            {
            }

            class C
            {
                dynamic<int> d;
            }
            """,
            testHost,
            Class("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsExternAlias(TestHost testHost)
        => TestAsync(
            """
            extern alias dynamic;

            class C
            {
                dynamic::Goo a;
            }
            """,
            testHost,
            Namespace("dynamic"));

    [Theory, CombinatorialData]
    public Task GenericClassNameButOtherArity(TestHost testHost)
        => TestAsync(
            """
            class A<T>
            {
            }

            class C
            {
                A d;
            }
            """, testHost,
            Class("A"));

    [Theory, CombinatorialData]
    public Task GenericTypeParameter(TestHost testHost)
        => TestAsync(
            """
            class C<T>
            {
                void M()
                {
                    default(T) }
            }
            """,
            testHost,
            TypeParameter("T"));

    [Theory, CombinatorialData]
    public Task GenericMethodTypeParameter(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                T M<T>(T t)
                {
                    return default(T);
                }
            }
            """,
            testHost,
            TypeParameter("T"),
            TypeParameter("T"),
            TypeParameter("T"));

    [Theory, CombinatorialData]
    public Task GenericMethodTypeParameterInLocalVariableDeclaration(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M<T>()
                {
                    T t;
                }
            }
            """,
            testHost,
            TypeParameter("T"));

    [Theory, CombinatorialData]
    public Task ParameterOfLambda1(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                C()
                {
                    Action a = (C p) => {
                    };
                }
            }
            """,
            testHost,
            Class("C"));

    [Theory, CombinatorialData]
    public Task ParameterOfAnonymousMethod(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                C()
                {
                    Action a = delegate (C p) {
                    };
                }
            }
            """,
            testHost,
            Class("C"));

    [Theory, CombinatorialData]
    public Task GenericTypeParameterAfterWhere(TestHost testHost)
        => TestAsync(
            """
            class C<A, B> where A : B
            {
            }
            """,
            testHost,
            TypeParameter("A"),
            TypeParameter("B"));

    [Theory, CombinatorialData]
    public Task BaseClass(TestHost testHost)
        => TestAsync(
            """
            class C
            {
            }

            class C2 : C
            {
            }
            """,
            testHost,
            Class("C"));

    [Theory, CombinatorialData]
    public Task BaseInterfaceOnInterface(TestHost testHost)
        => TestAsync(
            """
            interface T
            {
            }

            interface T2 : T
            {
            }
            """,
            testHost,
            Interface("T"));

    [Theory, CombinatorialData]
    public Task BaseInterfaceOnClass(TestHost testHost)
        => TestAsync(
            """
            interface T
            {
            }

            class T2 : T
            {
            }
            """,
            testHost,
            Interface("T"));

    [Theory, CombinatorialData]
    public Task InterfaceColorColor(TestHost testHost)
        => TestAsync(
            """
            interface T
            {
            }

            class T2 : T
            {
                T T;
            }
            """,
            testHost,
            Interface("T"),
            Interface("T"));

    [Theory, CombinatorialData]
    public Task DelegateColorColor(TestHost testHost)
        => TestAsync(
            """
            delegate void T();

            class T2
            {
                T T;
            }
            """,
            testHost,
            Delegate("T"));

    [Theory, CombinatorialData]
    public Task DelegateReturnsItself(TestHost testHost)
        => TestAsync(
            """
            delegate T T();

            class C
            {
                T T(T t);
            }
            """,
            testHost,
            Delegate("T"),
            Delegate("T"),
            Delegate("T"));

    [Theory, CombinatorialData]
    public Task StructColorColor(TestHost testHost)
        => TestAsync(
            """
            struct T
            {
                T T;
            }
            """,
            testHost,
            Struct("T"));

    [Theory, CombinatorialData]
    public Task EnumColorColor(TestHost testHost)
        => TestAsync(
            """
            enum T
            {
                T,
                T
            }

            class C
            {
                T T;
            }
            """,
            testHost,
            Enum("T"));

    [Theory, CombinatorialData]
    public Task DynamicAsGenericTypeParameter(TestHost testHost)
        => TestAsync(
            """
            class C<dynamic>
            {
                dynamic d;
            }
            """,
            testHost,
            TypeParameter("dynamic"));

    [Theory, CombinatorialData]
    public Task DynamicAsGenericFieldName(TestHost testHost)
        => TestAsync(
            """
            class A<T>
            {
                T dynamic;
            }
            """,
            testHost,
            TypeParameter("T"));

    [Theory, CombinatorialData]
    public Task PropertySameNameAsClass(TestHost testHost)
        => TestAsync(
            """
            class N
            {
                N N { get; set; }

                void M()
                {
                    N n = N;
                    N = n;
                    N = N;
                }
            }
            """,
            testHost,
            Class("N"),
            Class("N"),
            Property("N"),
            Property("N"),
            Local("n"),
            Property("N"),
            Property("N"));

    [Theory, CombinatorialData]
    public Task AttributeWithoutAttributeSuffix(TestHost testHost)
        => TestAsync(
            """
            using System;

            [Obsolete]
            class C
            {
            }
            """,
            testHost,
            Namespace("System"),
            Class("Obsolete"),
            Obsolete("C"));

    [Theory, CombinatorialData]
    public Task AttributeOnNonExistingMember(TestHost testHost)
        => TestAsync(
            """
            using System;

            class A
            {
                [Obsolete]
            }
            """,
            testHost,
            Namespace("System"),
            Class("Obsolete"));

    [Theory, CombinatorialData]
    public Task AttributeWithoutAttributeSuffixOnAssembly(TestHost testHost)
        => TestAsync(
            """
            using System;

            [assembly: My]

            class MyAttribute : Attribute
            {
            }
            """,
            testHost,
            Namespace("System"),
            Class("My"),
            Class("Attribute"));

    [Theory, CombinatorialData]
    public Task AttributeViaNestedClassOrDerivedClass(TestHost testHost)
        => TestAsync(
            """
            using System;

            [Base.My]
            [Derived.My]
            class Base
            {
                public class MyAttribute : Attribute
                {
                }
            }

            class Derived : Base
            {
            }
            """,
            testHost,
            Namespace("System"),
            Class("Base"),
            Class("My"),
            Class("Derived"),
            Class("My"),
            Class("Attribute"),
            Class("Base"));

    [Theory, CombinatorialData]
    public Task NamedAndOptional(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void B(C C = null)
                {
                }

                void M()
                {
                    B(C: null);
                }
            }
            """,
            testHost,
            Class("C"),
            Method("B"),
            Parameter("C"));

    [Theory, CombinatorialData]
    public Task PartiallyWrittenGenericName1(TestHost testHost)
        => TestInMethodAsync(
            className: "Class<T>",
            methodName: "M",
            @"Class<int",
            testHost,
            Class("Class"));

    [Theory, CombinatorialData]
    public Task PartiallyWrittenGenericName2(TestHost testHost)
        => TestInMethodAsync(
            className: "Class<T1, T2>",
            methodName: "M",
            @"Class<int, b",
            testHost,
            Class("Class"));

    // The "Color Color" problem is the C# IDE folklore for when
    // a property name is the same as a type name
    // and the resulting ambiguities that the spec
    // resolves in favor of properties
    [Theory, CombinatorialData]
    public Task ColorColor(TestHost testHost)
        => TestAsync(
            """
            class Color
            {
                Color Color;
            }
            """,
            testHost,
            Class("Color"));

    [Theory, CombinatorialData]
    public Task ColorColor2(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                T T = new T();

                T()
                {
                    this.T = new T();
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Field("T"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task ColorColor3(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                T T = new T();

                void M();

                T()
                {
                    T.M();
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Field("T"),
            Method("M"));

    /// <summary>
    /// Instance field should be preferred to type
    /// §7.5.4.1
    /// </summary>
    [Theory, CombinatorialData]
    public Task ColorColor4(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                T T;

                void M()
                {
                    T.T = null;
                }
            }
            """,
            testHost,
            Class("T"),
            Field("T"),
            Field("T"));

    /// <summary>
    /// Type should be preferred to a static field
    /// §7.5.4.1
    /// </summary>
    [Theory, CombinatorialData]
    public Task ColorColor5(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                static T T;

                void M()
                {
                    T.T = null;
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Field("T"),
            Static("T"));

    /// <summary>
    /// Needs to prefer the local
    /// </summary>
    [Theory, CombinatorialData]
    public Task ColorColor6(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                int field;

                void M()
                {
                    T T = new T();
                    T.field = 0;
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Local("T"),
            Field("field"));

    /// <summary>
    /// Needs to prefer the type
    /// </summary>
    [Theory, CombinatorialData]
    public Task ColorColor7(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                static int field;

                void M()
                {
                    T T = new T();
                    T.field = 0;
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Class("T"),
            Field("field"),
            Static("field"));

    [Theory, CombinatorialData]
    public Task ColorColor8(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                void M(T T)
                {
                }

                void M2()
                {
                    T T = new T();
                    M(T);
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Class("T"),
            Method("M"),
            Local("T"));

    [Theory, CombinatorialData]
    public Task ColorColor9(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                T M(T T)
                {
                    T = new T();
                    return T;
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Parameter("T"),
            Class("T"),
            Parameter("T"));

    [Theory, CombinatorialData]
    public Task ColorColor10(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                void M()
                {
                    var T = new object();
                    T temp = T as T;
                }
            }
            """,
            testHost,
            Keyword("var"),
            Class("T"),
            Local("T"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task ColorColor11(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                void M()
                {
                    var T = new object();
                    bool b = T is T;
                }
            }
            """,
            testHost,
            Keyword("var"),
            Local("T"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task ColorColor12(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                void M()
                {
                    T T = new T();
                    var t = typeof(T);
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Keyword("var"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task ColorColor13(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                void M()
                {
                    T T = new T();
                    T t = default(T);
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Class("T"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task ColorColor14(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                void M()
                {
                    object T = new T();
                    T t = (T)T;
                }
            }
            """,
            testHost,
            Class("T"),
            Class("T"),
            Class("T"),
            Local("T"));

    [Theory, CombinatorialData]
    public Task NamespaceNameSameAsTypeName1(TestHost testHost)
        => TestAsync(
            """
            namespace T
            {
                class T
                {
                    void M()
                    {
                        T.T T = new T.T();
                    }
                }
            }
            """,
            testHost,
            Namespace("T"),
            Class("T"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task NamespaceNameSameAsTypeNameWithGlobal(TestHost testHost)
        => TestAsync(
            """
            namespace T
            {
                class T
                {
                    void M()
                    {
                        global::T.T T = new global::T.T();
                    }
                }
            }
            """,
            testHost,
            Namespace("T"),
            Namespace("T"),
            Class("T"),
            Namespace("T"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task AmbiguityTypeAsGenericMethodArgumentVsLocal(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                void M<T>()
                {
                    T T;
                    M<T>();
                }
            }
            """,
            testHost,
            TypeParameter("T"),
            Method("M"),
            TypeParameter("T"));

    [Theory, CombinatorialData]
    public Task AmbiguityTypeAsGenericArgumentVsLocal(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                class G<T>
                {
                }

                void M()
                {
                    T T;
                    G<T> g = new G<T>();
                }
            }
            """,
            testHost,
            Class("T"),
            Class("G"),
            Class("T"),
            Class("G"),
            Class("T"));

    [Theory, CombinatorialData]
    public Task AmbiguityTypeAsGenericArgumentVsField(TestHost testHost)
        => TestAsync(
            """
            class T
            {
                class H<T>
                {
                    public static int f;
                }

                void M()
                {
                    T T;
                    int i = H<T>.f;
                }
            }
            """,
            testHost,
            Class("T"),
            Class("H"),
            Class("T"),
            Field("f"),
            Static("f"));

    /// <summary>
    /// §7.5.4.2
    /// </summary>
    [Theory, CombinatorialData]
    public Task GrammarAmbiguity_7_5_4_2(TestHost testHost)
        => TestAsync(
            """
            class M
            {
                void m()
                {
                    int A = 2;
                    int B = 3;
                    F(G<A, B>(7));
                }

                void F(bool b)
                {
                }

                bool G<t, f>(int a)
                {
                    return true;
                }

                class A
                {
                }

                class B
                {
                }
            }
            """,
            testHost,
            Method("F"),
            Method("G"),
            Class("A"),
            Class("B"));

    [Theory, CombinatorialData]
    public Task AnonymousTypePropertyName(TestHost testHost)
        => TestAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var x = new { String = " }; } }
            """,
            testHost,
            Namespace("System"),
            Keyword("var"),
            Property("String"));

    [Theory, CombinatorialData]
    public Task YieldAsATypeName(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class yield
            {
                IEnumerable<yield> M()
                {
                    yield yield = new yield();
                    yield return yield;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Collections"),
            Namespace("Generic"),
            Interface("IEnumerable"),
            Class("yield"),
            Class("yield"),
            Class("yield"),
            Local("yield"));

    [Theory, CombinatorialData]
    public Task TypeNameDottedNames(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                class Nested
                {
                }

                C.Nested f;
            }
            """,
            testHost,
            Class("C"),
            Class("Nested"));

    [Theory, CombinatorialData]
    public Task BindingTypeNameFromBCLViaGlobalAlias(TestHost testHost)
        => TestAsync(
            """
            using System;

            class C
            {
                global::System.String f;
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("System"),
            Class("String"));

    [Theory, CombinatorialData]
    public async Task BindingTypeNames(TestHost testHost)
    {
        var code = """
            using System;
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
            }
            """;
        await TestAsync(code,
            code,
            testHost,
            Options.Regular,
            Namespace("System"),
            Class("Str"),
            Namespace("System"),
            Class("String"),
            Class("Str"),
            Class("Nested"),
            Class("String"),
            Class("C"),
            Class("C"),
            Class("Nested"),
            Class("C"),
            Namespace("System"),
            Class("String"));
    }

    [Theory, CombinatorialData]
    public Task Constructors(TestHost testHost)
        => TestAsync(
            """
            struct S
            {
                public int i;

                public S(int i)
                {
                    this.i = i;
                }
            }

            class C
            {
                public C()
                {
                    var s = new S(1);
                    var c = new C();
                }
            }
            """,
            testHost,
            Field("i"),
            Parameter("i"),
            Keyword("var"),
            Struct("S"),
            Keyword("var"),
            Class("C"));

    [Theory, CombinatorialData]
    public Task TypesOfClassMembers(TestHost testHost)
        => TestAsync(
            """
            class Type
            {
                public Type()
                {
                }

                static Type()
                {
                }

                ~Type()
                {
                }

                Type Property { get; set; }

                Type Method()
                {
                }

                event Type Event;

                Type this[Type index] { get; set; }

                Type field;
                const Type constant = null;

                static operator Type(Type other)
                {
                }

                static operator +(Type other)
                {
                }

                static operator int(Type other)
                {
                }

                static operator Type(int other)
                {
                }
            }
            """,
            testHost,
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

    /// <summary>
    /// NAQ = Namespace Alias Qualifier (?)
    /// </summary>
    [Theory, CombinatorialData]
    public Task NAQTypeNameCtor(TestHost testHost)
        => TestInMethodAsync(
@"System.IO.BufferedStream b = new global::System.IO.BufferedStream();",
            testHost,
            Namespace("System"),
            Namespace("IO"),
            Class("BufferedStream"),
            Namespace("System"),
            Namespace("IO"),
            Class("BufferedStream"));

    [Theory, CombinatorialData]
    public Task NAQEnum(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    global::System.IO.DriveType d;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("IO"),
            Enum("DriveType"));

    [Theory, CombinatorialData]
    public Task NAQDelegate(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    global::System.AssemblyLoadEventHandler d;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Delegate("AssemblyLoadEventHandler"));

    [Theory, CombinatorialData]
    public Task NAQTypeNameMethodCall(TestHost testHost)
        => TestInMethodAsync(@"global::System.String.Clone("");",
            testHost,
            Namespace("System"),
            Class("String"),
            Method("Clone"));

    [Theory, CombinatorialData]
    public Task NAQEventSubscription(TestHost testHost)
        => TestInMethodAsync(
            """
            global::System.AppDomain.CurrentDomain.AssemblyLoad += 
                        delegate (object sender, System.AssemblyLoadEventArgs args) {};
            """,
            testHost,
            Namespace("System"),
            Class("AppDomain"),
            Property("CurrentDomain"),
            Static("CurrentDomain"),
            Event("AssemblyLoad"),
            Namespace("System"),
            Class("AssemblyLoadEventArgs"));

    [Theory, CombinatorialData]
    public Task AnonymousDelegateParameterType(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    System.Action<System.EventArgs> a = delegate (System.EventArgs e) {
                    };
                }
            }
            """,
            testHost,
            Namespace("System"),
            Delegate("Action"),
            Namespace("System"),
            Class("EventArgs"),
            Namespace("System"),
            Class("EventArgs"));

    [Theory, CombinatorialData]
    public Task NAQCtor(TestHost testHost)
        => TestInMethodAsync(
@"global::System.Collections.DictionaryEntry de = new global::System.Collections.DictionaryEntry();",
            testHost,
            Namespace("System"),
            Namespace("Collections"),
            Struct("DictionaryEntry"),
            Namespace("System"),
            Namespace("Collections"),
            Struct("DictionaryEntry"));

    [Theory, CombinatorialData]
    public Task NAQSameFileClass(TestHost testHost)
        => TestAsync(@"class C { static void M() { global::C.M(); } }",
            testHost,
            ParseOptions(Options.Regular),
            Class("C"),
            Method("M"),
            Static("M"));

    [Theory, CombinatorialData]
    public Task InteractiveNAQSameFileClass(TestHost testHost)
        => TestAsync(@"class C { static void M() { global::Script.C.M(); } }",
            testHost,
            ParseOptions(Options.Script),
            Class("Script"),
            Class("C"),
            Method("M"),
            Static("M"));

    [Theory, CombinatorialData]
    public Task NAQSameFileClassWithNamespace(TestHost testHost)
        => TestAsync(
            """
            using @global = N;

            namespace N
            {
                class C
                {
                    static void M()
                    {
                        global::N.C.M();
                    }
                }
            }
            """,
            testHost,
            Namespace("@global"),
            Namespace("N"),
            Namespace("N"),
            Namespace("N"),
            Class("C"),
            Method("M"),
            Static("M"));

    [Theory, CombinatorialData]
    public Task NAQSameFileClassWithNamespaceAndEscapedKeyword(TestHost testHost)
        => TestAsync(
            """
            using @global = N;

            namespace N
            {
                class C
                {
                    static void M()
                    {
                        @global.C.M();
                    }
                }
            }
            """,
            testHost,
            Namespace("@global"),
            Namespace("N"),
            Namespace("N"),
            Namespace("@global"),
            Class("C"),
            Method("M"),
            Static("M"));

    [Theory, CombinatorialData]
    public Task NAQGlobalWarning(TestHost testHost)
        => TestAsync(
            """
            using global = N;

            namespace N
            {
                class C
                {
                    static void M()
                    {
                        global.C.M();
                    }
                }
            }
            """,
            testHost,
            Namespace("global"),
            Namespace("N"),
            Namespace("N"),
            Namespace("global"),
            Class("C"),
            Method("M"),
            Static("M"));

    [Theory, CombinatorialData]
    public Task NAQUserDefinedNAQNamespace(TestHost testHost)
        => TestAsync(
            """
            using goo = N;

            namespace N
            {
                class C
                {
                    static void M()
                    {
                        goo.C.M();
                    }
                }
            }
            """,
            testHost,
            Namespace("goo"),
            Namespace("N"),
            Namespace("N"),
            Namespace("goo"),
            Class("C"),
            Method("M"),
            Static("M"));

    [Theory, CombinatorialData]
    public Task NAQUserDefinedNAQNamespaceDoubleColon(TestHost testHost)
        => TestAsync(
            """
            using goo = N;

            namespace N
            {
                class C
                {
                    static void M()
                    {
                        goo::C.M();
                    }
                }
            }
            """,
            testHost,
            Namespace("goo"),
            Namespace("N"),
            Namespace("N"),
            Namespace("goo"),
            Class("C"),
            Method("M"),
            Static("M"));

    [Theory, CombinatorialData]
    public Task NAQUserDefinedNamespace1(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    A.B.D d;
                }
            }

            namespace A
            {
                namespace B
                {
                    class D
                    {
                    }
                }
            }
            """,
            testHost,
            Namespace("A"),
            Namespace("B"),
            Class("D"),
            Namespace("A"),
            Namespace("B"));

    [Theory, CombinatorialData]
    public Task NAQUserDefinedNamespaceWithGlobal(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    global::A.B.D d;
                }
            }

            namespace A
            {
                namespace B
                {
                    class D
                    {
                    }
                }
            }
            """,
            testHost,
            Namespace("A"),
            Namespace("B"),
            Class("D"),
            Namespace("A"),
            Namespace("B"));

    [Theory, CombinatorialData]
    public Task NAQUserDefinedNAQForClass(TestHost testHost)
        => TestAsync(
            """
            using IO = global::System.IO;

            class C
            {
                void M()
                {
                    IO::BinaryReader b;
                }
            }
            """,
            testHost,
            Namespace("IO"),
            Namespace("System"),
            Namespace("IO"),
            Namespace("IO"),
            Class("BinaryReader"));

    [Theory, CombinatorialData]
    public Task NAQUserDefinedTypes(TestHost testHost)
        => TestAsync(
            """
            using rabbit = MyNameSpace;

            class C
            {
                void M()
                {
                    rabbit::MyClass2.method();
                    new rabbit::MyClass2().myEvent += null;
                    rabbit::MyEnum Enum;
                    rabbit::MyStruct strUct;
                    object o2 = rabbit::MyClass2.MyProp;
                    object o3 = rabbit::MyClass2.myField;
                    rabbit::MyClass2.MyDelegate del = null;
                }
            }

            namespace MyNameSpace
            {
                namespace OtherNamespace
                {
                    class A
                    {
                    }
                }

                public class MyClass2
                {
                    public static int myField;

                    public delegate void MyDelegate();

                    public event MyDelegate myEvent;

                    public static void method()
                    {
                    }

                    public static int MyProp
                    {
                        get
                        {
                            return 0;
                        }
                    }
                }

                struct MyStruct
                {
                }

                enum MyEnum
                {
                }
            }
            """,
            testHost,
            Namespace("rabbit"),
            Namespace("MyNameSpace"),
            Namespace("rabbit"),
            Class("MyClass2"),
            Method("method"),
            Static("method"),
            Namespace("rabbit"),
            Class("MyClass2"),
            Event("myEvent"),
            Namespace("rabbit"),
            Enum("MyEnum"),
            Namespace("rabbit"),
            Struct("MyStruct"),
            Namespace("rabbit"),
            Class("MyClass2"),
            Property("MyProp"),
            Static("MyProp"),
            Namespace("rabbit"),
            Class("MyClass2"),
            Field("myField"),
            Static("myField"),
            Namespace("rabbit"),
            Class("MyClass2"),
            Delegate("MyDelegate"),
            Namespace("MyNameSpace"),
            Namespace("OtherNamespace"),
            Delegate("MyDelegate"));

    [Theory, CombinatorialData]
    public Task PreferPropertyOverNestedClass(TestHost testHost)
        => TestAsync(
            """
            class Outer
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
            }
            """,
            testHost,
            Class("A"),
            Class("A"),
            Local("a"),
            Field("B"));

    [Theory, CombinatorialData]
    public Task TypeNameInsideNestedClass(TestHost testHost)
        => TestAsync(
            """
            using System;

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
            }
            """,
            testHost,
            Namespace("System"),
            Class("Console"),
            Static("Console"),
            Method("WriteLine"),
            Static("WriteLine"),
            Class("Console"),
            Static("Console"),
            Method("WriteLine"),
            Static("WriteLine"));

    [Theory, CombinatorialData]
    public Task StructEnumTypeNames(TestHost testHost)
        => TestAsync(
            """
            using System;

            class C
            {
                enum MyEnum
                {
                }

                struct MyStruct
                {
                }

                static void Main()
                {
                    ConsoleColor c;
                    Int32 i;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Enum("ConsoleColor"),
            Struct("Int32"));

    [Theory, CombinatorialData]
    public Task PreferFieldOverClassWithSameName(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                public int C;

                void M()
                {
                    C = 0;
                }
            }
            """, testHost,
            Field("C"));

    [Theory, CombinatorialData]
    public Task AttributeBinding(TestHost testHost)
        => TestAsync(
            """
            using System;

            [Serializable]            // Binds to System.SerializableAttribute; colorized
            class Serializable
            {
            }

            [SerializableAttribute]   // Binds to System.SerializableAttribute; colorized
            class Serializable
            {
            }

            [NonSerialized]           // Binds to global::NonSerializedAttribute; colorized
            class NonSerializedAttribute
            {
            }

            [NonSerializedAttribute]  // Binds to global::NonSerializedAttribute; colorized
            class NonSerializedAttribute
            {
            }

            [Obsolete]                // Binds to global::Obsolete; colorized
            class Obsolete : Attribute
            {
            }

            [ObsoleteAttribute]       // Binds to global::Obsolete; colorized
            class ObsoleteAttribute : Attribute
            {
            }
            """,
            testHost,
            Namespace("System"),
            Class("Serializable"),
            Class("SerializableAttribute"),
            Class("NonSerialized"),
            Class("NonSerializedAttribute"),
            Class("Obsolete"),
            Class("Attribute"),
            Class("ObsoleteAttribute"),
            Class("Attribute"));

    [Theory, CombinatorialData]
    public Task ShouldNotClassifyNamespacesAsTypes(TestHost testHost)
        => TestAsync(
            """
            using System;

            namespace Roslyn.Compilers.Internal
            {
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Roslyn"),
            Namespace("Compilers"),
            Namespace("Internal"));

    [Theory, CombinatorialData]
    public Task NestedTypeCantHaveSameNameAsParentType(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                class Program
                {
                }

                static void Main(Program p)
                {
                }

                Program.Program p2;
            }
            """,
            testHost,
            Class("Program"),
            Class("Program"));

    [Theory, CombinatorialData]
    public Task NestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias(TestHost testHost)
        => TestAsync("""
            class Program
            {
                class Program { }
                static void Main(Program p) { }
                global::Program.Program p;
            }
            """,
            testHost,
            ParseOptions(Options.Regular),
            Class("Program"),
            Class("Program"),
            Class("Program"));

    [Theory, CombinatorialData]
    public Task InteractiveNestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias(TestHost testHost)
        => TestAsync("""
            class Program
            {
                class Program { }
                static void Main(Program p) { }
                global::Script.Program.Program p;
            }
            """,
            testHost,
            ParseOptions(Options.Script),
            Class("Program"),
            Class("Script"),
            Class("Program"),
            Class("Program"));

    [Theory, CombinatorialData]
    public Task EnumFieldWithSameNameShouldBePreferredToType(TestHost testHost)
        => TestAsync(
            """
            enum E
            {
                E,
                F = E
            }
            """, testHost,
            EnumMember("E"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541150")]
    [CombinatorialData]
    public Task TestGenericVarClassification(TestHost testHost)
        => TestAsync(
            """
            using System;

            static class Program
            {
                static void Main()
                {
                    var x = 1;
                }
            }

            class var<T>
            {
            }
            """,
            testHost,
            Namespace("System"),
            Keyword("var"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
    [CombinatorialData]
    public Task TestInaccessibleVarClassification(TestHost testHost)
        => TestAsync(
            """
            using System;

            class A
            {
                private class var
                {
                }
            }

            class B : A
            {
                static void Main()
                {
                    var x = 1;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Class("A"),
            Keyword("var"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
    [CombinatorialData]
    public Task TestVarNamedTypeClassification(TestHost testHost)
        => TestAsync(
            """
            class var
            {
                static void Main()
                {
                    var x;
                }
            }
            """,
            testHost,
            Keyword("var"));

    [Theory, WorkItem(9513, "DevDiv_Projects/Roslyn")]
    [CombinatorialData]
    public Task RegressionFor9513(TestHost testHost)
        => TestAsync(
            """
            enum E
            {
                A,
                B
            }

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
            """,
            testHost,
            Enum("E"),
            Enum("E"),
            EnumMember("A"),
            Enum("E"),
            EnumMember("B"),
            Enum("E"),
            EnumMember("B"),
            Enum("E"),
            EnumMember("A"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542368")]
    [CombinatorialData]
    public Task RegressionFor9572(TestHost testHost)
        => TestAsync(
            """
            class A<T, S> where T : A<T, S>.I, A<T, T>.I
            {
                public interface I
                {
                }
            }
            """,
            testHost,
            TypeParameter("T"),
            Class("A"),
            TypeParameter("T"),
            TypeParameter("S"),
            Interface("I"),
            Class("A"),
            TypeParameter("T"),
            TypeParameter("T"),
            Interface("I"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542368")]
    [CombinatorialData]
    public Task RegressionFor9831(TestHost testHost)
        => TestAsync(@"F : A",
            """
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
            """,
            testHost,
            Class("A"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542432")]
    [CombinatorialData]
    public Task TestVar(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                class var<T>
                {
                }

                static var<int> GetVarT()
                {
                    return null;
                }

                static void Main()
                {
                    var x = GetVarT();
                    var y = new var<int>();
                }
            }
            """,
            testHost,
            Class("var"),
            Keyword("var"),
            Method("GetVarT"),
            Static("GetVarT"),
            Keyword("var"),
            Class("var"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
    [CombinatorialData]
    public Task TestVar2(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                void Main(string[] args)
                {
                    foreach (var v in args)
                    {
                    }
                }
            }
            """,
            testHost,
            Keyword("var"),
            Parameter("args"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542778")]
    [CombinatorialData]
    public Task TestDuplicateTypeParamWithConstraint(TestHost testHost)
        => TestAsync(@"where U : IEnumerable<S>",
            """
            using System.Collections.Generic;

            class C<T>
            {
                public void Goo<U, U>(U arg)
                    where S : T
                    where U : IEnumerable<S>
                {
                }
            }
            """,
            testHost,
            TypeParameter("U"),
            Interface("IEnumerable"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
    [CombinatorialData]
    public Task OptimisticallyColorFromInDeclaration(TestHost testHost)
        => TestInExpressionAsync("from ",
            testHost,
            Keyword("from"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
    [CombinatorialData]
    public Task OptimisticallyColorFromInAssignment(TestHost testHost)
        => TestInMethodAsync(
            """
            var q = 3;

            q = from
            """,
            testHost,
            Keyword("var"),
            Local("q"),
            Keyword("from"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
    [CombinatorialData]
    public async Task DoNotColorThingsOtherThanFromInDeclaration(TestHost testHost)
        => await TestInExpressionAsync("fro ", testHost);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
    [CombinatorialData]
    public Task DoNotColorThingsOtherThanFromInAssignment(TestHost testHost)
        => TestInMethodAsync(
            """
            var q = 3;

            q = fro
            """,
            testHost,
            Keyword("var"),
            Local("q"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
    [CombinatorialData]
    public Task DoNotColorFromWhenBoundInDeclaration(TestHost testHost)
        => TestInMethodAsync(
            """
            var from = 3;
            var q = from
            """,
            testHost,
            Keyword("var"),
            Keyword("var"),
            Local("from"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
    [CombinatorialData]
    public Task DoNotColorFromWhenBoundInAssignment(TestHost testHost)
        => TestInMethodAsync(
            """
            var q = 3;
            var from = 3;

            q = from
            """,
            testHost,
            Keyword("var"),
            Keyword("var"),
            Local("q"),
            Local("from"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543404")]
    [CombinatorialData]
    public Task NewOfClassWithOnlyPrivateConstructor(TestHost testHost)
        => TestAsync(
            """
            class X
            {
                private X()
                {
                }
            }

            class Program
            {
                static void Main(string[] args)
                {
                    new X();
                }
            }
            """,
            testHost,
            Class("X"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544179")]
    [CombinatorialData]
    public Task TestNullableVersusConditionalAmbiguity1(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    C1 ?
                }
            }

            public class C1
            {
            }
            """,
            testHost,
            Class("C1"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544179")]
    [CombinatorialData]
    public Task TestPointerVersusMultiplyAmbiguity1(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    C1 *
                }
            }

            public class C1
            {
            }
            """,
            testHost,
            Class("C1"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544302")]
    [CombinatorialData]
    public Task EnumTypeAssignedToNamedPropertyOfSameNameInAttributeCtor(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Runtime.InteropServices;

            class C
            {
                [DllImport("abc", CallingConvention = CallingConvention)]
                static extern void M();
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("System"),
            Namespace("Runtime"),
            Namespace("InteropServices"),
            Class("DllImport"),
            Field("CallingConvention"),
            Enum("CallingConvention"));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531119")]
    [CombinatorialData]
    public Task OnlyClassifyGenericNameOnce(TestHost testHost)
        => TestAsync(
            """
            enum Type
            {
            }

            struct Type<T>
            {
                Type<int> f;
            }
            """,
            testHost,
            Struct("Type"));

    [Theory, CombinatorialData]
    public Task NameOf1(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void goo()
                {
                    var x = nameof
                }
            }
            """,
            testHost,
            Keyword("var"),
            Keyword("nameof"));

    [Theory, CombinatorialData]
    public Task NameOf2(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void goo()
                {
                    var x = nameof(C);
                }
            }
            """,
            testHost,
            Keyword("var"),
            Keyword("nameof"),
            Class("C"));

    [Theory, CombinatorialData]
    public Task NameOfLocalMethod(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void goo()
                {
                    var x = nameof(M);

                    void M()
                    {
                    }

                    void M(int a)
                    {
                    }

                    void M(string s)
                    {
                    }
                }
            }
            """,
            testHost,
            Keyword("var"),
            Keyword("nameof"),
            Method("M"));

    [Theory, CombinatorialData]
    public Task MethodCalledNameOfInScope(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void nameof(int i)
                {
                }

                void goo()
                {
                    int y = 3;
                    var x = nameof();
                }
            }
            """,
            testHost,
            Keyword("var"),
            Method("nameof"));

    [Theory, CombinatorialData]
    public Task Tuples(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                (int a, int b) x;
            }
            """,
            testHost,
            ParseOptions(TestOptions.Regular, Options.Script));

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/261049")]
    public Task DevDiv261049RegressionTest(TestHost testHost)
        => TestInMethodAsync(
            """
            var (a,b) =  Get(out int x, out int y);
            Console.WriteLine($"({a.first}, {a.second})");
            """,
            testHost,
            Keyword("var"), Local("a"), Local("a"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/633")]
    public Task InXmlDocCref_WhenTypeOnlyIsSpecified_ItIsClassified(TestHost testHost)
        => TestAsync(
            """
            /// <summary>
            /// <see cref="MyClass"/>
            /// </summary>
            class MyClass
            {
                public MyClass(int x)
                {
                }
            }
            """,
            testHost,
            Class("MyClass"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/633")]
    public Task InXmlDocCref_WhenConstructorOnlyIsSpecified_NothingIsClassified(TestHost testHost)
        => TestAsync(
            """
            /// <summary>
            /// <see cref="MyClass(int)"/>
            /// </summary>
            class MyClass
            {
                public MyClass(int x)
                {
                }
            }
            """, testHost,
            Class("MyClass"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/633")]
    public Task InXmlDocCref_WhenTypeAndConstructorSpecified_OnlyTypeIsClassified(TestHost testHost)
        => TestAsync(
            """
            /// <summary>
            /// <see cref="MyClass.MyClass(int)"/>
            /// </summary>
            class MyClass
            {
                public MyClass(int x)
                {
                }
            }
            """,
            testHost,
            Class("MyClass"),
            Class("MyClass"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13174")]
    public Task TestMemberBindingThatLooksGeneric(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics;
            using System.Threading.Tasks;

            namespace ConsoleApplication1
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Debug.Assert(args?.Length < 2);
                    }
                }
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Diagnostics"),
            Namespace("System"),
            Namespace("Threading"),
            Namespace("Tasks"),
            Namespace("ConsoleApplication1"),
            Class("Debug"),
            Static("Debug"),
            Method("Assert"),
            Static("Assert"),
            Parameter("args"),
            Property("Length"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23940")]
    public Task TestAliasQualifiedClass(TestHost testHost)
        => TestAsync(
            """
            using System;
            using Col = System.Collections.Generic;

            namespace AliasTest
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var list1 = new Col::List
                    }
                }
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Col"),
            Namespace("System"),
            Namespace("Collections"),
            Namespace("Generic"),
            Namespace("AliasTest"),
            Keyword("var"),
            Namespace("Col"),
            Class("List"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_InsideMethod(TestHost testHost)
        => TestInMethodAsync("""
            var unmanaged = 0;
            unmanaged++;
            """,
            testHost,
            Keyword("var"),
            Local("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_Keyword(TestHost testHost)
        => TestAsync(
            "class X<T> where T : unmanaged { }",
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface unmanaged {}
            class X<T> where T : unmanaged { }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface unmanaged {}
            }
            class X<T> where T : unmanaged { }
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Method_Keyword(TestHost testHost)
        => TestAsync("""
            class X
            {
                void M<T>() where T : unmanaged { }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Method_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface unmanaged {}
            class X
            {
                void M<T>() where T : unmanaged { }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Method_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface unmanaged {}
            }
            class X
            {
                void M<T>() where T : unmanaged { }
            }
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_Keyword(TestHost testHost)
        => TestAsync(
            "delegate void D<T>() where T : unmanaged;",
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface unmanaged {}
            delegate void D<T>() where T : unmanaged;
            """,
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface unmanaged {}
            }
            delegate void D<T>() where T : unmanaged;
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_LocalFunction_Keyword(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    void M<T>() where T : unmanaged { }
                }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_LocalFunction_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface unmanaged {}
            class X
            {
                void N()
                {
                    void M<T>() where T : unmanaged { }
                }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_LocalFunction_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface unmanaged {}
            }
            class X
            {
                void N()
                {
                    void M<T>() where T : unmanaged { }
                }
            }
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("unmanaged"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/29451")]
    [CombinatorialData]
    public async Task TestDirectiveStringLiteral(TestHost testHost)
        => await TestInMethodAsync("""
            #line 1 "a\b"
            """, testHost);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/30378")]
    [CombinatorialData]
    public Task TestFormatSpecifierInInterpolation(TestHost testHost)
        => TestInMethodAsync(@"var goo = $""goo{{1:0000}}bar"";",
            testHost,
            Keyword("var"),
            Escape(@"{{"),
            Escape(@"}}"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/29492")]
    [CombinatorialData]
    public Task TestOverloadedOperator_BinaryExpression(TestHost testHost)
        => TestAsync("""
            class C
            {
                void M()
                {
                    var a = 1 + 1;
                    var b = new True() + new True();
                }
            }
            class True
            {
                public static True operator +(True a, True b)
                {
                     return new True();
                }
            }
            """,
            testHost,
            Keyword("var"),
            Keyword("var"),
            Class("True"),
            OverloadedOperators.Plus,
            Class("True"),
            Class("True"),
            Class("True"),
            Class("True"),
            Class("True"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/29492")]
    [CombinatorialData]
    public Task TestOverloadedOperator_PrefixUnaryExpression(TestHost testHost)
        => TestAsync("""
            class C
            {
                void M()
                {
                    var a = !false;
                    var b = !new True();
                }
            }
            class True
            {
                public static bool operator !(True a)
                {
                     return false;
                }
            }
            """,
            testHost,
            Keyword("var"),
            Keyword("var"),
            OverloadedOperators.Exclamation,
            Class("True"),
            Class("True"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/29492")]
    [CombinatorialData]
    public Task TestOverloadedOperator_PostfixUnaryExpression(TestHost testHost)
        => TestAsync("""
            class C
            {
                void M()
                {
                    var a = 1;
                    a++;
                    var b = new True();
                    b++;
                }
            }
            class True
            {
                public static True operator ++(True a)
                {
                     return new True();
                }
            }
            """,
            testHost,
            Keyword("var"),
            Local("a"),
            Keyword("var"),
            Class("True"),
            Local("b"),
            OverloadedOperators.PlusPlus,
            Class("True"),
            Class("True"),
            Class("True"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/29492")]
    [CombinatorialData]
    public Task TestOverloadedOperator_ConditionalExpression(TestHost testHost)
        => TestAsync("""
            class C
            {
                void M()
                {
                    var a = 1 == 1;
                    var b = new True() == new True();
                }
            }
            class True
            {
                public static bool operator ==(True a, True b)
                {
                     return true;
                }
            }
            """,
            testHost,
            Keyword("var"),
            Keyword("var"),
            Class("True"),
            OverloadedOperators.EqualsEquals,
            Class("True"),
            Class("True"),
            Class("True"));

    [Theory, CombinatorialData]
    public Task TestCatchDeclarationVariable(TestHost testHost)
        => TestInMethodAsync("""
            try
            {
            }
            catch (Exception ex)
            {
                throw ex;
            }
            """,
            testHost,
            Local("ex"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_InsideMethod(TestHost testHost)
        => TestInMethodAsync("""
            var notnull = 0;
            notnull++;
            """,
            testHost,
            Keyword("var"),
            Local("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_Keyword(TestHost testHost)
        => TestAsync(
            "class X<T> where T : notnull { }",
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface notnull {}
            class X<T> where T : notnull { }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface notnull {}
            }
            class X<T> where T : notnull { }
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Method_Keyword(TestHost testHost)
        => TestAsync("""
            class X
            {
                void M<T>() where T : notnull { }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Method_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface notnull {}
            class X
            {
                void M<T>() where T : notnull { }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Method_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface notnull {}
            }
            class X
            {
                void M<T>() where T : notnull { }
            }
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_Keyword(TestHost testHost)
        => TestAsync(
            "delegate void D<T>() where T : notnull;",
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface notnull {}
            delegate void D<T>() where T : notnull;
            """,
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface notnull {}
            }
            delegate void D<T>() where T : notnull;
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_LocalFunction_Keyword(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    void M<T>() where T : notnull { }
                }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_LocalFunction_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface notnull {}
            class X
            {
                void N()
                {
                    void M<T>() where T : notnull { }
                }
            }
            """,
            testHost,
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_LocalFunction_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""
            namespace OtherScope
            {
                interface notnull {}
            }
            class X
            {
                void N()
                {
                    void M<T>() where T : notnull { }
                }
            }
            """,
            testHost,
            Namespace("OtherScope"),
            TypeParameter("T"),
            Keyword("notnull"));

    [Theory, CombinatorialData]
    public Task NonDiscardVariableDeclaration(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    var _ = int.Parse("");
                }
            }
            """,
            testHost,
            Keyword("var"),
            Method("Parse"),
            Static("Parse"));

    [Theory, CombinatorialData]
    public Task NonDiscardVariableDeclarationMultipleDeclarators(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    int i = 1, _ = 1;
                    int _ = 2, j = 1;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task DiscardAssignment(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    _ = int.Parse("");
                }
            }
            """,
            testHost,
            Keyword("_"),
            Method("Parse"),
            Static("Parse"));

    [Theory, CombinatorialData]
    public Task DiscardInOutDeclaration(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    int.TryParse("", out var _);
                }
            }
            """,
            testHost,
            Method("TryParse"),
            Static("TryParse"),
            Keyword("var"),
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task DiscardInOutAssignment(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    int.TryParse("", out _);
                }
            }
            """,
            testHost,
            Method("TryParse"),
            Static("TryParse"),
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task DiscardInDeconstructionAssignment(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    (x, _) = (0, 0);
                }
            }
            """,
            testHost,
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task DiscardInDeconstructionDeclaration(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    (int x, int _) = (0, 0);
                }
            }
            """,
            testHost,
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task DiscardInPatternMatch(TestHost testHost)
        => TestAsync("""
            class X
            {
                bool N(object x)
                {
                    return x is int _;
                }
            }
            """,
            testHost,
            Parameter("x"),
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task DiscardInSwitch(TestHost testHost)
        => TestAsync("""
            class X
            {
                bool N(object x)
                {
                    switch(x)
                    {
                        case int _:
                            return true;
                        default:
                            return false;
                    }
                }
            }
            """,
            testHost,
            Parameter("x"),
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task DiscardInSwitchPatternMatch(TestHost testHost)
        => TestAsync("""
            class X
            {
                bool N(object x)
                {
                    return x switch
                    {
                        _ => return true;
                    };
                }
            }
            """,
            testHost,
            Parameter("x"),
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task UnusedUnderscoreParameterInLambda(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    System.Func<int, int> a = (int _) => 0;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Delegate("Func"));

    [Theory, CombinatorialData]
    public Task UsedUnderscoreParameterInLambda(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    System.Func<int, int> a = (int _) => _;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Delegate("Func"),
            Parameter("_"));

    [Theory, CombinatorialData]
    public Task DiscardsInLambda(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    System.Func<int, int, int> a = (int _, int _) => 0;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Delegate("Func"),
            Keyword("_"),
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task DiscardsInLambdaWithInferredType(TestHost testHost)
        => TestAsync("""
            class X
            {
                void N()
                {
                    System.Func<int, int, int> a = (_, _) => 0;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Delegate("Func"),
            Keyword("_"),
            Keyword("_"));

    [Theory, CombinatorialData]
    public Task NativeInteger(TestHost testHost)
        => TestInMethodAsync(
            @"nint i = 0; nuint i2 = 0;",
            testHost,
            Classifications(Keyword("nint"), Keyword("nuint")));

    [Theory, CombinatorialData]
    public Task NotNativeInteger(TestHost testHost)
        => TestInMethodAsync(
            "nint",
            "M",
            "nint i = 0;",
            testHost,
            Classifications(Class("nint")));

    [Theory, CombinatorialData]
    public Task NotNativeUnsignedInteger(TestHost testHost)
        => TestInMethodAsync(
            "nuint",
            "M",
            "nuint i = 0;",
            testHost,
            Classifications(Class("nuint")));

    [Theory, CombinatorialData]
    public Task StaticBoldingMethodName(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                public static void Method()
                {
                    System.Action action = Method;
                }
            }
            """,
            testHost,
            Namespace("System"),
            Delegate("Action"),
            Method("Method"),
            Static("Method"));

    [Theory, CombinatorialData]
    public Task StaticBoldingMethodNameNestedInNameof(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                public static void Method()
                {
                    _ = nameof(Method);
                }
            }
            """,
            testHost,
            Keyword("_"),
            Keyword("nameof"),
            Static("Method"),
            Method("Method"));

    [Theory, CombinatorialData]
    public Task BoldingMethodNameStaticAndNot(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                public static void Method()
                {

                }

                public void Method(int x) 
                {

                }

                public void Test() {
                    _ = nameof(Method);
                }
            }
            """,
            testHost,
            Keyword("_"),
            Keyword("nameof"),
            Static("Method"),
            Method("Method"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46985")]
    public Task BasicRecordClassification(TestHost testHost)
        => TestAsync(
            """
            record R
            {
                R r;

                R() { }
            }
            """,
            testHost,
            RecordClass("R"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46985")]
    public Task ParameterizedRecordClassification(TestHost testHost)
        => TestAsync(
            """
            record R(int X, int Y);

            class C
            {
                R r;
            }
            """,
            testHost,
            RecordClass("R"));

    [Theory, CombinatorialData]
    public Task BasicRecordClassClassification(TestHost testHost)
        => TestAsync(
            """
            record class R
            {
                R r;

                R() { }
            }
            """,
            testHost,
            RecordClass("R"));

    [Theory, CombinatorialData]
    public Task BasicRecordStructClassification(TestHost testHost)
        => TestAsync(
            """
            record struct R
            {
                R property { get; set; }
            }
            """,
            testHost,
            RecordStruct("R"));

    [Theory, CombinatorialData]
    public Task BasicFileScopedNamespaceClassification(TestHost testHost)
        => TestAsync(
            """
            namespace NS;

            class C { }
            """,
            testHost,
            Namespace("NS"));

    [Theory, CombinatorialData]
    public Task NullCheckedParameterClassification(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M(string s!!) { }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/57184")]
    public Task MethodGroupClassifications(TestHost testHost)
        => TestAsync(
            """
            var f = m;
            Delegate d = m;
            MulticastDelegate md = m;
            ICloneable c = m;
            object obj = m;
            m(m);

            int m(Delegate d) { }
            """,
            testHost,
            Keyword("var"),
            Method("m"),
            Method("m"),
            Method("m"),
            Method("m"),
            Method("m"),
            Method("m"),
            Method("m"));

    /// <seealso cref="SyntacticClassifierTests.LocalFunctionDeclaration"/>
    /// <seealso cref="TotalClassifierTests.LocalFunctionDeclarationAndUse"/>
    [Theory, CombinatorialData]
    public Task LocalFunctionUse(TestHost testHost)
        => TestAsync(
            """
            using System;

            class C
            {
                void M(Action action)
                {
                    [|localFunction();
                    staticLocalFunction();

                    M(localFunction);
                    M(staticLocalFunction);

                    void localFunction() { }
                    static void staticLocalFunction() { }|]
                }
            }

            """,
            testHost,
            Method("localFunction"),
            Method("staticLocalFunction"),
            Static("staticLocalFunction"),
            Method("M"),
            Method("localFunction"),
            Method("M"),
            Method("staticLocalFunction"),
            Static("staticLocalFunction"));

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/744813")]
    public async Task TestCreateWithBufferNotInWorkspace()
    {
        // don't crash
        using var workspace = EditorTestWorkspace.CreateCSharp("");
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);

        var contentTypeService = document.GetRequiredLanguageService<IContentTypeLanguageService>();
        var contentType = contentTypeService.GetDefaultContentType();
        var extraBuffer = workspace.ExportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer("", contentType);

        WpfTestRunner.RequireWpfFact($"Creates an {nameof(IWpfTextView)} explicitly with an unrelated buffer");
        using var disposableView = workspace.ExportProvider.GetExportedValue<ITextEditorFactoryService>().CreateDisposableTextView(extraBuffer);
        var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();
        var globalOptions = workspace.ExportProvider.GetExportedValue<IGlobalOptionService>();

        var provider = new SemanticClassificationViewTaggerProvider(
            workspace.GetService<TaggerHost>(),
            workspace.GetService<ClassificationTypeMap>());

        using var tagger = provider.CreateTagger(disposableView.TextView, extraBuffer);
        using (var edit = extraBuffer.CreateEdit())
        {
            edit.Insert(0, "class A { }");
            edit.Apply();
        }

        var waiter = listenerProvider.GetWaiter(FeatureAttribute.Classification);
        await waiter.ExpeditedWaitAsync();
    }
}

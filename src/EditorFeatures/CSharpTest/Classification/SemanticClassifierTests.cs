// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    [Trait(Traits.Feature, Traits.Features.Classification)]
    public class SemanticClassifierTests : AbstractCSharpClassifierTests
    {
        protected override async Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, TextSpan span, ParseOptions? options, TestHost testHost)
        {
            using var workspace = CreateWorkspace(code, options, testHost);
            var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);

            return await GetSemanticClassificationsAsync(document, span);
        }

        [Theory]
        [CombinatorialData]
        public async Task GenericClassDeclaration(TestHost testHost)
        {
            await TestInMethodAsync(
                className: "Class<T>",
                methodName: "M",
                @"new Class<int>();",
                testHost,
                Class("Class"));
        }

        [Theory]
        [CombinatorialData]
        public async Task RefVar(TestHost testHost)
        {
            await TestInMethodAsync(
                @"int i = 0; ref var x = ref i;",
                testHost,
                Classifications(Keyword("var"), Local("i")));
        }

        [Theory]
        [CombinatorialData]
        public async Task UsingAlias1(TestHost testHost)
        {
            await TestAsync(
@"using M = System.Math;",
                testHost,
                Class("M"),
                Namespace("System"),
                Class("Math"),
                Static("Math"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsTypeArgument(TestHost testHost)
        {
            await TestInMethodAsync(
                className: "Class<T>",
                methodName: "M",
                @"new Class<dynamic>();",
                testHost,
                Classifications(Class("Class"), Keyword("dynamic")));
        }

        [Theory]
        [CombinatorialData]
        public async Task UsingTypeAliases(TestHost testHost)
        {
            var code = @"using Alias = Test; 
class Test { void M() { Test a = new Test(); Alias b = new Alias(); } }";

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

        [Theory]
        [CombinatorialData]
        public async Task DynamicTypeAlias(TestHost testHost)
        {
            await TestAsync(
@"using dynamic = System.EventArgs;

class C
{
    dynamic d = new dynamic();
}",
                testHost,
                Class("dynamic"),
                Namespace("System"),
                Class("EventArgs"),
                Class("dynamic"),
                Class("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsDelegateName(TestHost testHost)
        {
            await TestAsync(
@"delegate void dynamic();

class C
{
    void M()
    {
        dynamic d;
    }
}",
                testHost,
                Delegate("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsInterfaceName(TestHost testHost)
        {
            await TestAsync(
@"interface dynamic
{
}

class C
{
    dynamic d;
}",
                testHost,
                Interface("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsEnumName(TestHost testHost)
        {
            await TestAsync(
@"enum dynamic
{
}

class C
{
    dynamic d;
}",
                testHost,
                Enum("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsClassName(TestHost testHost)
        {
            await TestAsync(
@"class dynamic
{
}

class C
{
    dynamic d;
}",
                testHost,
                Class("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(46985, "https://github.com/dotnet/roslyn/issues/46985")]
        public async Task DynamicAsRecordName(TestHost testHost)
        {
            await TestAsync(
@"record dynamic
{
}

class C
{
    dynamic d;
}",
                testHost,
                Record("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsClassNameAndLocalVariableName(TestHost testHost)
        {
            await TestAsync(
@"class dynamic
{
    dynamic()
    {
        dynamic dynamic;
    }
}",
                testHost,
                Class("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsStructName(TestHost testHost)
        {
            await TestAsync(
@"struct dynamic
{
}

class C
{
    dynamic d;
}",
                testHost,
                Struct("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsGenericClassName(TestHost testHost)
        {
            await TestAsync(
@"class dynamic<T>
{
}

class C
{
    dynamic<int> d;
}",
                testHost,
                Class("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsGenericClassNameButOtherArity(TestHost testHost)
        {
            await TestAsync(
@"class dynamic<T>
{
}

class C
{
    dynamic d;
}",
                testHost,
                Keyword("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsUndefinedGenericType(TestHost testHost)
        {
            await TestAsync(
@"class dynamic
{
}

class C
{
    dynamic<int> d;
}",
                testHost,
                Class("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsExternAlias(TestHost testHost)
        {
            await TestAsync(
@"extern alias dynamic;

class C
{
    dynamic::Goo a;
}",
    testHost,
    Namespace("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task GenericClassNameButOtherArity(TestHost testHost)
        {
            await TestAsync(
@"class A<T>
{
}

class C
{
    A d;
}", testHost,
 Class("A"));
        }

        [Theory]
        [CombinatorialData]
        public async Task GenericTypeParameter(TestHost testHost)
        {
            await TestAsync(
@"class C<T>
{
    void M()
    {
        default(T) }
}",
                testHost,
                TypeParameter("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task GenericMethodTypeParameter(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    T M<T>(T t)
    {
        return default(T);
    }
}",
                testHost,
                TypeParameter("T"),
                TypeParameter("T"),
                TypeParameter("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task GenericMethodTypeParameterInLocalVariableDeclaration(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void M<T>()
    {
        T t;
    }
}",
                testHost,
                TypeParameter("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ParameterOfLambda1(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    C()
    {
        Action a = (C p) => {
        };
    }
}",
                testHost,
                Class("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ParameterOfAnonymousMethod(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    C()
    {
        Action a = delegate (C p) {
        };
    }
}",
                testHost,
                Class("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task GenericTypeParameterAfterWhere(TestHost testHost)
        {
            await TestAsync(
@"class C<A, B> where A : B
{
}",
                testHost,
                TypeParameter("A"),
                TypeParameter("B"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BaseClass(TestHost testHost)
        {
            await TestAsync(
@"class C
{
}

class C2 : C
{
}",
                testHost,
                Class("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BaseInterfaceOnInterface(TestHost testHost)
        {
            await TestAsync(
@"interface T
{
}

interface T2 : T
{
}",
                testHost,
                Interface("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BaseInterfaceOnClass(TestHost testHost)
        {
            await TestAsync(
@"interface T
{
}

class T2 : T
{
}",
                testHost,
                Interface("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task InterfaceColorColor(TestHost testHost)
        {
            await TestAsync(
@"interface T
{
}

class T2 : T
{
    T T;
}",
                testHost,
                Interface("T"),
                Interface("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DelegateColorColor(TestHost testHost)
        {
            await TestAsync(
@"delegate void T();

class T2
{
    T T;
}",
                testHost,
                Delegate("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DelegateReturnsItself(TestHost testHost)
        {
            await TestAsync(
@"delegate T T();

class C
{
    T T(T t);
}",
                testHost,
                Delegate("T"),
                Delegate("T"),
                Delegate("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task StructColorColor(TestHost testHost)
        {
            await TestAsync(
@"struct T
{
    T T;
}",
                testHost,
                Struct("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EnumColorColor(TestHost testHost)
        {
            await TestAsync(
@"enum T
{
    T,
    T
}

class C
{
    T T;
}",
                testHost,
                Enum("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsGenericTypeParameter(TestHost testHost)
        {
            await TestAsync(
@"class C<dynamic>
{
    dynamic d;
}",
                testHost,
                TypeParameter("dynamic"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DynamicAsGenericFieldName(TestHost testHost)
        {
            await TestAsync(
@"class A<T>
{
    T dynamic;
}",
                testHost,
                TypeParameter("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task PropertySameNameAsClass(TestHost testHost)
        {
            await TestAsync(
@"class N
{
    N N { get; set; }

    void M()
    {
        N n = N;
        N = n;
        N = N;
    }
}",
                testHost,
                Class("N"),
                Class("N"),
                Property("N"),
                Property("N"),
                Local("n"),
                Property("N"),
                Property("N"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AttributeWithoutAttributeSuffix(TestHost testHost)
        {
            await TestAsync(
@"using System;

[Obsolete]
class C
{
}",
                testHost,
                Namespace("System"),
                Class("Obsolete"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AttributeOnNonExistingMember(TestHost testHost)
        {
            await TestAsync(
@"using System;

class A
{
    [Obsolete]
}",
                testHost,
                Namespace("System"),
                Class("Obsolete"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AttributeWithoutAttributeSuffixOnAssembly(TestHost testHost)
        {
            await TestAsync(
@"using System;

[assembly: My]

class MyAttribute : Attribute
{
}",
                testHost,
                Namespace("System"),
                Class("My"),
                Class("Attribute"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AttributeViaNestedClassOrDerivedClass(TestHost testHost)
        {
            await TestAsync(
@"using System;

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
}",
                testHost,
                Namespace("System"),
                Class("Base"),
                Class("My"),
                Class("Derived"),
                Class("My"),
                Class("Attribute"),
                Class("Base"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NamedAndOptional(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void B(C C = null)
    {
    }

    void M()
    {
        B(C: null);
    }
}",
                testHost,
                Class("C"),
                Method("B"),
                Parameter("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task PartiallyWrittenGenericName1(TestHost testHost)
        {
            await TestInMethodAsync(
                className: "Class<T>",
                methodName: "M",
                @"Class<int",
                testHost,
                Class("Class"));
        }

        [Theory]
        [CombinatorialData]
        public async Task PartiallyWrittenGenericName2(TestHost testHost)
        {
            await TestInMethodAsync(
                className: "Class<T1, T2>",
                methodName: "M",
                @"Class<int, b",
                testHost,
                Class("Class"));
        }

        // The "Color Color" problem is the C# IDE folklore for when
        // a property name is the same as a type name
        // and the resulting ambiguities that the spec
        // resolves in favor of properties
        [Theory]
        [CombinatorialData]
        public async Task ColorColor(TestHost testHost)
        {
            await TestAsync(
@"class Color
{
    Color Color;
}",
                testHost,
                Class("Color"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor2(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    T T = new T();

    T()
    {
        this.T = new T();
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Field("T"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor3(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    T T = new T();

    void M();

    T()
    {
        T.M();
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Field("T"),
                Method("M"));
        }

        /// <summary>
        /// Instance field should be preferred to type
        /// §7.5.4.1
        /// </summary>
        [Theory]
        [CombinatorialData]
        public async Task ColorColor4(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    T T;

    void M()
    {
        T.T = null;
    }
}",
                testHost,
                Class("T"),
                Field("T"),
                Field("T"));
        }

        /// <summary>
        /// Type should be preferred to a static field
        /// §7.5.4.1
        /// </summary>
        [Theory]
        [CombinatorialData]
        public async Task ColorColor5(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    static T T;

    void M()
    {
        T.T = null;
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Field("T"),
                Static("T"));
        }

        /// <summary>
        /// Needs to prefer the local
        /// </summary>
        [Theory]
        [CombinatorialData]
        public async Task ColorColor6(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    int field;

    void M()
    {
        T T = new T();
        T.field = 0;
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Local("T"),
                Field("field"));
        }

        /// <summary>
        /// Needs to prefer the type
        /// </summary>
        [Theory]
        [CombinatorialData]
        public async Task ColorColor7(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    static int field;

    void M()
    {
        T T = new T();
        T.field = 0;
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Class("T"),
                Field("field"),
                Static("field"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor8(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    void M(T T)
    {
    }

    void M2()
    {
        T T = new T();
        M(T);
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Class("T"),
                Method("M"),
                Local("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor9(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    T M(T T)
    {
        T = new T();
        return T;
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Parameter("T"),
                Class("T"),
                Parameter("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor10(TestHost testHost)
        {
            // note: 'var' now binds to the type of the local.
            await TestAsync(
@"class T
{
    void M()
    {
        var T = new object();
        T temp = T as T;
    }
}",
                testHost,
                Keyword("var"),
                Class("T"),
                Local("T"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor11(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    void M()
    {
        var T = new object();
        bool b = T is T;
    }
}",
                testHost,
                Keyword("var"),
                Local("T"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor12(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    void M()
    {
        T T = new T();
        var t = typeof(T);
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Keyword("var"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor13(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    void M()
    {
        T T = new T();
        T t = default(T);
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task ColorColor14(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    void M()
    {
        object T = new T();
        T t = (T)T;
    }
}",
                testHost,
                Class("T"),
                Class("T"),
                Class("T"),
                Local("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NamespaceNameSameAsTypeName1(TestHost testHost)
        {
            await TestAsync(
@"namespace T
{
    class T
    {
        void M()
        {
            T.T T = new T.T();
        }
    }
}",
                testHost,
                Namespace("T"),
                Class("T"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NamespaceNameSameAsTypeNameWithGlobal(TestHost testHost)
        {
            await TestAsync(
@"namespace T
{
    class T
    {
        void M()
        {
            global::T.T T = new global::T.T();
        }
    }
}",
                testHost,
                Namespace("T"),
                Namespace("T"),
                Class("T"),
                Namespace("T"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AmbiguityTypeAsGenericMethodArgumentVsLocal(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    void M<T>()
    {
        T T;
        M<T>();
    }
}",
                testHost,
                TypeParameter("T"),
                Method("M"),
                TypeParameter("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AmbiguityTypeAsGenericArgumentVsLocal(TestHost testHost)
        {
            await TestAsync(
@"class T
{
    class G<T>
    {
    }

    void M()
    {
        T T;
        G<T> g = new G<T>();
    }
}",
                testHost,
                Class("T"),
                Class("G"),
                Class("T"),
                Class("G"),
                Class("T"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AmbiguityTypeAsGenericArgumentVsField(TestHost testHost)
        {
            await TestAsync(
@"class T
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
}",
                testHost,
                Class("T"),
                Class("H"),
                Class("T"),
                Field("f"),
                Static("f"));
        }

        /// <summary>
        /// §7.5.4.2
        /// </summary>
        [Theory]
        [CombinatorialData]
        public async Task GrammarAmbiguity_7_5_4_2(TestHost testHost)
        {
            await TestAsync(
@"class M
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
}",
                testHost,
                Method("F"),
                Method("G"),
                Class("A"),
                Class("B"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AnonymousTypePropertyName(TestHost testHost)
        {
            await TestAsync(
@"using System;

class C
{
    void M()
    {
        var x = new { String = "" }; } }",
                testHost,
                Namespace("System"),
                Keyword("var"),
                Property("String"));
        }

        [Theory]
        [CombinatorialData]
        public async Task YieldAsATypeName(TestHost testHost)
        {
            await TestAsync(
@"using System.Collections.Generic;

class yield
{
    IEnumerable<yield> M()
    {
        yield yield = new yield();
        yield return yield;
    }
}",
                testHost,
                Namespace("System"),
                Namespace("Collections"),
                Namespace("Generic"),
                Interface("IEnumerable"),
                Class("yield"),
                Class("yield"),
                Class("yield"),
                Local("yield"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TypeNameDottedNames(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    class Nested
    {
    }

    C.Nested f;
}",
                testHost,
                Class("C"),
                Class("Nested"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BindingTypeNameFromBCLViaGlobalAlias(TestHost testHost)
        {
            await TestAsync(
@"using System;

class C
{
    global::System.String f;
}",
                testHost,
                Namespace("System"),
                Namespace("System"),
                Class("String"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BindingTypeNames(TestHost testHost)
        {
            var code = @"using System;
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

        [Theory]
        [CombinatorialData]
        public async Task Constructors(TestHost testHost)
        {
            await TestAsync(
@"struct S
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
}",
                testHost,
                Field("i"),
                Parameter("i"),
                Keyword("var"),
                Struct("S"),
                Keyword("var"),
                Class("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TypesOfClassMembers(TestHost testHost)
        {
            await TestAsync(
@"class Type
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
}",
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
        }

        /// <summary>
        /// NAQ = Namespace Alias Qualifier (?)
        /// </summary>
        [Theory]
        [CombinatorialData]
        public async Task NAQTypeNameCtor(TestHost testHost)
        {
            await TestInMethodAsync(
@"System.IO.BufferedStream b = new global::System.IO.BufferedStream();",
                testHost,
                Namespace("System"),
                Namespace("IO"),
                Class("BufferedStream"),
                Namespace("System"),
                Namespace("IO"),
                Class("BufferedStream"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQEnum(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void M()
    {
        global::System.IO.DriveType d;
    }
}",
                testHost,
                Namespace("System"),
                Namespace("IO"),
                Enum("DriveType"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQDelegate(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void M()
    {
        global::System.AssemblyLoadEventHandler d;
    }
}",
                testHost,
                Namespace("System"),
                Delegate("AssemblyLoadEventHandler"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQTypeNameMethodCall(TestHost testHost)
        {
            await TestInMethodAsync(@"global::System.String.Clone("");",
                testHost,
                Namespace("System"),
                Class("String"),
                Method("Clone"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQEventSubscription(TestHost testHost)
        {
            await TestInMethodAsync(
@"global::System.AppDomain.CurrentDomain.AssemblyLoad += 
            delegate (object sender, System.AssemblyLoadEventArgs args) {};",
                testHost,
                Namespace("System"),
                Class("AppDomain"),
                Property("CurrentDomain"),
                Static("CurrentDomain"),
                Event("AssemblyLoad"),
                Namespace("System"),
                Class("AssemblyLoadEventArgs"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AnonymousDelegateParameterType(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void M()
    {
        System.Action<System.EventArgs> a = delegate (System.EventArgs e) {
        };
    }
}",
                testHost,
                Namespace("System"),
                Delegate("Action"),
                Namespace("System"),
                Class("EventArgs"),
                Namespace("System"),
                Class("EventArgs"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQCtor(TestHost testHost)
        {
            await TestInMethodAsync(
@"global::System.Collections.DictionaryEntry de = new global::System.Collections.DictionaryEntry();",
                testHost,
                Namespace("System"),
                Namespace("Collections"),
                Struct("DictionaryEntry"),
                Namespace("System"),
                Namespace("Collections"),
                Struct("DictionaryEntry"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQSameFileClass(TestHost testHost)
        {
            var code = @"class C { static void M() { global::C.M(); } }";

            await TestAsync(code,
                testHost,
                ParseOptions(Options.Regular),
                Class("C"),
                Method("M"),
                Static("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task InteractiveNAQSameFileClass(TestHost testHost)
        {
            var code = @"class C { static void M() { global::Script.C.M(); } }";

            await TestAsync(code,
                testHost,
                ParseOptions(Options.Script),
                Class("Script"),
                Class("C"),
                Method("M"),
                Static("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQSameFileClassWithNamespace(TestHost testHost)
        {
            await TestAsync(
@"using @global = N;

namespace N
{
    class C
    {
        static void M()
        {
            global::N.C.M();
        }
    }
}",
                testHost,
                Namespace("@global"),
                Namespace("N"),
                Namespace("N"),
                Namespace("N"),
                Class("C"),
                Method("M"),
                Static("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQSameFileClassWithNamespaceAndEscapedKeyword(TestHost testHost)
        {
            await TestAsync(
@"using @global = N;

namespace N
{
    class C
    {
        static void M()
        {
            @global.C.M();
        }
    }
}",
                testHost,
                Namespace("@global"),
                Namespace("N"),
                Namespace("N"),
                Namespace("@global"),
                Class("C"),
                Method("M"),
                Static("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQGlobalWarning(TestHost testHost)
        {
            await TestAsync(
@"using global = N;

namespace N
{
    class C
    {
        static void M()
        {
            global.C.M();
        }
    }
}",
                testHost,
                Namespace("global"),
                Namespace("N"),
                Namespace("N"),
                Namespace("global"),
                Class("C"),
                Method("M"),
                Static("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQUserDefinedNAQNamespace(TestHost testHost)
        {
            await TestAsync(
@"using goo = N;

namespace N
{
    class C
    {
        static void M()
        {
            goo.C.M();
        }
    }
}",
                testHost,
                Namespace("goo"),
                Namespace("N"),
                Namespace("N"),
                Namespace("goo"),
                Class("C"),
                Method("M"),
                Static("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQUserDefinedNAQNamespaceDoubleColon(TestHost testHost)
        {
            await TestAsync(
@"using goo = N;

namespace N
{
    class C
    {
        static void M()
        {
            goo::C.M();
        }
    }
}",
                testHost,
                Namespace("goo"),
                Namespace("N"),
                Namespace("N"),
                Namespace("goo"),
                Class("C"),
                Method("M"),
                Static("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQUserDefinedNamespace1(TestHost testHost)
        {
            await TestAsync(
@"class C
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
}",
                testHost,
                Namespace("A"),
                Namespace("B"),
                Class("D"),
                Namespace("A"),
                Namespace("B"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQUserDefinedNamespaceWithGlobal(TestHost testHost)
        {
            await TestAsync(
@"class C
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
}",
                testHost,
                Namespace("A"),
                Namespace("B"),
                Class("D"),
                Namespace("A"),
                Namespace("B"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQUserDefinedNAQForClass(TestHost testHost)
        {
            await TestAsync(
@"using IO = global::System.IO;

class C
{
    void M()
    {
        IO::BinaryReader b;
    }
}",
                testHost,
                Namespace("IO"),
                Namespace("System"),
                Namespace("IO"),
                Namespace("IO"),
                Class("BinaryReader"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NAQUserDefinedTypes(TestHost testHost)
        {
            await TestAsync(
@"using rabbit = MyNameSpace;

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
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task PreferPropertyOverNestedClass(TestHost testHost)
        {
            await TestAsync(
@"class Outer
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
                testHost,
                Class("A"),
                Class("A"),
                Local("a"),
                Field("B"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TypeNameInsideNestedClass(TestHost testHost)
        {
            await TestAsync(
@"using System;

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
        }

        [Theory]
        [CombinatorialData]
        public async Task StructEnumTypeNames(TestHost testHost)
        {
            await TestAsync(
@"using System;

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
}",
                testHost,
                Namespace("System"),
                Enum("ConsoleColor"),
                Struct("Int32"));
        }

        [Theory]
        [CombinatorialData]
        public async Task PreferFieldOverClassWithSameName(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    public int C;

    void M()
    {
        C = 0;
    }
}", testHost,
 Field("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task AttributeBinding(TestHost testHost)
        {
            await TestAsync(
@"using System;

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
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task ShouldNotClassifyNamespacesAsTypes(TestHost testHost)
        {
            await TestAsync(
@"using System;

namespace Roslyn.Compilers.Internal
{
}",
    testHost,
    Namespace("System"),
    Namespace("Roslyn"),
    Namespace("Compilers"),
    Namespace("Internal"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedTypeCantHaveSameNameAsParentType(TestHost testHost)
        {
            await TestAsync(
@"class Program
{
    class Program
    {
    }

    static void Main(Program p)
    {
    }

    Program.Program p2;
}",
                testHost,
                Class("Program"),
                Class("Program"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias(TestHost testHost)
        {
            var code = @"class Program
{
    class Program { }
    static void Main(Program p) { }
    global::Program.Program p;
}";

            await TestAsync(code,
                testHost,
                ParseOptions(Options.Regular),
                Class("Program"),
                Class("Program"),
                Class("Program"));
        }

        [Theory]
        [CombinatorialData]
        public async Task InteractiveNestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias(TestHost testHost)
        {
            var code = @"class Program
{
    class Program { }
    static void Main(Program p) { }
    global::Script.Program.Program p;
}";

            await TestAsync(code,
                testHost,
                ParseOptions(Options.Script),
                Class("Program"),
                Class("Script"),
                Class("Program"),
                Class("Program"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EnumFieldWithSameNameShouldBePreferredToType(TestHost testHost)
        {
            await TestAsync(
@"enum E
{
    E,
    F = E
}", testHost,
 EnumMember("E"));
        }

        [WorkItem(541150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541150")]
        [Theory]
        [CombinatorialData]
        public async Task TestGenericVarClassification(TestHost testHost)
        {
            await TestAsync(
@"using System;

static class Program
{
    static void Main()
    {
        var x = 1;
    }
}

class var<T>
{
}",
    testHost,
    Namespace("System"),
    Keyword("var"));
        }

        [WorkItem(541154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
        [Theory]
        [CombinatorialData]
        public async Task TestInaccessibleVarClassification(TestHost testHost)
        {
            await TestAsync(
@"using System;

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
}",
                testHost,
                Namespace("System"),
                Class("A"),
                Keyword("var"));
        }

        [WorkItem(541154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
        [Theory]
        [CombinatorialData]
        public async Task TestVarNamedTypeClassification(TestHost testHost)
        {
            await TestAsync(
@"class var
{
    static void Main()
    {
        var x;
    }
}",
                testHost,
                Class("var"));
        }

        [WorkItem(9513, "DevDiv_Projects/Roslyn")]
        [Theory]
        [CombinatorialData]
        public async Task RegressionFor9513(TestHost testHost)
        {
            await TestAsync(
@"enum E
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
}",
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
        }

        [WorkItem(542368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542368")]
        [Theory]
        [CombinatorialData]
        public async Task RegressionFor9572(TestHost testHost)
        {
            await TestAsync(
@"class A<T, S> where T : A<T, S>.I, A<T, T>.I
{
    public interface I
    {
    }
}",
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
        }

        [WorkItem(542368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542368")]
        [Theory]
        [CombinatorialData]
        public async Task RegressionFor9831(TestHost testHost)
        {
            await TestAsync(@"F : A",
@"public class B<T>
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
}",
                testHost,
                Class("A"));
        }

        [WorkItem(542432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542432")]
        [Theory]
        [CombinatorialData]
        public async Task TestVar(TestHost testHost)
        {
            await TestAsync(
@"class Program
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
}",
                testHost,
                Class("var"),
                Keyword("var"),
                Method("GetVarT"),
                Static("GetVarT"),
                Keyword("var"),
                Class("var"));
        }

        [WorkItem(543123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
        [Theory]
        [CombinatorialData]
        public async Task TestVar2(TestHost testHost)
        {
            await TestAsync(
@"class Program
{
    void Main(string[] args)
    {
        foreach (var v in args)
        {
        }
    }
}",
                testHost,
                Keyword("var"),
                Parameter("args"));
        }

        [WorkItem(542778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542778")]
        [Theory]
        [CombinatorialData]
        public async Task TestDuplicateTypeParamWithConstraint(TestHost testHost)
        {
            await TestAsync(@"where U : IEnumerable<S>",
@"using System.Collections.Generic;

class C<T>
{
    public void Goo<U, U>(U arg)
        where S : T
        where U : IEnumerable<S>
    {
    }
}",
                testHost,
                TypeParameter("U"),
                Interface("IEnumerable"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Theory]
        [CombinatorialData]
        public async Task OptimisticallyColorFromInDeclaration(TestHost testHost)
        {
            await TestInExpressionAsync("from ",
                testHost,
                Keyword("from"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Theory]
        [CombinatorialData]
        public async Task OptimisticallyColorFromInAssignment(TestHost testHost)
        {
            await TestInMethodAsync(
@"var q = 3;

q = from",
                testHost,
                Keyword("var"),
                Local("q"),
                Keyword("from"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Theory]
        [CombinatorialData]
        public async Task DontColorThingsOtherThanFromInDeclaration(TestHost testHost)
            => await TestInExpressionAsync("fro ", testHost);

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Theory]
        [CombinatorialData]
        public async Task DontColorThingsOtherThanFromInAssignment(TestHost testHost)
        {
            await TestInMethodAsync(
@"var q = 3;

q = fro",
                testHost,
                Keyword("var"),
                Local("q"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Theory]
        [CombinatorialData]
        public async Task DontColorFromWhenBoundInDeclaration(TestHost testHost)
        {
            await TestInMethodAsync(
@"var from = 3;
var q = from",
                testHost,
                Keyword("var"),
                Keyword("var"),
                Local("from"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Theory]
        [CombinatorialData]
        public async Task DontColorFromWhenBoundInAssignment(TestHost testHost)
        {
            await TestInMethodAsync(
@"var q = 3;
var from = 3;

q = from",
                testHost,
                Keyword("var"),
                Keyword("var"),
                Local("q"),
                Local("from"));
        }

        [WorkItem(543404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543404")]
        [Theory]
        [CombinatorialData]
        public async Task NewOfClassWithOnlyPrivateConstructor(TestHost testHost)
        {
            await TestAsync(
@"class X
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
}",
                testHost,
                Class("X"));
        }

        [WorkItem(544179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544179")]
        [Theory]
        [CombinatorialData]
        public async Task TestNullableVersusConditionalAmbiguity1(TestHost testHost)
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
        C1 ?
    }
}

public class C1
{
}",
                testHost,
                Class("C1"));
        }

        [WorkItem(544179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544179")]
        [Theory]
        [CombinatorialData]
        public async Task TestPointerVersusMultiplyAmbiguity1(TestHost testHost)
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
        C1 *
    }
}

public class C1
{
}",
                testHost,
                Class("C1"));
        }

        [WorkItem(544302, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544302")]
        [Theory]
        [CombinatorialData]
        public async Task EnumTypeAssignedToNamedPropertyOfSameNameInAttributeCtor(TestHost testHost)
        {
            await TestAsync(
@"using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""abc"", CallingConvention = CallingConvention)]
    static extern void M();
}",
                testHost,
                Namespace("System"),
                Namespace("System"),
                Namespace("Runtime"),
                Namespace("InteropServices"),
                Class("DllImport"),
                Field("CallingConvention"),
                Enum("CallingConvention"));
        }

        [WorkItem(531119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531119")]
        [Theory]
        [CombinatorialData]
        public async Task OnlyClassifyGenericNameOnce(TestHost testHost)
        {
            await TestAsync(
@"enum Type
{
}

struct Type<T>
{
    Type<int> f;
}",
                testHost,
                Struct("Type"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NameOf1(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void goo()
    {
        var x = nameof
    }
}",
                testHost,
                Keyword("var"),
                Keyword("nameof"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NameOf2(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void goo()
    {
        var x = nameof(C);
    }
}",
                testHost,
                Keyword("var"),
                Keyword("nameof"),
                Class("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NameOfLocalMethod(TestHost testHost)
        {
            await TestAsync(
@"class C
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
}",
                testHost,
                Keyword("var"),
                Keyword("nameof"),
                Method("M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task MethodCalledNameOfInScope(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    void nameof(int i)
    {
    }

    void goo()
    {
        int y = 3;
        var x = nameof();
    }
}",
                testHost,
                Keyword("var"),
                Method("nameof"));
        }

        [WpfFact]
        [WorkItem(744813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/744813")]
        public async Task TestCreateWithBufferNotInWorkspace()
        {
            // don't crash
            using var workspace = TestWorkspace.CreateCSharp("");
            var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);

            var contentTypeService = document.GetRequiredLanguageService<IContentTypeLanguageService>();
            var contentType = contentTypeService.GetDefaultContentType();
            var extraBuffer = workspace.ExportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer("", contentType);

            WpfTestRunner.RequireWpfFact($"Creates an {nameof(IWpfTextView)} explicitly with an unrelated buffer");
            using var disposableView = workspace.ExportProvider.GetExportedValue<ITextEditorFactoryService>().CreateDisposableTextView(extraBuffer);
            var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();
            var globalOptions = workspace.ExportProvider.GetExportedValue<IGlobalOptionService>();

            var provider = new SemanticClassificationViewTaggerProvider(
                workspace.GetService<IThreadingContext>(),
                workspace.GetService<ClassificationTypeMap>(),
                globalOptions,
                listenerProvider);

            using var tagger = (IDisposable?)provider.CreateTagger<IClassificationTag>(disposableView.TextView, extraBuffer);
            using (var edit = extraBuffer.CreateEdit())
            {
                edit.Insert(0, "class A { }");
                edit.Apply();
            }

            var waiter = listenerProvider.GetWaiter(FeatureAttribute.Classification);
            await waiter.ExpeditedWaitAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task Tuples(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    (int a, int b) x;
}",
                testHost,
                ParseOptions(TestOptions.Regular, Options.Script));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(261049, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/261049")]
        public async Task DevDiv261049RegressionTest(TestHost testHost)
        {
            var source = @"
        var (a,b) =  Get(out int x, out int y);
        Console.WriteLine($""({a.first}, {a.second})"");";

            await TestInMethodAsync(
                source,
                testHost,
                Keyword("var"), Local("a"), Local("a"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        public async Task InXmlDocCref_WhenTypeOnlyIsSpecified_ItIsClassified(TestHost testHost)
        {
            await TestAsync(
@"/// <summary>
/// <see cref=""MyClass""/>
/// </summary>
class MyClass
{
    public MyClass(int x)
    {
    }
}",
    testHost,
    Class("MyClass"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        public async Task InXmlDocCref_WhenConstructorOnlyIsSpecified_NothingIsClassified(TestHost testHost)
        {
            await TestAsync(
@"/// <summary>
/// <see cref=""MyClass(int)""/>
/// </summary>
class MyClass
{
    public MyClass(int x)
    {
    }
}", testHost,
 Class("MyClass"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        public async Task InXmlDocCref_WhenTypeAndConstructorSpecified_OnlyTypeIsClassified(TestHost testHost)
        {
            await TestAsync(
@"/// <summary>
/// <see cref=""MyClass.MyClass(int)""/>
/// </summary>
class MyClass
{
    public MyClass(int x)
    {
    }
}",
    testHost,
    Class("MyClass"),
    Class("MyClass"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(13174, "https://github.com/dotnet/roslyn/issues/13174")]
        public async Task TestMemberBindingThatLooksGeneric(TestHost testHost)
        {
            await TestAsync(
@"using System.Diagnostics;
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
}",
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
        }

        [WpfTheory(Skip = "https://github.com/dotnet/roslyn/issues/30855")]
        [CombinatorialData]
        [WorkItem(18956, "https://github.com/dotnet/roslyn/issues/18956")]
        public async Task TestVarInPattern1(TestHost testHost)
        {
            await TestAsync(
@"
class Program
{
    void Main(string s)
    {
        if (s is var v)
        {
        }
    }
}", testHost,
 Parameter("s"), Keyword("var"));
        }

        [WpfTheory(Skip = "https://github.com/dotnet/roslyn/issues/30855")]
        [CombinatorialData]
        [WorkItem(18956, "https://github.com/dotnet/roslyn/issues/18956")]
        public async Task TestVarInPattern2(TestHost testHost)
        {
            await TestAsync(
@"
class Program
{
    void Main(string s)
    {
        switch (s)
        {
            case var v:
        }
    }
}", testHost,
 Parameter("s"), Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(23940, "https://github.com/dotnet/roslyn/issues/23940")]
        public async Task TestAliasQualifiedClass(TestHost testHost)
        {
            await TestAsync(
@"
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
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_InsideMethod(TestHost testHost)
        {
            // Asserts no Keyword("unmanaged") because it is an identifier.
            await TestInMethodAsync(@"
var unmanaged = 0;
unmanaged++;",
                testHost,
                Keyword("var"),
                Local("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Type_Keyword(TestHost testHost)
        {
            await TestAsync(
                "class X<T> where T : unmanaged { }",
                testHost,
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Type_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface unmanaged {}
class X<T> where T : unmanaged { }",
                testHost,
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Type_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
class X<T> where T : unmanaged { }",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Method_Keyword(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void M<T>() where T : unmanaged { }
}",
                testHost,
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Method_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface unmanaged {}
class X
{
    void M<T>() where T : unmanaged { }
}",
                testHost,
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Method_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
class X
{
    void M<T>() where T : unmanaged { }
}",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Delegate_Keyword(TestHost testHost)
        {
            await TestAsync(
                "delegate void D<T>() where T : unmanaged;",
                testHost,
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface unmanaged {}
delegate void D<T>() where T : unmanaged;",
                testHost,
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
delegate void D<T>() where T : unmanaged;",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex1(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = new Regex(@""$(\a\t\u0020)|[^\p{Lu}-a\w\sa-z-[m-p]]+?(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^"");
    }
}",
testHost,
Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Class("Regex"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("t"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("u"),
Regex.OtherEscape("0020"),
Regex.Grouping(")"),
Regex.Alternation("|"),
Regex.CharacterClass("["),
Regex.CharacterClass("^"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("p"),
Regex.CharacterClass("{"),
Regex.CharacterClass("Lu"),
Regex.CharacterClass("}"),
Regex.Text("-a"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("w"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("s"),
Regex.Text("a"),
Regex.CharacterClass("-"),
Regex.Text("z"),
Regex.CharacterClass("-"),
Regex.CharacterClass("["),
Regex.Text("m"),
Regex.CharacterClass("-"),
Regex.Text("p"),
Regex.CharacterClass("]"),
Regex.CharacterClass("]"),
Regex.Quantifier("+"),
Regex.Quantifier("?"),
Regex.Comment("(?#comment)"),
Regex.Alternation("|"),
Regex.Grouping("("),
Regex.Anchor("\\"),
Regex.Anchor("b"),
Regex.Anchor("\\"),
Regex.Anchor("G"),
Regex.Anchor("\\"),
Regex.Anchor("z"),
Regex.Grouping(")"),
Regex.Alternation("|"),
Regex.Grouping("("),
Regex.Grouping("?"),
Regex.Grouping("<"),
Regex.Grouping("name"),
Regex.Grouping(">"),
Regex.Text("sub"),
Regex.Grouping(")"),
Regex.Quantifier("{"),
Regex.Quantifier("0"),
Regex.Quantifier(","),
Regex.Quantifier("5"),
Regex.Quantifier("}"),
Regex.Quantifier("?"),
Regex.Anchor("^"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex2(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        // language=regex
        var r = @""$(\a\t\u0020)|[^\p{Lu}-a\w\sa-z-[m-p]]+?(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^"";
    }
}",
testHost,
Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("t"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("u"),
Regex.OtherEscape("0020"),
Regex.Grouping(")"),
Regex.Alternation("|"),
Regex.CharacterClass("["),
Regex.CharacterClass("^"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("p"),
Regex.CharacterClass("{"),
Regex.CharacterClass("Lu"),
Regex.CharacterClass("}"),
Regex.Text("-a"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("w"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("s"),
Regex.Text("a"),
Regex.CharacterClass("-"),
Regex.Text("z"),
Regex.CharacterClass("-"),
Regex.CharacterClass("["),
Regex.Text("m"),
Regex.CharacterClass("-"),
Regex.Text("p"),
Regex.CharacterClass("]"),
Regex.CharacterClass("]"),
Regex.Quantifier("+"),
Regex.Quantifier("?"),
Regex.Comment("(?#comment)"),
Regex.Alternation("|"),
Regex.Grouping("("),
Regex.Anchor("\\"),
Regex.Anchor("b"),
Regex.Anchor("\\"),
Regex.Anchor("G"),
Regex.Anchor("\\"),
Regex.Anchor("z"),
Regex.Grouping(")"),
Regex.Alternation("|"),
Regex.Grouping("("),
Regex.Grouping("?"),
Regex.Grouping("<"),
Regex.Grouping("name"),
Regex.Grouping(">"),
Regex.Text("sub"),
Regex.Grouping(")"),
Regex.Quantifier("{"),
Regex.Quantifier("0"),
Regex.Quantifier(","),
Regex.Quantifier("5"),
Regex.Quantifier("}"),
Regex.Quantifier("?"),
Regex.Anchor("^"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex3(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* language=regex */@""$(\a\t\u0020\\)|[^\p{Lu}-a\w\sa-z-[m-p]]+?(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^"";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("t"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("u"),
Regex.OtherEscape("0020"),
Regex.SelfEscapedCharacter("\\"),
Regex.SelfEscapedCharacter("\\"),
Regex.Grouping(")"),
Regex.Alternation("|"),
Regex.CharacterClass("["),
Regex.CharacterClass("^"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("p"),
Regex.CharacterClass("{"),
Regex.CharacterClass("Lu"),
Regex.CharacterClass("}"),
Regex.Text("-a"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("w"),
Regex.CharacterClass("\\"),
Regex.CharacterClass("s"),
Regex.Text("a"),
Regex.CharacterClass("-"),
Regex.Text("z"),
Regex.CharacterClass("-"),
Regex.CharacterClass("["),
Regex.Text("m"),
Regex.CharacterClass("-"),
Regex.Text("p"),
Regex.CharacterClass("]"),
Regex.CharacterClass("]"),
Regex.Quantifier("+"),
Regex.Quantifier("?"),
Regex.Comment("(?#comment)"),
Regex.Alternation("|"),
Regex.Grouping("("),
Regex.Anchor("\\"),
Regex.Anchor("b"),
Regex.Anchor("\\"),
Regex.Anchor("G"),
Regex.Anchor("\\"),
Regex.Anchor("z"),
Regex.Grouping(")"),
Regex.Alternation("|"),
Regex.Grouping("("),
Regex.Grouping("?"),
Regex.Grouping("<"),
Regex.Grouping("name"),
Regex.Grouping(">"),
Regex.Text("sub"),
Regex.Grouping(")"),
Regex.Quantifier("{"),
Regex.Quantifier("0"),
Regex.Quantifier(","),
Regex.Quantifier("5"),
Regex.Quantifier("}"),
Regex.Quantifier("?"),
Regex.Anchor("^"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex4(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* lang=regex */@""$\a(?#comment)"";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex5(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* lang=regexp */@""$\a(?#comment)"";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex6(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* lang=regexp */@""$\a(?#comment) # not end of line comment"";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"),
Regex.Text(" # not end of line comment"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex7(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* lang=regexp,ignorepatternwhitespace */@""$\a(?#comment) # is end of line comment"";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"),
Regex.Comment("# is end of line comment"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex8(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* lang = regexp , ignorepatternwhitespace */@""$\a(?#comment) # is end of line comment"";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"),
Regex.Comment("# is end of line comment"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex9(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = new Regex(@""$\a(?#comment) # is end of line comment"", RegexOptions.IgnorePatternWhitespace);
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Class("Regex"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"),
Regex.Comment("# is end of line comment"),
Enum("RegexOptions"),
EnumMember("IgnorePatternWhitespace"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex10(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = new Regex(@""$\a(?#comment) # is not end of line comment"");
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Class("Regex"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"),
Regex.Text(" # is not end of line comment"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegex11(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    // language=regex
    private static string myRegex = @""$(\a\t\u0020)"";
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("t"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("u"),
Regex.OtherEscape("0020"),
Regex.Grouping(")"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexSingleLineRawStringLiteral(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* lang=regex */ """"""$\a(?#comment)"""""";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexMultiLineRawStringLiteral(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void Goo()
    {
        var r = /* lang=regex */ """"""
            $\a(?#comment)
            """""";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Regex.Text(@"
            "),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"),
Regex.Text(@"
            "));
        }

        [Theory, WorkItem(47079, "https://github.com/dotnet/roslyn/issues/47079")]
        [CombinatorialData]
        public async Task TestRegexWithSpecialCSharpCharLiterals(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    // the double-quote inside the string should not affect this being classified as a regex.
    private Regex myRegex = new Regex(@""^ """" $"";
}",
testHost,
Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Class("Regex"),
Class("Regex"),
Regex.Anchor("^"),
Regex.Text(@" """" "),
Regex.Anchor("$"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_Field(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    [StringSyntax(StringSyntaxAttribute.Regex)]
    private string field;

    void Goo()
    {
        [|this.field = @""$\a(?#comment)"";|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Field("field"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_Property(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    [StringSyntax(StringSyntaxAttribute.Regex)]
    private string Prop { get; set; }

    void Goo()
    {
        [|this.Prop = @""$\a(?#comment)"";|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Property("Prop"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_Argument(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Regex)] string p)
    {
    }

    void Goo()
    {
        [|M(@""$\a(?#comment)"");|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_ParamsArgument(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Regex)] params string[] p)
    {
    }

    void Goo()
    {
        [|M(@""$\a(?#comment)"");|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_ArrayArgument(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Regex)] string[] p)
    {
    }

    void Goo()
    {
        [|M(new string[] { @""$\a(?#comment)"" });|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_ImplicitArrayArgument(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Regex)] string[] p)
    {
    }

    void Goo()
    {
        [|M(new[] { @""$\a(?#comment)"" });|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_CollectionArgument(TestHost testHost)
        {
            await TestAsync(
@"
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Regex)] List<string> p)
    {
    }

    void Goo()
    {
        [|M(new List<string> { @""$\a(?#comment)"" });|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Class("List"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_ImplicitCollectionArgument(TestHost testHost)
        {
            await TestAsync(
@"
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Regex)] List<string> p)
    {
    }

    void Goo()
    {
        [|M(new() { @""$\a(?#comment)"" });|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_Argument_Options(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Regex)] string p, RegexOptions options)
    {
    }

    void Goo()
    {
        [|M(@""$\a(?#comment) # is end of line comment"", RegexOptions.IgnorePatternWhitespace);|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"),
Regex.Comment("# is end of line comment"),
Enum("RegexOptions"),
EnumMember("IgnorePatternWhitespace"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_Attribute(TestHost testHost)
        {
            await TestAsync(
@"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

[AttributeUsage(AttributeTargets.Field)]
class RegexTestAttribute : Attribute
{
    public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string value) { }
}

class Program
{
    [|[RegexTest(@""$\a(?#comment)"")]|]
    private string field;
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Class("RegexTest"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_ParamsAttribute(TestHost testHost)
        {
            await TestAsync(
@"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

[AttributeUsage(AttributeTargets.Field)]
class RegexTestAttribute : Attribute
{
    public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] params string[] value) { }
}

class Program
{
    [|[RegexTest(@""$\a(?#comment)"")]|]
    private string field;
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Class("RegexTest"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_ArrayAttribute(TestHost testHost)
        {
            await TestAsync(
@"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

[AttributeUsage(AttributeTargets.Field)]
class RegexTestAttribute : Attribute
{
    public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string[] value) { }
}

class Program
{
    [|[RegexTest(new string[] { @""$\a(?#comment)"" })]|]
    private string field;
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Class("RegexTest"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestRegexOnApiWithStringSyntaxAttribute_ImplicitArrayAttribute(TestHost testHost)
        {
            await TestAsync(
@"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

[AttributeUsage(AttributeTargets.Field)]
class RegexTestAttribute : Attribute
{
    public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string[] value) { }
}

class Program
{
    [|[RegexTest(new[] { @""$\a(?#comment)"" })]|]
    private string field;
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Class("RegexTest"),
Regex.Anchor("$"),
Regex.OtherEscape("\\"),
Regex.OtherEscape("a"),
Regex.Comment("(?#comment)"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestIncompleteRegexLeadingToStringInsideSkippedTokensInsideADirective(TestHost testHost)
        {
            await TestAsync(
@"
using System.Text.RegularExpressions;

class Program
{
    void M()
    {
        // not terminating this string caused us to eat up to the quote on the next line.
        // we then treated #comment as a directive with a lot of skipped tokens on it, including
        // a skipped token for "";
        //
        // Because it's a comment on a directive, special lexing rules apply (i.e. no escape
        // characters are supposed, and we want our system to bail there and not try to validate
        // it.
        var r = new Regex(@""$;
        var s = /* language=regex */ @""(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^"";
    }
}",
testHost, Namespace("System"),
Namespace("Text"),
Namespace("RegularExpressions"),
Keyword("var"),
Class("Regex"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestJson1(TestHost testHost)
        {
            await TestAsync(
@"
class Program
{
    void Goo()
    {
        // lang=json
        var r = @""[/*comment*/{ 'goo': 0, bar: -Infinity, """"baz"""": true }, new Date(), text, 'str'] // comment"";
    }
}",
testHost,
Keyword("var"),
Json.Array("["),
Json.Comment("/*comment*/"),
Json.Object("{"),
Json.PropertyName("'goo'"),
Json.Punctuation(":"),
Json.Number("0"),
Json.PropertyName("bar"),
Json.Punctuation(":"),
Json.Operator("-"),
Json.Keyword("Infinity"),
Json.PropertyName(@"""""baz"""""),
Json.Punctuation(":"),
Json.Keyword("true"),
Json.Object("}"),
Json.Punctuation(","),
Json.Keyword("new"),
Json.ConstructorName("Date"),
Json.Punctuation("("),
Json.Punctuation(")"),
Json.Punctuation(","),
Json.Text("text"),
Json.Punctuation(","),
Json.String("'str'"),
Json.Array("]"),
Json.Comment("// comment"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestJson_RawString(TestHost testHost)
        {
            await TestAsync(
@"
class Program
{
    void Goo()
    {
        // lang=json
        var r = """"""[/*comment*/{ 'goo': 0 }]"""""";
    }
}",
testHost,
Keyword("var"),
Json.Array("["),
Json.Comment("/*comment*/"),
Json.Object("{"),
Json.PropertyName("'goo'"),
Json.Punctuation(":"),
Json.Number("0"),
Json.Object("}"),
Json.Array("]"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestMultiLineJson1(TestHost testHost)
        {
            await TestAsync(
@"
class Program
{
    void Goo()
    {
        // lang=json
        var r = @""[
            /*comment*/
            {
                'goo': 0,
                bar: -Infinity,
                """"baz"""": true,
                0: null
            },
            new Date(),
            text,
            'str'] // comment"";
    }
}",
testHost,
Keyword("var"),
Json.Array("["),
Json.Comment("/*comment*/"),
Json.Object("{"),
Json.PropertyName("'goo'"),
Json.Punctuation(":"),
Json.Number("0"),
Json.PropertyName("bar"),
Json.Punctuation(":"),
Json.Operator("-"),
Json.Keyword("Infinity"),
Json.PropertyName(@"""""baz"""""),
Json.Punctuation(":"),
Json.Keyword("true"),
Json.PropertyName("0"),
Json.Punctuation(":"),
Json.Keyword("null"),
Json.Object("}"),
Json.Punctuation(","),
Json.Keyword("new"),
Json.ConstructorName("Date"),
Json.Punctuation("("),
Json.Punctuation(")"),
Json.Punctuation(","),
Json.Text("text"),
Json.Punctuation(","),
Json.String("'str'"),
Json.Array("]"),
Json.Comment("// comment"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestJson_NoComment_NotLikelyJson(TestHost testHost)
        {
            var input = @"
class C
{
    void Goo()
    {
        var r = @""[1, 2, 3]"";
    }
}";
            await TestAsync(input,
testHost,
Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestJson_NoComment_LikelyJson(TestHost testHost)
        {
            var input = @"
class C
{
    void Goo()
    {
        var r = @""[1, { prop: 0 }, 3]"";
    }
}";
            await TestAsync(input,
testHost,
Keyword("var"),
Json.Array("["),
Json.Number("1"),
Json.Punctuation(","),
Json.Object("{"),
Json.PropertyName("prop"),
Json.Punctuation(":"),
Json.Number("0"),
Json.Object("}"),
Json.Punctuation(","),
Json.Number("3"),
Json.Array("]"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestJsonOnApiWithStringSyntaxAttribute_Field(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    [StringSyntax(StringSyntaxAttribute.Json)]
    private string field;
    void Goo()
    {
        [|this.field = @""[{ 'goo': 0}]"";|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Field("field"),
Json.Array("["),
Json.Object("{"),
Json.PropertyName("'goo'"),
Json.Punctuation(":"),
Json.Number("0"),
Json.Object("}"),
Json.Array("]"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestJsonOnApiWithStringSyntaxAttribute_Property(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    [StringSyntax(StringSyntaxAttribute.Json)]
    private string Prop { get; set; }
    void Goo()
    {
        [|this.Prop = @""[{ 'goo': 0}]"";|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Property("Prop"),
Json.Array("["),
Json.Object("{"),
Json.PropertyName("'goo'"),
Json.Punctuation(":"),
Json.Number("0"),
Json.Object("}"),
Json.Array("]"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestJsonOnApiWithStringSyntaxAttribute_Argument(TestHost testHost)
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    private void M([StringSyntax(StringSyntaxAttribute.Json)] string p)
    {
    }

    void Goo()
    {
        [|M(@""[{ 'goo': 0}]"");|]
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
testHost,
Method("M"),
Json.Array("["),
Json.Object("{"),
Json.PropertyName("'goo'"),
Json.Punctuation(":"),
Json.Number("0"),
Json.Object("}"),
Json.Array("]"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_LocalFunction_Keyword(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        void M<T>() where T : unmanaged { }
    }
}",
                testHost,
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_LocalFunction_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface unmanaged {}
class X
{
    void N()
    {
        void M<T>() where T : unmanaged { }
    }
}",
                testHost,
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_LocalFunction_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
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
}",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape1(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = ""goo\r\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"\n"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape2(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = @""goo\r\nbar"";",
                testHost,
                Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape3(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo{{1}}bar"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape4(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""goo{{1}}bar"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape5(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo\r{{1}}\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"{{"),
                Escape(@"}}"),
                Escape(@"\n"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape6(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""goo\r{{1}}\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape7(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo\r{1}\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"\n"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""{{goo{1}bar}}"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestStringEscape9(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""{{{12:X}}}"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral1(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""goo\r\nbar"""""";",
                testHost,
                Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral2(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""
    goo\r\nbar
    """""";",
                testHost,
                Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral3(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""""""
    goo\r\nbar
    """""";",
                testHost,
                Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral4(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""\"""""";",
                testHost,
                Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral5(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""
    \
    """""";",
                testHost,
                Keyword("var"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral6(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""""""
    \
    """""";",
                testHost,
                Keyword("var"));
        }

        [WorkItem(31200, "https://github.com/dotnet/roslyn/issues/31200")]
        [Theory]
        [CombinatorialData]
        public async Task TestCharEscape1(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\n';",
                testHost,
                Keyword("var"),
                Escape(@"\n"));
        }

        [WorkItem(31200, "https://github.com/dotnet/roslyn/issues/31200")]
        [Theory]
        [CombinatorialData]
        public async Task TestCharEscape2(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\\';",
                testHost,
                Keyword("var"),
                Escape(@"\\"));
        }

        [WorkItem(31200, "https://github.com/dotnet/roslyn/issues/31200")]
        [Theory]
        [CombinatorialData]
        public async Task TestCharEscape3(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\'';",
                testHost,
                Keyword("var"),
                Escape(@"\'"));
        }

        [WorkItem(31200, "https://github.com/dotnet/roslyn/issues/31200")]
        [Theory]
        [CombinatorialData]
        public async Task TestCharEscape5(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '""';",
                testHost,
                Keyword("var"));
        }

        [WorkItem(31200, "https://github.com/dotnet/roslyn/issues/31200")]
        [Theory]
        [CombinatorialData]
        public async Task TestCharEscape4(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\u000a';",
                testHost,
                Keyword("var"),
                Escape(@"\u000a"));
        }

        [WorkItem(29451, "https://github.com/dotnet/roslyn/issues/29451")]
        [Theory]
        [CombinatorialData]
        public async Task TestDirectiveStringLiteral(TestHost testHost)
            => await TestInMethodAsync(@"#line 1 ""a\b""", testHost);

        [WorkItem(30378, "https://github.com/dotnet/roslyn/issues/30378")]
        [Theory]
        [CombinatorialData]
        public async Task TestFormatSpecifierInInterpolation(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo{{1:0000}}bar"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")]
        [Theory]
        [CombinatorialData]
        public async Task TestOverloadedOperator_BinaryExpression(TestHost testHost)
        {
            await TestAsync(@"
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
}",
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
        }

        [WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")]
        [Theory]
        [CombinatorialData]
        public async Task TestOverloadedOperator_PrefixUnaryExpression(TestHost testHost)
        {
            await TestAsync(@"
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
}",
                testHost,
                Keyword("var"),
                Keyword("var"),
                OverloadedOperators.Exclamation,
                Class("True"),
                Class("True"));
        }

        [WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")]
        [Theory]
        [CombinatorialData]
        public async Task TestOverloadedOperator_PostfixUnaryExpression(TestHost testHost)
        {
            await TestAsync(@"
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
}",
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
        }

        [WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")]
        [Theory]
        [CombinatorialData]
        public async Task TestOverloadedOperator_ConditionalExpression(TestHost testHost)
        {
            await TestAsync(@"
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
}",
                testHost,
                Keyword("var"),
                Keyword("var"),
                Class("True"),
                OverloadedOperators.EqualsEquals,
                Class("True"),
                Class("True"),
                Class("True"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestCatchDeclarationVariable(TestHost testHost)
        {
            await TestInMethodAsync(@"
try
{
}
catch (Exception ex)
{
    throw ex;
}",
                testHost,
                Local("ex"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_InsideMethod(TestHost testHost)
        {
            // Asserts no Keyword("notnull") because it is an identifier.
            await TestInMethodAsync(@"
var notnull = 0;
notnull++;",
                testHost,
                Keyword("var"),
                Local("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Type_Keyword(TestHost testHost)
        {
            await TestAsync(
                "class X<T> where T : notnull { }",
                testHost,
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Type_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface notnull {}
class X<T> where T : notnull { }",
                testHost,
                TypeParameter("T"),
                Interface("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Type_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
class X<T> where T : notnull { }",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Method_Keyword(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void M<T>() where T : notnull { }
}",
                testHost,
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Method_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface notnull {}
class X
{
    void M<T>() where T : notnull { }
}",
                testHost,
                TypeParameter("T"),
                Interface("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Method_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
class X
{
    void M<T>() where T : notnull { }
}",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Delegate_Keyword(TestHost testHost)
        {
            await TestAsync(
                "delegate void D<T>() where T : notnull;",
                testHost,
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Delegate_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface notnull {}
delegate void D<T>() where T : notnull;",
                testHost,
                TypeParameter("T"),
                Interface("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Delegate_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
delegate void D<T>() where T : notnull;",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_LocalFunction_Keyword(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        void M<T>() where T : notnull { }
    }
}",
                testHost,
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_LocalFunction_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface notnull {}
class X
{
    void N()
    {
        void M<T>() where T : notnull { }
    }
}",
                testHost,
                TypeParameter("T"),
                Interface("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_LocalFunction_ExistingInterfaceButOutOfScope(TestHost testHost)
        {
            await TestAsync(@"
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
}",
                testHost,
                Namespace("OtherScope"),
                TypeParameter("T"),
                Keyword("notnull"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NonDiscardVariableDeclaration(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        var _ = int.Parse("""");
    }
}",
            testHost,
            Keyword("var"),
            Method("Parse"),
            Static("Parse"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NonDiscardVariableDeclarationMultipleDeclarators(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        int i = 1, _ = 1;
        int _ = 2, j = 1;
    }
}", testHost);
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardAssignment(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        _ = int.Parse("""");
    }
}",
            testHost,
            Keyword("_"),
            Method("Parse"),
            Static("Parse"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardInOutDeclaration(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        int.TryParse("""", out var _);
    }
}",
            testHost,
            Method("TryParse"),
            Static("TryParse"),
            Keyword("var"),
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardInOutAssignment(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        int.TryParse("""", out _);
    }
}",
            testHost,
            Method("TryParse"),
            Static("TryParse"),
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardInDeconstructionAssignment(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        (x, _) = (0, 0);
    }
}",
            testHost,
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardInDeconstructionDeclaration(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        (int x, int _) = (0, 0);
    }
}",
            testHost,
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardInPatternMatch(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    bool N(object x)
    {
        return x is int _;
    }
}",
            testHost,
            Parameter("x"),
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardInSwitch(TestHost testHost)
        {
            await TestAsync(@"
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
}",
            testHost,
            Parameter("x"),
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardInSwitchPatternMatch(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    bool N(object x)
    {
        return x switch
        {
            _ => return true;
        };
    }
}",
            testHost,
            Parameter("x"),
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task UnusedUnderscoreParameterInLambda(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        System.Func<int, int> a = (int _) => 0;
    }
}",
            testHost,
            Namespace("System"),
            Delegate("Func"));
        }

        [Theory]
        [CombinatorialData]
        public async Task UsedUnderscoreParameterInLambda(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        System.Func<int, int> a = (int _) => _;
    }
}",
            testHost,
            Namespace("System"),
            Delegate("Func"),
            Parameter("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardsInLambda(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        System.Func<int, int, int> a = (int _, int _) => 0;
    }
}",
            testHost,
            Namespace("System"),
            Delegate("Func"),
            Keyword("_"),
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task DiscardsInLambdaWithInferredType(TestHost testHost)
        {
            await TestAsync(@"
class X
{
    void N()
    {
        System.Func<int, int, int> a = (_, _) => 0;
    }
}",
            testHost,
            Namespace("System"),
            Delegate("Func"),
            Keyword("_"),
            Keyword("_"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NativeInteger(TestHost testHost)
        {
            await TestInMethodAsync(
                @"nint i = 0; nuint i2 = 0;",
                testHost,
                Classifications(Keyword("nint"), Keyword("nuint")));
        }

        [Theory]
        [CombinatorialData]
        public async Task NotNativeInteger(TestHost testHost)
        {
            await TestInMethodAsync(
                "nint",
                "M",
                "nint i = 0;",
                testHost,
                Classifications(Class("nint")));
        }

        [Theory]
        [CombinatorialData]
        public async Task NotNativeUnsignedInteger(TestHost testHost)
        {
            await TestInMethodAsync(
                "nuint",
                "M",
                "nuint i = 0;",
                testHost,
                Classifications(Class("nuint")));
        }

        [Theory]
        [CombinatorialData]
        public async Task StaticBoldingMethodName(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    public static void Method()
    {
        System.Action action = Method;
    }
}",
            testHost,
            Namespace("System"),
            Delegate("Action"),
            Method("Method"),
            Static("Method"));
        }

        [Theory]
        [CombinatorialData]
        public async Task StaticBoldingMethodNameNestedInNameof(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    public static void Method()
    {
        _ = nameof(Method);
    }
}",
            testHost,
            Keyword("_"),
            Keyword("nameof"),
            Static("Method"),
            Method("Method"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BoldingMethodNameStaticAndNot(TestHost testHost)
        {
            await TestAsync(
    @"class C
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
}",
            testHost,
            Keyword("_"),
            Keyword("nameof"),
            Static("Method"),
            Method("Method"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(46985, "https://github.com/dotnet/roslyn/issues/46985")]
        public async Task BasicRecordClassification(TestHost testHost)
        {
            await TestAsync(
@"record R
{
    R r;

    R() { }
}",
                testHost,
                Record("R"));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(46985, "https://github.com/dotnet/roslyn/issues/46985")]
        public async Task ParameterizedRecordClassification(TestHost testHost)
        {
            await TestAsync(
@"record R(int X, int Y);

class C
{
    R r;
}",
                testHost,
                Record("R"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BasicRecordClassClassification(TestHost testHost)
        {
            await TestAsync(
@"record class R
{
    R r;

    R() { }
}",
                testHost,
                Record("R"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BasicRecordStructClassification(TestHost testHost)
        {
            await TestAsync(
@"record struct R
{
    R property { get; set; }
}",
                testHost,
                RecordStruct("R"));
        }

        [Theory]
        [CombinatorialData]
        public async Task BasicFileScopedNamespaceClassification(TestHost testHost)
        {
            await TestAsync(
@"namespace NS;

class C { }",
                testHost,
                Namespace("NS"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NullCheckedParameterClassification(TestHost testHost)
        {
            await TestAsync(
@"
class C
{
    void M(string s!!) { }
}",
                testHost);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(57184, "https://github.com/dotnet/roslyn/issues/57184")]
        public async Task MethodGroupClassifications(TestHost testHost)
        {
            await TestAsync(
@"var f = m;
Delegate d = m;
MulticastDelegate md = m;
ICloneable c = m;
object obj = m;
m(m);

int m(Delegate d) { }",
                testHost,
                    Keyword("var"),
                    Method("m"),
                    Method("m"),
                    Method("m"),
                    Method("m"),
                    Method("m"),
                    Method("m"),
                    Method("m"));
        }
    }
}

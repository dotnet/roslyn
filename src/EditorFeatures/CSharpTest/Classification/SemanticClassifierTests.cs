﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class SemanticClassifierTests : AbstractCSharpClassifierTests
    {
        protected override Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, TextSpan span, ParseOptions options)
        {
            using (var workspace = TestWorkspace.CreateCSharp(code, options))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

                return GetSemanticClassificationsAsync(document, span);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericClassDeclaration()
        {
            await TestInMethodAsync(
                className: "Class<T>",
                methodName: "M",
                code: @"new Class<int>();",
                expected: Class("Class"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task RefVar()
        {
            await TestInMethodAsync(
                code: @"int i = 0; ref var x = ref i;",
                expected: Classifications(Keyword("var"), Local("i")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task UsingAlias1()
        {
            await TestAsync(
@"using M = System.Math;",
                Class("M"),
                Class("Math"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsTypeArgument()
        {
            await TestInMethodAsync(
                className: "Class<T>",
                methodName: "M",
                code: @"new Class<dynamic>();",
                expected: Classifications(Class("Class"), Keyword("dynamic")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task UsingTypeAliases()
        {
            var code = @"using Alias = Test; 
class Test { void M() { Test a = new Test(); Alias b = new Alias(); } }";

            await TestAsync(code,
                code,
                Class("Alias"),
                Class("Test"),
                Class("Test"),
                Class("Test"),
                Class("Alias"),
                Class("Alias"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicTypeAlias()
        {
            await TestAsync(
@"using dynamic = System.EventArgs;

class C
{
    dynamic d = new dynamic();
}",
                Class("dynamic"),
                Class("EventArgs"),
                Class("dynamic"),
                Class("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsDelegateName()
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
                Delegate("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsInterfaceName()
        {
            await TestAsync(
@"interface dynamic
{
}

class C
{
    dynamic d;
}",
                Interface("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsEnumName()
        {
            await TestAsync(
@"enum dynamic
{
}

class C
{
    dynamic d;
}",
                Enum("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsClassName()
        {
            await TestAsync(
@"class dynamic
{
}

class C
{
    dynamic d;
}",
                Class("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsClassNameAndLocalVariableName()
        {
            await TestAsync(
@"class dynamic
{
    dynamic()
    {
        dynamic dynamic;
    }
}",
                Class("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsStructName()
        {
            await TestAsync(
@"struct dynamic
{
}

class C
{
    dynamic d;
}",
                Struct("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericClassName()
        {
            await TestAsync(
@"class dynamic<T>
{
}

class C
{
    dynamic<int> d;
}",
                Class("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericClassNameButOtherArity()
        {
            await TestAsync(
@"class dynamic<T>
{
}

class C
{
    dynamic d;
}",
                Keyword("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsUndefinedGenericType()
        {
            await TestAsync(
@"class dynamic
{
}

class C
{
    dynamic<int> d;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsExternAlias()
        {
            await TestAsync(
@"extern alias dynamic;

class C
{
    dynamic::Goo a;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericClassNameButOtherArity()
        {
            await TestAsync(
@"class A<T>
{
}

class C
{
    A d;
}", Class("A"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericTypeParameter()
        {
            await TestAsync(
@"class C<T>
{
    void M()
    {
        default(T) }
}",
                TypeParameter("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericMethodTypeParameter()
        {
            await TestAsync(
@"class C
{
    T M<T>(T t)
    {
        return default(T);
    }
}",
                TypeParameter("T"),
                TypeParameter("T"),
                TypeParameter("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericMethodTypeParameterInLocalVariableDeclaration()
        {
            await TestAsync(
@"class C
{
    void M<T>()
    {
        T t;
    }
}",
                TypeParameter("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ParameterOfLambda1()
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
                Class("C"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ParameterOfAnonymousMethod()
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
                Class("C"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericTypeParameterAfterWhere()
        {
            await TestAsync(
@"class C<A, B> where A : B
{
}",
                TypeParameter("A"),
                TypeParameter("B"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task BaseClass()
        {
            await TestAsync(
@"class C
{
}

class C2 : C
{
}",
                Class("C"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task BaseInterfaceOnInterface()
        {
            await TestAsync(
@"interface T
{
}

interface T2 : T
{
}",
                Interface("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task BaseInterfaceOnClass()
        {
            await TestAsync(
@"interface T
{
}

class T2 : T
{
}",
                Interface("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InterfaceColorColor()
        {
            await TestAsync(
@"interface T
{
}

class T2 : T
{
    T T;
}",
                Interface("T"),
                Interface("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DelegateColorColor()
        {
            await TestAsync(
@"delegate void T();

class T2
{
    T T;
}",
                Delegate("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DelegateReturnsItself()
        {
            await TestAsync(
@"delegate T T();

class C
{
    T T(T t);
}",
                Delegate("T"),
                Delegate("T"),
                Delegate("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task StructColorColor()
        {
            await TestAsync(
@"struct T
{
    T T;
}",
                Struct("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task EnumColorColor()
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
                Enum("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericTypeParameter()
        {
            await TestAsync(
@"class C<dynamic>
{
    dynamic d;
}",
                TypeParameter("dynamic"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericFieldName()
        {
            await TestAsync(
@"class A<T>
{
    T dynamic;
}",
                TypeParameter("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PropertySameNameAsClass()
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
                Class("N"),
                Class("N"),
                Property("N"),
                Property("N"),
                Local("n"),
                Property("N"),
                Property("N"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeWithoutAttributeSuffix()
        {
            await TestAsync(
@"using System;

[Obsolete]
class C
{
}",
                Class("Obsolete"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeOnNonExistingMember()
        {
            await TestAsync(
@"using System;

class A
{
    [Obsolete]
}",
                Class("Obsolete"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeWithoutAttributeSuffixOnAssembly()
        {
            await TestAsync(
@"using System;

[assembly: My]

class MyAttribute : Attribute
{
}",
                Class("My"),
                Class("Attribute"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeViaNestedClassOrDerivedClass()
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
                Class("Base"),
                Class("My"),
                Method("My"),
                Class("Derived"),
                Class("My"),
                Method("My"),
                Class("Attribute"),
                Class("Base"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NamedAndOptional()
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
                Class("C"),
                Method("B"),
                Parameter("C"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PartiallyWrittenGenericName1()
        {
            await TestInMethodAsync(
                className: "Class<T>",
                methodName: "M",
                code: @"Class<int",
                expected: Class("Class"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PartiallyWrittenGenericName2()
        {
            await TestInMethodAsync(
                className: "Class<T1, T2>",
                methodName: "M",
                code: @"Class<int, b",
                expected: Class("Class"));
        }

        // The "Color Color" problem is the C# IDE folklore for when
        // a property name is the same as a type name
        // and the resulting ambiguities that the spec
        // resolves in favor of properties
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor()
        {
            await TestAsync(
@"class Color
{
    Color Color;
}",
                Class("Color"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor2()
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
                Class("T"),
                Class("T"),
                Field("T"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor3()
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
                Class("T"),
                Class("T"),
                Field("T"),
                Method("M"));
        }

        /// <summary>
        /// Instance field should be preferred to type
        /// §7.5.4.1
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor4()
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
                Class("T"),
                Field("T"),
                Field("T"));
        }

        /// <summary>
        /// Type should be preferred to a static field
        /// §7.5.4.1
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor5()
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
                Class("T"),
                Class("T"),
                Field("T"));
        }

        /// <summary>
        /// Needs to prefer the local
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor6()
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
                Class("T"),
                Class("T"),
                Local("T"),
                Field("field"));
        }

        /// <summary>
        /// Needs to prefer the type
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor7()
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
                Class("T"),
                Class("T"),
                Class("T"),
                Field("field"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor8()
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
                Class("T"),
                Class("T"),
                Class("T"),
                Method("M"),
                Local("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor9()
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
                Class("T"),
                Class("T"),
                Parameter("T"),
                Class("T"),
                Parameter("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor10()
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
                Keyword("var"),
                Class("T"),
                Local("T"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor11()
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
                Keyword("var"),
                Local("T"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor12()
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
                Class("T"),
                Class("T"),
                Keyword("var"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor13()
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
                Class("T"),
                Class("T"),
                Class("T"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ColorColor14()
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
                Class("T"),
                Class("T"),
                Class("T"),
                Local("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NamespaceNameSameAsTypeName1()
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
                Class("T"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NamespaceNameSameAsTypeNameWithGlobal()
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
                Class("T"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AmbiguityTypeAsGenericMethodArgumentVsLocal()
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
                TypeParameter("T"),
                Method("M"),
                TypeParameter("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AmbiguityTypeAsGenericArgumentVsLocal()
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
                Class("T"),
                Class("G"),
                Class("T"),
                Class("G"),
                Class("T"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AmbiguityTypeAsGenericArgumentVsField()
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
                Class("T"),
                Class("H"),
                Class("T"),
                Field("f"));
        }

        /// <summary>
        /// §7.5.4.2
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GrammarAmbiguity_7_5_4_2()
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
                Method("F"),
                Method("G"),
                Class("A"),
                Class("B"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AnonymousTypePropertyName()
        {
            await TestAsync(
@"using System;

class C
{
    void M()
    {
        var x = new { String = "" }; } }",
                Keyword("var"),
                Property("String"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task YieldAsATypeName()
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
                Interface("IEnumerable"),
                Class("yield"),
                Class("yield"),
                Class("yield"),
                Local("yield"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TypeNameDottedNames()
        {
            await TestAsync(
@"class C
{
    class Nested
    {
    }

    C.Nested f;
}",
                Class("C"),
                Class("Nested"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task BindingTypeNameFromBCLViaGlobalAlias()
        {
            await TestAsync(
@"using System;

class C
{
    global::System.String f;
}",
                Class("String"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task BindingTypeNames()
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
            await TestAsync(code,
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TypesOfClassMembers()
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQTypeNameCtor()
        {
            await TestInMethodAsync(
@"System.IO.BufferedStream b = new global::System.IO.BufferedStream();",
                Class("BufferedStream"),
                Class("BufferedStream"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQEnum()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        global::System.IO.DriveType d;
    }
}",
                Enum("DriveType"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQDelegate()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        global::System.AssemblyLoadEventHandler d;
    }
}",
                Delegate("AssemblyLoadEventHandler"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQTypeNameMethodCall()
        {
            await TestInMethodAsync(@"global::System.String.Clone("");",
                Class("String"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQEventSubscription()
        {
            await TestInMethodAsync(
@"global::System.AppDomain.CurrentDomain.AssemblyLoad += 
            delegate (object sender, System.AssemblyLoadEventArgs args) {};",
                Class("AppDomain"),
                Property("CurrentDomain"),
                Event("AssemblyLoad"),
                Class("AssemblyLoadEventArgs"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AnonymousDelegateParameterType()
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
                Delegate("Action"),
                Class("EventArgs"),
                Class("EventArgs"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQCtor()
        {
            await TestInMethodAsync(
@"global::System.Collections.DictionaryEntry de = new global::System.Collections.DictionaryEntry();",
                Struct("DictionaryEntry"),
                Struct("DictionaryEntry"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQSameFileClass()
        {
            var code = @"class C { static void M() { global::C.M(); } }";

            await TestAsync(code,
                ParseOptions(Options.Regular),
                Class("C"),
                Method("M"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InteractiveNAQSameFileClass()
        {
            var code = @"class C { static void M() { global::Script.C.M(); } }";

            await TestAsync(code,
                ParseOptions(Options.Script),
                Class("Script"),
                Class("C"),
                Method("M"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQSameFileClassWithNamespace()
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
                Class("C"),
                Method("M"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQSameFileClassWithNamespaceAndEscapedKeyword()
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
                Class("C"),
                Method("M"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQGlobalWarning()
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
                Class("C"),
                Method("M"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQUserDefinedNAQNamespace()
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
                Class("C"),
                Method("M"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQUserDefinedNAQNamespaceDoubleColon()
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
                Class("C"),
                Method("M"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQUserDefinedNamespace1()
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
                Class("D"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQUserDefinedNamespaceWithGlobal()
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
                Class("D"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQUserDefinedNAQForClass()
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
                Class("BinaryReader"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NAQUserDefinedTypes()
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
                Class("MyClass2"),
                Method("method"),
                Class("MyClass2"),
                Event("myEvent"),
                Enum("MyEnum"),
                Struct("MyStruct"),
                Class("MyClass2"),
                Property("MyProp"),
                Class("MyClass2"),
                Field("myField"),
                Class("MyClass2"),
                Delegate("MyDelegate"),
                Delegate("MyDelegate"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PreferPropertyOverNestedClass()
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
                Class("A"),
                Class("A"),
                Local("a"),
                Field("B"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TypeNameInsideNestedClass()
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
                Class("Console"),
                Method("WriteLine"),
                Class("Console"),
                Method("WriteLine"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task StructEnumTypeNames()
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
                Enum("ConsoleColor"),
                Struct("Int32"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PreferFieldOverClassWithSameName()
        {
            await TestAsync(
@"class C
{
    public int C;

    void M()
    {
        C = 0;
    }
}", Field("C"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeBinding()
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

[NonSerialized]           // Binds to global::NonSerializedAttribute; not colorized
class NonSerializedAttribute
{
}

[NonSerializedAttribute]  // Binds to global::NonSerializedAttribute; not colorized
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
                Class("Serializable"),
                Class("SerializableAttribute"),
                Class("Obsolete"),
                Class("Attribute"),
                Class("ObsoleteAttribute"),
                Class("Attribute"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ShouldNotClassifyNamespacesAsTypes()
        {
            await TestAsync(
@"using System;

namespace Roslyn.Compilers.Internal
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NestedTypeCantHaveSameNameAsParentType()
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
                Class("Program"),
                Class("Program"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias()
        {
            var code = @"class Program
{
    class Program { }
    static void Main(Program p) { }
    global::Program.Program p;
}";

            await TestAsync(code,
                ParseOptions(Options.Regular),
                Class("Program"),
                Class("Program"),
                Class("Program"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InteractiveNestedTypeCantHaveSameNameAsParentTypeWithGlobalNamespaceAlias()
        {
            var code = @"class Program
{
    class Program { }
    static void Main(Program p) { }
    global::Script.Program.Program p;
}";

            await TestAsync(code,
                ParseOptions(Options.Script),
                Class("Program"),
                Class("Script"),
                Class("Program"),
                Class("Program"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task EnumFieldWithSameNameShouldBePreferredToType()
        {
            await TestAsync(
@"enum E
{
    E,
    F = E
}", EnumMember("E"));
        }

        [WorkItem(541150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541150")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestGenericVarClassification()
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
}", Keyword("var"));
        }

        [WorkItem(541154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestInaccessibleVarClassification()
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
                Class("A"),
                Keyword("var"));
        }

        [WorkItem(541154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVarNamedTypeClassification()
        {
            await TestAsync(
@"class var
{
    static void Main()
    {
        var x;
    }
}",
                Class("var"));
        }

        [WorkItem(9513, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task RegressionFor9513()
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task RegressionFor9572()
        {
            await TestAsync(
@"class A<T, S> where T : A<T, S>.I, A<T, T>.I
{
    public interface I
    {
    }
}",
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task RegressionFor9831()
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
                Class("A"));
        }

        [WorkItem(542432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542432")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVar()
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
                Class("var"),
                Keyword("var"),
                Method("GetVarT"),
                Keyword("var"),
                Class("var"));
        }

        [WorkItem(543123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVar2()
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
                Keyword("var"),
                Parameter("args"));
        }

        [WorkItem(542778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542778")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestDuplicateTypeParamWithConstraint()
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
                TypeParameter("U"),
                Interface("IEnumerable"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task OptimisticallyColorFromInDeclaration()
        {
            await TestInExpressionAsync("from ",
                Keyword("from"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task OptimisticallyColorFromInAssignment()
        {
            await TestInMethodAsync(
@"var q = 3;

q = from",
                Keyword("var"),
                Local("q"),
                Keyword("from"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DontColorThingsOtherThanFromInDeclaration()
        {
            await TestInExpressionAsync("fro ");
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DontColorThingsOtherThanFromInAssignment()
        {
            await TestInMethodAsync(
@"var q = 3;

q = fro",
                Keyword("var"),
                Local("q"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DontColorFromWhenBoundInDeclaration()
        {
            await TestInMethodAsync(
@"var from = 3;
var q = from",
                Keyword("var"),
                Keyword("var"),
                Local("from"));
        }

        [WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DontColorFromWhenBoundInAssignment()
        {
            await TestInMethodAsync(
@"var q = 3;
var from = 3;

q = from",
                Keyword("var"),
                Keyword("var"),
                Local("q"),
                Local("from"));
        }

        [WorkItem(543404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543404")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NewOfClassWithOnlyPrivateConstructor()
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
                Class("X"));
        }

        [WorkItem(544179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544179")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNullableVersusConditionalAmbiguity1()
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
                Class("C1"));
        }

        [WorkItem(544179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544179")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestPointerVersusMultiplyAmbiguity1()
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
                Class("C1"));
        }

        [WorkItem(544302, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544302")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task EnumTypeAssignedToNamedPropertyOfSameNameInAttributeCtor()
        {
            await TestAsync(
@"using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""abc"", CallingConvention = CallingConvention)]
    static extern void M();
}",
                Class("DllImport"),
                Field("CallingConvention"),
                Enum("CallingConvention"));
        }

        [WorkItem(531119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531119")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task OnlyClassifyGenericNameOnce()
        {
            await TestAsync(
@"enum Type
{
}

struct Type<T>
{
    Type<int> f;
}",
                Struct("Type"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NameOf1()
        {
            await TestAsync(
@"class C
{
    void goo()
    {
        var x = nameof
    }
}",
                Keyword("var"),
                Keyword("nameof"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NameOf2()
        {
            await TestAsync(
@"class C
{
    void goo()
    {
        var x = nameof(C);
    }
}",
                Keyword("var"),
                Keyword("nameof"),
                Class("C"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task MethodCalledNameOfInScope()
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
                Keyword("var"));
        }

        [WorkItem(744813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/744813")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestCreateWithBufferNotInWorkspace()
        {
            // don't crash
            using (var workspace = TestWorkspace.CreateCSharp(""))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

                var contentTypeService = document.GetLanguageService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();
                var extraBuffer = workspace.ExportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer("", contentType);

                WpfTestCase.RequireWpfFact("Creates an IWpfTextView explicitly with an unrelated buffer");
                using (var disposableView = workspace.ExportProvider.GetExportedValue<ITextEditorFactoryService>().CreateDisposableTextView(extraBuffer))
                {
                    var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

                    var provider = new SemanticClassificationViewTaggerProvider(
                        workspace.ExportProvider.GetExportedValue<IForegroundNotificationService>(),
                        workspace.ExportProvider.GetExportedValue<ISemanticChangeNotificationService>(),
                        workspace.ExportProvider.GetExportedValue<ClassificationTypeMap>(),
                        listenerProvider);

                    using (var tagger = (IDisposable)provider.CreateTagger<IClassificationTag>(disposableView.TextView, extraBuffer))
                    {
                        using (var edit = extraBuffer.CreateEdit())
                        {
                            edit.Insert(0, "class A { }");
                            edit.Apply();
                        }

                        var waiter = listenerProvider.GetWaiter(FeatureAttribute.Classification);
                        await waiter.CreateWaitTask();
                    }
                }
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestGetTagsOnBufferTagger()
        {
            // don't crash
            using (var workspace = TestWorkspace.CreateCSharp("class C { C c; }"))
            {
                var document = workspace.Documents.First();

                var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

                var provider = new SemanticClassificationBufferTaggerProvider(
                    workspace.ExportProvider.GetExportedValue<IForegroundNotificationService>(),
                    workspace.ExportProvider.GetExportedValue<ISemanticChangeNotificationService>(),
                    workspace.ExportProvider.GetExportedValue<ClassificationTypeMap>(),
                    listenerProvider);

                var tagger = provider.CreateTagger<IClassificationTag>(document.TextBuffer);
                using (var disposable = (IDisposable)tagger)
                {
                    var waiter = listenerProvider.GetWaiter(FeatureAttribute.Classification);
                    await waiter.CreateWaitTask();

                    var tags = tagger.GetTags(document.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection());
                    var allTags = tagger.GetAllTags(document.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection(), CancellationToken.None);

                    Assert.Empty(tags);
                    Assert.NotEmpty(allTags);

                    Assert.Equal(allTags.Count(), 1);
                }
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task Tuples()
        {
            await TestAsync(
@"class C
{
    (int a, int b) x;
}",
                ParseOptions(TestOptions.Regular, Options.Script));
        }

        [Fact]
        [WorkItem(261049, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/261049")]
        public async Task DevDiv261049RegressionTest()
        {
            var source = @"
        var (a,b) =  Get(out int x, out int y);
        Console.WriteLine($""({a.first}, {a.second})"");";

            await TestInMethodAsync(
                source,
                Keyword("var"), Local("a"), Local("a"));
        }

        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InXmlDocCref_WhenTypeOnlyIsSpecified_ItIsClassified()
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
    Class("MyClass"));
        }

        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InXmlDocCref_WhenConstructorOnlyIsSpecified_NothingIsClassified()
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
}", Method("MyClass"));
        }

        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InXmlDocCref_WhenTypeAndConstructorSpecified_OnlyTypeIsClassified()
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
    Class("MyClass"),
    Method("MyClass"));
        }

        [WorkItem(13174, "https://github.com/dotnet/roslyn/issues/13174")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestMemberBindingThatLooksGeneric()
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
}", Class("Debug"), Method("Assert"), Parameter("args"), Property("Length"));
        }

        [WorkItem(18956, "https://github.com/dotnet/roslyn/issues/18956")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVarInPattern1()
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
}", Parameter("s"), Keyword("var"));
        }

        [WorkItem(18956, "https://github.com/dotnet/roslyn/issues/18956")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVarInPattern2()
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
}", Parameter("s"), Keyword("var"));
        }

        [WorkItem(23940, "https://github.com/dotnet/roslyn/issues/23940")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestAliasQualifiedClass()
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
    Keyword("var"), Class("List"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_InsideMethod()
        {
            // Asserts no Keyword("unmanaged") because it is an identifier.
            await TestInMethodAsync(@"
var unmanaged = 0;
unmanaged++;",
                Keyword("var"),
                Local("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_Keyword()
        {
            await TestAsync(
                "class X<T> where T : unmanaged { }",
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
class X<T> where T : unmanaged { }",
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
class X<T> where T : unmanaged { }",
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_Keyword()
        {
            await TestAsync(@"
class X
{
    void M<T>() where T : unmanaged { }
}",
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
class X
{
    void M<T>() where T : unmanaged { }
}",
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_ExistingInterfaceButOutOfScope()
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
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_Keyword()
        {
            await TestAsync(
                "delegate void D<T>() where T : unmanaged;",
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
delegate void D<T>() where T : unmanaged;",
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
delegate void D<T>() where T : unmanaged;",
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_LocalFunction_Keyword()
        {
            await TestAsync(@"
class X
{
    void N()
    {
        void M<T>() where T : unmanaged { }
    }
}",
                TypeParameter("T"),
                Keyword("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_LocalFunction_ExistingInterface()
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
                TypeParameter("T"),
                Interface("unmanaged"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_LocalFunction_ExistingInterfaceButOutOfScope()
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
                TypeParameter("T"),
                Keyword("unmanaged"));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

[Trait(Traits.Feature, Traits.Features.Classification)]
public sealed partial class TotalClassifierTests : AbstractCSharpClassifierTests
{
    protected override async Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, ImmutableArray<TextSpan> spans, ParseOptions? options, TestHost testHost)
    {
        using var workspace = CreateWorkspace(code, options, testHost);
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);

        return await GetAllClassificationsAsync(document, spans);
    }

    [Theory, CombinatorialData]
    public Task VarAsUsingAliasForNamespace(TestHost testHost)
        => TestAsync(
            @"using var = System;",
            testHost,
            Keyword("using"),
            Namespace("var"),
            Operators.Equals,
            Namespace("System"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547068")]
    public Task Bug17819(TestHost testHost)
        => TestAsync(
            """
            _ _()
            {
            }
            ///<param name='_
            }
            """,
            testHost,
            ParseOptions(Options.Regular),
            Method("_"),
            Method("_"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("param"),
            XmlDoc.AttributeName("name"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("'"),
            Identifier("_"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task VarAsUsingAliasForClass(TestHost testHost)
        => TestAsync(
@"using var = System.Math;",
            testHost,
            Keyword("using"),
            Class("var"),
            Operators.Equals,
            Namespace("System"),
            Operators.Dot,
            Class("Math"),
            Static("Math"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task VarAsUsingAliasForDelegate(TestHost testHost)
        => TestAsync(
@"using var = System.Action;",
            testHost,
            Keyword("using"),
            Delegate("var"),
            Operators.Equals,
            Namespace("System"),
            Operators.Dot,
            Delegate("Action"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task VarAsUsingAliasForStruct(TestHost testHost)
        => TestAsync(
@"using var = System.DateTime;",
            testHost,
            Keyword("using"),
            Struct("var"),
            Operators.Equals,
            Namespace("System"),
            Operators.Dot,
            Struct("DateTime"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task VarAsUsingAliasForEnum(TestHost testHost)
        => TestAsync(
@"using var = System.DayOfWeek;",
            testHost,
            Keyword("using"),
            Enum("var"),
            Operators.Equals,
            Namespace("System"),
            Operators.Dot,
            Enum("DayOfWeek"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task VarAsUsingAliasForInterface(TestHost testHost)
        => TestAsync(
@"using var = System.IDisposable;",
            testHost,
            Keyword("using"),
            Interface("var"),
            Operators.Equals,
            Namespace("System"),
            Operators.Dot,
            Interface("IDisposable"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task VarAsConstructorName(TestHost testHost)
        => TestAsync(
            """
            class var
            {
                var()
                {
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("var"),
            Punctuation.OpenCurly,
            Class("var"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRecordClass(TestHost testHost)
        => TestAsync(
            """
            record class R
            {
                R()
                {
                }
            }
            """,
            testHost,
            Keyword("record"),
            Keyword("class"),
            RecordClass("R"),
            Punctuation.OpenCurly,
            RecordClass("R"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRecordStruct(TestHost testHost)
        => TestAsync(
            """
            record struct R
            {
                R(int i)
                {
                }
            }
            """,
            testHost,
            Keyword("record"),
            Keyword("struct"),
            RecordStruct("R"),
            Punctuation.OpenCurly,
            RecordStruct("R"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("i"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task UsingAliasGlobalNamespace(TestHost testHost)
        => TestAsync(
@"using IO = global::System.IO;",
            testHost,
            Keyword("using"),
            Namespace("IO"),
            Operators.Equals,
            Keyword("global"),
            Operators.ColonColon,
            Namespace("System"),
            Operators.Dot,
            Namespace("IO"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task PartialDynamicWhere(TestHost testHost)
        => TestAsync("""
            partial class partial<where> where where : partial<where>
            {
                static dynamic dynamic<partial>()
                {
                    return dynamic<dynamic>();
                }
            }
            """,
            testHost,
            Keyword("partial"),
            Keyword("class"),
            Class("partial"),
            Punctuation.OpenAngle,
            TypeParameter("where"),
            Punctuation.CloseAngle,
            Keyword("where"),
            TypeParameter("where"),
            Punctuation.Colon,
            Class("partial"),
            Punctuation.OpenAngle,
            TypeParameter("where"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Keyword("static"),
            Keyword("dynamic"),
            Method("dynamic"),
            Static("dynamic"),
            Punctuation.OpenAngle,
            TypeParameter("partial"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("return"),
            Method("dynamic"),
            Static("dynamic"),
            Punctuation.OpenAngle,
            Keyword("dynamic"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
    public Task VarInForeach(TestHost testHost)
        => TestInMethodAsync(@"foreach (var v in args) { }",
            testHost,
            ControlKeyword("foreach"),
            Punctuation.OpenParen,
            Keyword("var"),
            Local("v"),
            ControlKeyword("in"),
            Identifier("args"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ValueInSetterAndAnonymousTypePropertyName(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                int P
                {
                    set
                    {
                        var t = new { value = value };
                    }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Property("P"),
            Punctuation.OpenCurly,
            Keyword("set"),
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("t"),
            Operators.Equals,
            Keyword("new"),
            Punctuation.OpenCurly,
            Property("value"),
            Operators.Equals,
            Keyword("value"),
            Punctuation.CloseCurly,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestValueInEvent(TestHost testHost)
        => TestInClassAsync(
            """
            event int Bar
            {
                add
                {
                    this.value = value;
                }

                remove
                {
                    this.value = value;
                }
            }
            """,
            testHost,
            Keyword("event"),
            Keyword("int"),
            Event("Bar"),
            Punctuation.OpenCurly,
            Keyword("add"),
            Punctuation.OpenCurly,
            Keyword("this"),
            Operators.Dot,
            Identifier("value"),
            Operators.Equals,
            Keyword("value"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,

            Keyword("remove"),
            Punctuation.OpenCurly,
            Keyword("this"),
            Operators.Dot,
            Identifier("value"),
            Operators.Equals,
            Keyword("value"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestValueInProperty(TestHost testHost)
        => TestInClassAsync(
            """
            int Goo
            {
                get
                {
                    this.value = value;
                }

                set
                {
                    this.value = value;
                }
            }
            """,
            testHost,
            Keyword("int"),
            Property("Goo"),
            Punctuation.OpenCurly,
            Keyword("get"),
            Punctuation.OpenCurly,
            Keyword("this"),
            Operators.Dot,
            Identifier("value"),
            Operators.Equals,
            Identifier("value"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,

            Keyword("set"),
            Punctuation.OpenCurly,
            Keyword("this"),
            Operators.Dot,
            Identifier("value"),
            Operators.Equals,
            Keyword("value"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ValueFieldInSetterAccessedThroughThis(TestHost testHost)
        => TestInClassAsync(
            """
            int P
            {
                set
                {
                    this.value = value;
                }
            }
            """,
            testHost,
            Keyword("int"),
            Property("P"),
            Punctuation.OpenCurly,
            Keyword("set"),
            Punctuation.OpenCurly,
            Keyword("this"),
            Operators.Dot,
            Identifier("value"),
            Operators.Equals,
            Keyword("value"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task NewOfInterface(TestHost testHost)
        => TestInMethodAsync(
@"object o = new System.IDisposable();",
            testHost,
            Keyword("object"),
            Local("o"),
            Operators.Equals,
            Keyword("new"),
            Namespace("System"),
            Operators.Dot,
            Interface("IDisposable"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545611")]
    [CombinatorialData]
    public Task TestVarConstructor(TestHost testHost)
        => TestAsync(
            """
            class var
            {
                void Main()
                {
                    new var();
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("var"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Main"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("new"),
            Class("var"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545609")]
    [CombinatorialData]
    public Task TestVarTypeParameter(TestHost testHost)
        => TestAsync(
            """
            class X
            {
                void Goo<var>()
                {
                    var x;
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenAngle,
            TypeParameter("var"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("x"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545610")]
    [CombinatorialData]
    public Task TestVarAttribute1(TestHost testHost)
        => TestAsync(
            """
            using System;

            [var]
            class var : Attribute
            {
            }
            """,
            testHost,
            Keyword("using"),
            Namespace("System"),
            Punctuation.Semicolon,
            Punctuation.OpenBracket,
            Class("var"),
            Punctuation.CloseBracket,
            Keyword("class"),
            Class("var"),
            Punctuation.Colon,
            Class("Attribute"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545610")]
    [CombinatorialData]
    public Task TestVarAttribute2(TestHost testHost)
        => TestAsync(
            """
            using System;

            [var]
            class varAttribute : Attribute
            {
            }
            """,
            testHost,
            Keyword("using"),
            Namespace("System"),
            Punctuation.Semicolon,
            Punctuation.OpenBracket,
            Class("var"),
            Punctuation.CloseBracket,
            Keyword("class"),
            Class("varAttribute"),
            Punctuation.Colon,
            Class("Attribute"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546170")]
    [CombinatorialData]
    public Task TestStandaloneTypeName(TestHost testHost)
        => TestAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    var tree = Console
                }
            }
            """,
            testHost,
            Keyword("using"),
            Namespace("System"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("static"),
            Keyword("void"),
            Method("Main"),
            Static("Main"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("tree"),
            Operators.Equals,
            Class("Console"),
            Static("Console"),
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")]
    [CombinatorialData]
    public Task TestNamespaceClassAmbiguities(TestHost testHost)
        => TestAsync(
            """
            class C
            {
            }

            namespace C
            {
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("namespace"),
            Namespace("C"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task NameAttributeValue(TestHost testHost)
        => TestAsync(
            """
            class Program<T>
            {
                /// <param name="x"/>
                void Goo(int x)
                {
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("param"),
            XmlDoc.AttributeName("name"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            Parameter("x"),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task Cref1(TestHost testHost)
        => TestAsync(
            """
            /// <see cref="Program{T}"/>
            class Program<T>
            {
                void Goo()
                {
                }
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("see"),
            XmlDoc.AttributeName("cref"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            Class("Program"),
            Punctuation.OpenCurly,
            TypeParameter("T"),
            Punctuation.CloseCurly,
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CrefNamespaceIsNotClass(TestHost testHost)
        => TestAsync(
            """
            ///  <see cref="N"/>
            namespace N
            {
                class Program
                {
                }
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text("  "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("see"),
            XmlDoc.AttributeName("cref"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            Namespace("N"),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            Keyword("namespace"),
            Namespace("N"),
            Punctuation.OpenCurly,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task InterfacePropertyWithSameNameShouldBePreferredToType(TestHost testHost)
        => TestAsync(
            """
            interface IGoo
            {
                int IGoo { get; set; }

                void Bar(int x = IGoo);
            }
            """,
            testHost,
            Keyword("interface"),
            Interface("IGoo"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Property("IGoo"),
            Punctuation.OpenCurly,
            Keyword("get"),
            Punctuation.Semicolon,
            Keyword("set"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("void"),
            Method("Bar"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Operators.Equals,
            Property("IGoo"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/633")]
    public Task XmlDocCref(TestHost testHost)
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
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("see"),
            XmlDoc.AttributeName("cref"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            Class("MyClass"),
            Operators.Dot,
            Class("MyClass"),
            Punctuation.OpenParen,
            Keyword("int"),
            Punctuation.CloseParen,
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("MyClass"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Class("MyClass"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestGenericTypeWithNoArity(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class Program : IReadOnlyCollection
            {
            }
            """,
            testHost,
            Keyword("using"),
            Namespace("System"),
            Operators.Dot,
            Namespace("Collections"),
            Operators.Dot,
            Namespace("Generic"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("Program"),
            Punctuation.Colon,
            Interface("IReadOnlyCollection"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestGenericTypeWithWrongArity(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class Program : IReadOnlyCollection<int,string>
            {
            }
            """,
            testHost,
            Keyword("using"),
            Namespace("System"),
            Operators.Dot,
            Namespace("Collections"),
            Operators.Dot,
            Namespace("Generic"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("Program"),
            Punctuation.Colon,
            Interface("IReadOnlyCollection"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.Comma,
            Keyword("string"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestExtensionMethodDeclaration(TestHost testHost)
        => TestAsync(
            """
            static class ExtMethod
            {
                public static void TestMethod(this C c)
                {
                }
            }
            """,
            testHost,
            Keyword("static"),
            Keyword("class"),
            Class("ExtMethod"),
            Static("ExtMethod"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            ExtensionMethod("TestMethod"),
            Static("TestMethod"),
            Punctuation.OpenParen,
            Keyword("this"),
            Identifier("C"),
            Parameter("c"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestExtensionMethodUsage(TestHost testHost)
        => TestAsync(
            """
            static class ExtMethod
            {
                public static void TestMethod(this C c)
                {
                }
            }

            class C
            {
                void Test()
                {
                    ExtMethod.TestMethod(new C());
                    new C().TestMethod();
                }
            }
            """,
            testHost,
            ParseOptions(Options.Regular),
            Keyword("static"),
            Keyword("class"),
            Class("ExtMethod"),
            Static("ExtMethod"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            ExtensionMethod("TestMethod"),
            Static("TestMethod"),
            Punctuation.OpenParen,
            Keyword("this"),
            Class("C"),
            Parameter("c"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Test"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Class("ExtMethod"),
            Static("ExtMethod"),
            Operators.Dot,
            Method("TestMethod"),
            Static("TestMethod"),
            Punctuation.OpenParen,
            Keyword("new"),
            Class("C"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("new"),
            Class("C"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Operators.Dot,
            ExtensionMethod("TestMethod"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestConstLocals(TestHost testHost)
        => TestInMethodAsync(
            """
            const int Number = 42;
            var x = Number;
            """,
            testHost,
            Keyword("const"),
            Keyword("int"),
            Constant("Number"),
            Static("Number"),
            Operators.Equals,
            Number("42"),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("x"),
            Operators.Equals,
            Constant("Number"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_InsideMethod(TestHost testHost)
        => TestInMethodAsync("""
            var unmanaged = 0;
            unmanaged++;
            """,
            testHost,
            Keyword("var"),
            Local("unmanaged"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon,
            Local("unmanaged"),
            Operators.PlusPlus,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_Keyword(TestHost testHost)
        => TestAsync(
            "class X<T> where T : unmanaged { }",
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface unmanaged {}
            class X<T> where T : unmanaged { }
            """,
            testHost,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Method_Keyword(TestHost testHost)
        => TestAsync("""
            class X
            {
                void M<T>() where T : unmanaged { }
            }
            """,
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_Keyword(TestHost testHost)
        => TestAsync(
            "delegate void D<T>() where T : unmanaged;",
            testHost,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface unmanaged {}
            delegate void D<T>() where T : unmanaged;
            """,
            testHost,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.Semicolon);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.Semicolon);

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
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/29492")]
    [CombinatorialData]
    public Task TestOperatorOverloading(TestHost testHost)
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
Keyword("class"),
Class("C"),
Punctuation.OpenCurly,
Keyword("void"),
Method("M"),
Punctuation.OpenParen,
Punctuation.CloseParen,
Punctuation.OpenCurly,
Keyword("var"),
Local("a"),
Operators.Equals,
Number("1"),
Operators.Plus,
Number("1"),
Punctuation.Semicolon,
Keyword("var"),
Local("b"),
Operators.Equals,
Keyword("new"),
Class("True"),
Punctuation.OpenParen,
Punctuation.CloseParen,
OverloadedOperators.Plus,
Keyword("new"),
Class("True"),
Punctuation.OpenParen,
Punctuation.CloseParen,
Punctuation.Semicolon,
Punctuation.CloseCurly,
Punctuation.CloseCurly,
Keyword("class"),
Class("True"),
Punctuation.OpenCurly,
Keyword("public"),
Keyword("static"),
Class("True"),
Keyword("operator"),
Operators.Plus,
Punctuation.OpenParen,
Class("True"),
Parameter("a"),
Punctuation.Comma,
Class("True"),
Parameter("b"),
Punctuation.CloseParen,
Punctuation.OpenCurly,
ControlKeyword("return"),
Keyword("new"),
Class("True"),
Punctuation.OpenParen,
Punctuation.CloseParen,
Punctuation.Semicolon,
Punctuation.CloseCurly,
Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_InsideMethod(TestHost testHost)
        => TestInMethodAsync("""
            var notnull = 0;
            notnull++;
            """,
            testHost,
            Keyword("var"),
            Local("notnull"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon,
            Local("notnull"),
            Operators.PlusPlus,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_Keyword(TestHost testHost)
        => TestAsync(
            "class X<T> where T : notnull { }",
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface notnull {}
            class X<T> where T : notnull { }
            """,
            testHost,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Method_Keyword(TestHost testHost)
        => TestAsync("""
            class X
            {
                void M<T>() where T : notnull { }
            }
            """,
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_Keyword(TestHost testHost)
        => TestAsync(
            "delegate void D<T>() where T : notnull;",
            testHost,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_ExistingInterface(TestHost testHost)
        => TestAsync("""
            interface notnull {}
            delegate void D<T>() where T : notnull;
            """,
            testHost,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.Semicolon);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.Semicolon);

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
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

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
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            TypeParameter("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/10174")]
    [CombinatorialData]
    public Task VarInPropertyPattern(TestHost testHost)
        => TestAsync(
            """
            using System;

            class Person { public string Name; }

            class Program
            {
                void Goo(object o)
                {
                    if (o is Person { Name: var n })
                    {
                        Console.WriteLine(n);
                    }
                }
            }
            """,
            testHost,
            Keyword("using"),
            Namespace("System"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("Person"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("string"),
            Field("Name"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Keyword("object"),
            Parameter("o"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Parameter("o"),
            Keyword("is"),
            Class("Person"),
            Punctuation.OpenCurly,
            Field("Name"),
            Punctuation.Colon,
            Keyword("var"),
            Local("n"),
            Punctuation.CloseCurly,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Class("Console"),
            Static("Console"),
            Operators.Dot,
            Method("WriteLine"),
            Static("WriteLine"),
            Punctuation.OpenParen,
            Local("n"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    [CombinatorialData]
    public Task NotPattern(TestHost testHost)
        => TestAsync(
            """
            class Person
            {
                void Goo(object o)
                {
                    if (o is not Person p)
                    {
                    }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Person"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Keyword("object"),
            Parameter("o"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Parameter("o"),
            Keyword("is"),
            Keyword("not"),
            Class("Person"),
            Local("p"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    [CombinatorialData]
    public Task OrPattern(TestHost testHost)
        => TestAsync(
            """
            class Person
            {
                void Goo(object o)
                {
                    if (o is Person or int)
                    {
                    }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Person"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Keyword("object"),
            Parameter("o"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Parameter("o"),
            Keyword("is"),
            Class("Person"),
            Keyword("or"),
            Keyword("int"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/59484")]
    [CombinatorialData]
    public Task TestPatternVariables(TestHost testHost)
        => TestAsync(
            """
            void M(object o) {
                _ = o is [var (x, y), {} z] list;
            }
            """,
            testHost,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Keyword("object"),
            Parameter("o"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("_"),
            Operators.Equals,
            Parameter("o"),
            Keyword("is"),
            Punctuation.OpenBracket,
            Keyword("var"),
            Punctuation.OpenParen,
            Local("x"),
            Punctuation.Comma,
            Local("y"),
            Punctuation.CloseParen,
            Punctuation.Comma,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Local("z"),
            Punctuation.CloseBracket,
            Local("list"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    [CombinatorialData]
    public Task RelationalPattern(TestHost testHost)
        => TestAsync(
            """
            class Person
            {
                void Goo(object o)
                {
                    if (o is >= 0)
                    {
                    }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Person"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Keyword("object"),
            Parameter("o"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Parameter("o"),
            Keyword("is"),
            Operators.GreaterThanEquals,
            NumericLiteral("0"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task BasicFileScopedNamespaceClassification(TestHost testHost)
        => TestAsync(
            """
            namespace NS;

            class C { }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("NS"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestStringEscape(TestHost testHost)
        => TestInMethodAsync(@"var goo = $@""{{{12:X}}}"";",
            testHost,
            Keyword("var"),
            Local("goo"),
            Operators.Equals,
            Verbatim("""
                $@"
                """),
            Escape("{{"),
            Punctuation.OpenCurly,
            Number("12"),
            Punctuation.Colon,
            String("X"),
            Punctuation.CloseCurly,
            Escape("}}"),
            Verbatim("""
                "
                """),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/55313")]
    public Task TestStaticConstructorClass(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                static C() { }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("static"),
            Class("C"),
            Static("C"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/55313")]
    public Task TestStaticConstructorInterface(TestHost testHost)
        => TestAsync(
            """
            interface C
            {
                static C() { }
            }
            """,
            testHost,
            Keyword("interface"),
            Interface("C"),
            Punctuation.OpenCurly,
            Keyword("static"),
            Interface("C"),
            Static("C"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/59569")]
    public Task TestArgsInTopLevel(TestHost testHost)
        => TestAsync(
            """
            [|foreach (var arg in args)
            {
            }|]
            """,
            testHost,
            parseOptions: null,
            ControlKeyword("foreach"),
            Punctuation.OpenParen,
            Keyword("var"),
            Local("arg"),
            ControlKeyword("in"),
            Keyword("args"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/59569")]
    public Task TestArgsInNormalProgram(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|foreach (var arg in args)
                    {
                    }|]
                }
            }
            """,
            testHost,
            parseOptions: null,
            ControlKeyword("foreach"),
            Punctuation.OpenParen,
            Keyword("var"),
            Local("arg"),
            ControlKeyword("in"),
            Parameter("args"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncInIncompleteMember(TestHost testHost)
        => TestAsync(
            """
            class Test
            {
                public async
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("class"),
            Class("Test"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("async"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncInIncompleteMemberWhenAsyncTypeIsDefined(TestHost testHost)
        => TestAsync(
            """
            [|class Test
            {
                public async
            }|]

            class async
            {
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("class"),
            Class("Test"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Class("async"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncInPotentialLocalFunctionDeclaration(TestHost testHost)
        => TestAsync(
            """
            void M()
            {
                async
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("async"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncInPotentialLocalFunctionDeclarationWhenAsyncTypeIsDefined(TestHost testHost)
        => TestAsync(
            """
            [|void M()
            {
                async
            }|]

            class async
            {
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Class("async"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncAsLocalMemberType_NoAsyncInScope(TestHost testHost)
        => TestAsync(
            """
            class Test
            {
                void M()
                {
                    [|async a;|]
                }
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("async"),
            Local("a"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncAsLocalMemberType_AsyncInScope(TestHost testHost)
        => TestAsync(
            """
            class async { }

            class Test
            {
                void M()
                {
                    [|async a;|]
                }
            }
            """,
            testHost,
            parseOptions: null,
            Class("async"),
            Local("a"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncAsPropertyType_NoAsyncInScope(TestHost testHost)
        => TestAsync(
            """
            class Test
            {
                [|public async Prop { get; set; }|]
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("public"),
            Keyword("async"),
            Property("Prop"),
            Punctuation.OpenCurly,
            Keyword("get"),
            Punctuation.Semicolon,
            Keyword("set"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncAsPropertyType_AsyncInScope(TestHost testHost)
        => TestAsync(
            """
            class async { }

            class Test
            {
                [|public async Prop { get; set; }|]
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("public"),
            Class("async"),
            Property("Prop"),
            Punctuation.OpenCurly,
            Keyword("get"),
            Punctuation.Semicolon,
            Keyword("set"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncAsMethodReturnType_NoAsyncInScope(TestHost testHost)
        => TestAsync(
            """
            class Test
            {
                [|public async M()|] {}
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("public"),
            Keyword("async"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncAsMethodReturnType_AsyncInScope(TestHost testHost)
        => TestAsync(
            """
            class async { }

            class Test
            {
                [|public async M()|] {}
            }
            """,
            testHost,
            parseOptions: null,
            Keyword("public"),
            Class("async"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncAsAccessingName(TestHost testHost)
        => TestAsync(
            """
            class Test
            {
                void M()
                {
                    var a = [|C.async;|]
                }
            }

            class C
            {
                public static int async;
            }
            """,
            testHost,
            parseOptions: null,
            Class("C"),
            Operators.Dot,
            Field("async"),
            Static("async"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem(60399, "https://github.com/dotnet/roslyn/issues/60339")]
    public Task TestAsyncInIncompleteDelegateOrLambda(TestHost testHost)
        => TestAsync(
            """
            using System;
            class Test
            {
                void M()
                {
                    [|Action a = async |]
                }
            }
            """,
            testHost,
            parseOptions: null,
            Delegate("Action"),
            Local("a"),
            Operators.Equals,
            Keyword("async"));

    [Theory, CombinatorialData]
    public Task TestPartialInIncompleteMember1(TestHost testHost)
        => TestAsync("""
            class C
            {
                [|partial|]
            }
            """,
            testHost,
            Keyword("partial"));

    [Theory, CombinatorialData]
    public Task TestPartialInIncompleteMember2(TestHost testHost)
        => TestAsync("""
            class C
            {
                [|public partial|]
            }
            """,
            testHost,
            Keyword("public"),
            Keyword("partial"));

    [Theory, CombinatorialData]
    public Task TestPartialInIncompleteMember1_PartialTypeIsDefined(TestHost testHost)
        => TestAsync("""
            class partial
            {
            }

            class C
            {
                [|partial|]
            }
            """,
            testHost,
            Class("partial"));

    [Theory, CombinatorialData]
    public Task TestPartialInIncompleteMember2_PartialTypeIsDefined(TestHost testHost)
        => TestAsync("""
            class partial
            {
            }

            class C
            {
                [|public partial|]
            }
            """,
            testHost,
            Keyword("public"),
            Class("partial"));

    [Theory, CombinatorialData]
    public Task TestTopLevelPartial1(TestHost testHost)
        => TestAsync("""
            partial
            """,
            testHost,
            Keyword("partial"));

    [Theory, CombinatorialData]
    public Task TestTopLevelPartial2(TestHost testHost)
        => TestAsync("""
            public partial
            """,
            testHost,
            Keyword("public"),
            Keyword("partial"));

    [Theory, CombinatorialData]
    public Task TestTopLevelPartial1_PartialTypeIsDefined(TestHost testHost)
        => TestAsync("""
            class partial
            {
            }

            [|partial|]
            """,
            testHost,
            Class("partial"));

    [Theory, CombinatorialData]
    public Task TestTopLevelPartial2_PartialTypeIsDefined(TestHost testHost)
        => TestAsync("""
            class partial
            {
            }

            [|public partial|]
            """,
            testHost,
            Keyword("public"),
            Class("partial"));

    /// <seealso cref="SemanticClassifierTests.LocalFunctionUse"/>
    /// <seealso cref="SyntacticClassifierTests.LocalFunctionDeclaration"/>
    [Theory, CombinatorialData]
    public Task LocalFunctionDeclarationAndUse(TestHost testHost)
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
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Method("staticLocalFunction"),
            Static("staticLocalFunction"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Method("M"),
            Punctuation.OpenParen,
            Method("localFunction"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Method("M"),
            Punctuation.OpenParen,
            Method("staticLocalFunction"),
            Static("staticLocalFunction"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("void"),
            Method("localFunction"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("static"),
            Keyword("void"),
            Method("staticLocalFunction"),
            Static("staticLocalFunction"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestScopedVar(TestHost testHost)
        => TestAsync("""
            static void method(scoped in S s)
            {
                scoped var rs1 = s;
            }

            file readonly ref struct S { }
            """, testHost,
            Keyword("static"),
            Keyword("void"),
            Method("method"),
            Static("method"),
            Punctuation.OpenParen,
            Keyword("scoped"),
            Keyword("in"),
            Struct("S"),
            Parameter("s"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("scoped"),
            Keyword("var"),
            Local("rs1"),
            Operators.Equals,
            Parameter("s"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("file"),
            Keyword("readonly"),
            Keyword("ref"),
            Keyword("struct"),
            Struct("S"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task Lambda_DefaultParameterValue(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                const int N = 10;

                void M()
                {
                    var lam = [|(int x = N) => x|];
                }
            }

            """,
            testHost,
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Operators.Equals,
            Constant("N"),
            Static("N"),
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Parameter("x"));

    [Theory, CombinatorialData]
    public Task UsingAliasToType1(TestHost testHost)
        => TestAsync(
            """
            using X = int;
            """,
            testHost,
            Keyword("using"),
            Struct("X"),
            Operators.Equals,
            Keyword("int"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task UsingAliasToType2(TestHost testHost)
        => TestAsync(
            """
            using X = int[];
            """,
            testHost,
            Keyword("using"),
            Array("X"),
            Operators.Equals,
            Keyword("int"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task UsingAliasToType3(TestHost testHost)
        => TestAsync(
            """
            using unsafe X = int*;
            """,
            testHost,
            Keyword("using"),
            Keyword("unsafe"),
            Pointer("X"),
            Operators.Equals,
            Keyword("int"),
            Operators.Asterisk,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task UsingAliasToType4(TestHost testHost)
        => TestAsync(
            """
            using unsafe X = delegate*<int,int>;
            """,
            testHost,
            Keyword("using"),
            Keyword("unsafe"),
            FunctionPointer("X"),
            Operators.Equals,
            Keyword("delegate"),
            Operators.Asterisk,
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.Comma,
            Keyword("int"),
            Punctuation.CloseAngle,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task UsingAliasToType5(TestHost testHost)
        => TestAsync(
            """
            using X = (int x, string b);
            """,
            testHost,
            Keyword("using"),
            Struct("X"),
            Operators.Equals,
            Punctuation.OpenParen,
            Keyword("int"),
            Identifier("x"),
            Punctuation.Comma,
            Keyword("string"),
            Identifier("b"),
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/70107")]
    public Task TestFunctionPointer1(TestHost testHost)
        => TestAsync(
            """
            delegate* unmanaged[Fastcall, Stdcall, Thiscall]<int> fp;
            """,
            testHost,
            parseOptions: null,
            Keyword("delegate"),
            Operators.Asterisk,
            Keyword("unmanaged"),
            Punctuation.OpenBracket,
            Class("Fastcall"),
            Punctuation.Comma,
            Class("Stdcall"),
            Punctuation.Comma,
            Class("Thiscall"),
            Punctuation.CloseBracket,
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.CloseAngle,
            Local("fp"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/70107")]
    public Task TestFunctionPointer2(TestHost testHost)
        => TestAsync(
            """
            delegate* unmanaged[Member]<int> fp;
            """,
            testHost,
            parseOptions: null,
            Keyword("delegate"),
            Operators.Asterisk,
            Keyword("unmanaged"),
            Punctuation.OpenBracket,
            Identifier("Member"),
            Punctuation.CloseBracket,
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.CloseAngle,
            Local("fp"),
            Punctuation.Semicolon);

    [WpfFact]
    public async Task TestTotalClassifier()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(""""
            using System.Text.RegularExpressions;

            class C
            {
                // class D { }
                void M()
                {
                    new Regex("(a)");
                    var s1 = "s1";
                    var s2 = $"s2";
                    var s3 = @"s3";
                    var s4 = """
                    s4
                    """;
                }
            }
            """");
        var document = workspace.Documents.First();

        var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();
        var globalOptions = workspace.ExportProvider.GetExportedValue<IGlobalOptionService>();

        var provider = new TotalClassificationTaggerProvider(
            workspace.GetService<TaggerHost>(),
            workspace.GetService<ClassificationTypeMap>());

        var buffer = document.GetTextBuffer();
        using var tagger = provider.CreateTagger(document.GetTextView(), buffer);

        var waiter = listenerProvider.GetWaiter(FeatureAttribute.Classification);
        await waiter.ExpeditedWaitAsync();

        var allCode = buffer.CurrentSnapshot.GetText();
        var tags = tagger!.GetTags(new NormalizedSnapshotSpanCollection(buffer.CurrentSnapshot.GetFullSpan()));

        var actualOrdered = tags.OrderBy((t1, t2) => t1.Span.Span.Start - t2.Span.Span.Start);

        var actualFormatted = actualOrdered.Select(a => new FormattedClassification(allCode.Substring(a.Span.Span.Start, a.Span.Span.Length), a.Tag.ClassificationType.Classification));

        AssertEx.Equal(
        [
            Keyword("using"),
            Namespace("System"),
            Operators.Dot,
            Namespace("Text"),
            Operators.Dot,
            Namespace("RegularExpressions"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Comment("// class D { }"),
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("new"),
            Class("Regex"),
            Punctuation.OpenParen,
            String("""
                "
                """),
            Regex.Grouping("("),
            Regex.Text("a"),
            Regex.Grouping(")"),
            String("""
                "
                """),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s1"),
            Operators.Equals,
            String("""
                "s1"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s2"),
            Operators.Equals,
            String("""
                $"
                """),
            String("s2"),
            String("""
                "
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s3"),
            Operators.Equals,
            Verbatim("""
                @"s3"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s4"),
            Operators.Equals,
            String(""""
                """
                        s4
                        """
                """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
        ], actualFormatted);
    }

    [WpfFact]
    public void TestCopyPasteClassifier()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(""""
            using System.Text.RegularExpressions;

            class C
            {
                // class D { }
                void M()
                {
                    new Regex("(a)");
                    var s1 = "s1";
                    var s2 = $"s2";
                    var s3 = @"s3";
                    var s4 = """
                    s4
                    """;
                }
            }
            """");
        var document = workspace.Documents.First();

        var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();
        var globalOptions = workspace.ExportProvider.GetExportedValue<IGlobalOptionService>();

        var provider = new CopyPasteAndPrintingClassificationBufferTaggerProvider(
            workspace.GetService<IThreadingContext>(),
            workspace.GetService<ClassificationTypeMap>(),
            listenerProvider,
            globalOptions);

        var buffer = document.GetTextBuffer();
        using var tagger = provider.CreateTagger<IClassificationTag>(buffer);

        var allCode = buffer.CurrentSnapshot.GetText();
        var tags = tagger!.GetAllTags(new NormalizedSnapshotSpanCollection(buffer.CurrentSnapshot.GetFullSpan()), CancellationToken.None);

        var actualOrdered = tags.OrderBy((t1, t2) => t1.Span.Span.Start - t2.Span.Span.Start);

        var actualFormatted = actualOrdered.Select(a => new FormattedClassification(allCode.Substring(a.Span.Span.Start, a.Span.Span.Length), a.Tag.ClassificationType.Classification));

        AssertEx.Equal(
        [
            Keyword("using"),
            Namespace("System"),
            Operators.Dot,
            Namespace("Text"),
            Operators.Dot,
            Namespace("RegularExpressions"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Comment("// class D { }"),
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("new"),
            Class("Regex"),
            Punctuation.OpenParen,
            String("""
                "
                """),
            Regex.Grouping("("),
            Regex.Text("a"),
            Regex.Grouping(")"),
            String("""
                "
                """),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s1"),
            Operators.Equals,
            String("""
                "s1"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s2"),
            Operators.Equals,
            String("""
                $"
                """),
            String("s2"),
            String("""
                "
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s3"),
            Operators.Equals,
            Verbatim("""
                @"s3"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("s4"),
            Operators.Equals,
            String(""""
                """
                        s4
                        """
                """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
        ], actualFormatted);
    }

    [Theory, CombinatorialData]
    public Task TestModernExtension1(TestHost testHost)
        => TestAsync(
            """
            static class C
            {
                extension(string s)
                {
                    public bool IsNullOrEmpty() => false;
                }

                void M(string s)
                {
                    var v = s.IsNullOrEmpty();
                }
            }
            """,
            testHost,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp14),
            Keyword("static"),
            Keyword("class"),
            Class("C"),
            Static("C"),
            Punctuation.OpenCurly,
            Keyword("extension"),
            Punctuation.OpenParen,
            Keyword("string"),
            Parameter("s"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("bool"),
            ExtensionMethod("IsNullOrEmpty"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Keyword("false"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Keyword("string"),
            Parameter("s"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("v"),
            Operators.Equals,
            Parameter("s"),
            Operators.Dot,
            ExtensionMethod("IsNullOrEmpty"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task DocCommentCodeBlockWithCSharpLang(TestHost testHost)
        => TestAsync(
            """
            /// <summary>
            /// Test method
            /// </summary>
            [|/// <code lang="C#">
            /// using System;
            /// class C { int x = 5; }
            /// </code>|]
            class Program
            {
                void Test() { }
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("code"),
            XmlDoc.AttributeName("lang"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("\""),
            XmlDoc.AttributeValue("C#"),
            XmlDoc.AttributeQuotes("\""),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            Keyword("using"),
            Namespace("System"),
            Punctuation.Semicolon,
            XmlDoc.Delimiter("///"),
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Field("x"),
            Operators.Equals,
            Number("5"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("code"),
            XmlDoc.Delimiter(">"));

    [Theory, CombinatorialData]
    public Task DocCommentCodeBlockWithCSharpLangCData(TestHost testHost)
        => TestAsync(
            """
            /// <summary>
            /// Test method
            /// </summary>
            [|/// <code lang="C#"><![CDATA[
            /// using System.Collections.Generic;
            /// class C { IList<int> x = 5; }
            /// ]]></code>|]
            class Program
            {
                void Test() { }
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("code"),
            XmlDoc.AttributeName("lang"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("\""),
            XmlDoc.AttributeValue("C#"),
            XmlDoc.AttributeQuotes("\""),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("<![CDATA["),
            XmlDoc.Delimiter("///"),
            Keyword("using"),
            Namespace("System"),
            Operators.Dot,
            Namespace("Collections"),
            Operators.Dot,
            Namespace("Generic"),
            Punctuation.Semicolon,
            XmlDoc.Delimiter("///"),
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Interface("IList"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.CloseAngle,
            Field("x"),
            Operators.Equals,
            Number("5"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("]]>"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("code"),
            XmlDoc.Delimiter(">"));

    [Theory, CombinatorialData]
    public Task DocCommentCodeBlockWithCSharpLangGenerics(TestHost testHost)
        => TestAsync(
            """
            /// <summary>
            /// Test method
            /// </summary>
            [|/// <code lang="C#">
            /// using System.Collections.Generic;
            /// class C { IList&lt;int&gt; x = 5; }
            /// </code>|]
            class Program
            {
                void Test() { }
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("code"),
            XmlDoc.AttributeName("lang"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("\""),
            XmlDoc.AttributeValue("C#"),
            XmlDoc.AttributeQuotes("\""),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            Keyword("using"),
            Namespace("System"),
            Operators.Dot,
            Namespace("Collections"),
            Operators.Dot,
            Namespace("Generic"),
            Punctuation.Semicolon,
            XmlDoc.Delimiter("///"),
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Interface("IList"),
            Punctuation.XmlOpenAngle,
            Keyword("int"),
            Punctuation.XmlCloseAngle,
            Field("x"),
            Operators.Equals,
            Number("5"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("code"),
            XmlDoc.Delimiter(">"));

    [Theory, CombinatorialData]
    public Task TestWithElement(TestHost testHost)
        => TestAsync(
            """
            var v = [with(0)]
            """,
            testHost,
            parseOptions: null,
            Keyword("var"),
            Local("v"),
            Operators.Equals,
            Punctuation.OpenBracket,
            Keyword("with"),
            Punctuation.OpenParen,
            Number("0"),
            Punctuation.CloseParen,
            Punctuation.CloseBracket);
}

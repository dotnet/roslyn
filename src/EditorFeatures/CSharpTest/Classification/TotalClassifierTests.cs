﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    [Trait(Traits.Feature, Traits.Features.Classification)]
    public partial class TotalClassifierTests : AbstractCSharpClassifierTests
    {
        protected override async Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, TextSpan span, ParseOptions options, TestHost testHost)
        {
            using var workspace = CreateWorkspace(code, options, testHost);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

            return await GetAllClassificationsAsync(document, span);
        }

        [Theory]
        [CombinatorialData]
        public async Task VarAsUsingAliasForNamespace(TestHost testHost)
        {
            await TestAsync(
@"using var = System;",
                testHost,
                Keyword("using"),
                Namespace("var"),
                Operators.Equals,
                Namespace("System"),
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(547068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547068")]
        public async Task Bug17819(TestHost testHost)
        {
            await TestAsync(
@"_ _()
{
}
///<param name='_
}",
                testHost,
                Identifier("_"),
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
        }

        [Theory]
        [CombinatorialData]
        public async Task VarAsUsingAliasForClass(TestHost testHost)
        {
            await TestAsync(
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
        }

        [Theory]
        [CombinatorialData]
        public async Task VarAsUsingAliasForDelegate(TestHost testHost)
        {
            await TestAsync(
@"using var = System.Action;",
                testHost,
                Keyword("using"),
                Delegate("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Delegate("Action"),
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        public async Task VarAsUsingAliasForStruct(TestHost testHost)
        {
            await TestAsync(
@"using var = System.DateTime;",
                testHost,
                Keyword("using"),
                Struct("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Struct("DateTime"),
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        public async Task VarAsUsingAliasForEnum(TestHost testHost)
        {
            await TestAsync(
@"using var = System.DayOfWeek;",
                testHost,
                Keyword("using"),
                Enum("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Enum("DayOfWeek"),
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        public async Task VarAsUsingAliasForInterface(TestHost testHost)
        {
            await TestAsync(
@"using var = System.IDisposable;",
                testHost,
                Keyword("using"),
                Interface("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Interface("IDisposable"),
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        public async Task VarAsConstructorName(TestHost testHost)
        {
            await TestAsync(
@"class var
{
    var()
    {
    }
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task UsingAliasGlobalNamespace(TestHost testHost)
        {
            await TestAsync(
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
        }

        [Theory]
        [CombinatorialData]
        public async Task PartialDynamicWhere(TestHost testHost)
        {
            var code = @"partial class partial<where> where where : partial<where>
{
    static dynamic dynamic<partial>()
    {
        return dynamic<dynamic>();
    }
}
";
            await TestAsync(code,
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
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(543123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
        public async Task VarInForeach(TestHost testHost)
        {
            await TestInMethodAsync(@"foreach (var v in args) { }",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task ValueInSetterAndAnonymousTypePropertyName(TestHost testHost)
        {
            await TestAsync(
@"class C
{
    int P
    {
        set
        {
            var t = new { value = value };
        }
    }
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestValueInEvent(TestHost testHost)
        {
            await TestInClassAsync(
@"event int Bar
{
    add
    {
        this.value = value;
    }

    remove
    {
        this.value = value;
    }
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestValueInProperty(TestHost testHost)
        {
            await TestInClassAsync(
@"int Goo
{
    get
    {
        this.value = value;
    }

    set
    {
        this.value = value;
    }
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task ValueFieldInSetterAccessedThroughThis(TestHost testHost)
        {
            await TestInClassAsync(
@"int P
{
    set
    {
        this.value = value;
    }
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task NewOfInterface(TestHost testHost)
        {
            await TestInMethodAsync(
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
        }

        [WorkItem(545611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545611")]
        [Theory]
        [CombinatorialData]
        public async Task TestVarConstructor(TestHost testHost)
        {
            await TestAsync(
@"class var
{
    void Main()
    {
        new var();
    }
}",
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
        }

        [WorkItem(545609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545609")]
        [Theory]
        [CombinatorialData]
        public async Task TestVarTypeParameter(TestHost testHost)
        {
            await TestAsync(
@"class X
{
    void Goo<var>()
    {
        var x;
    }
}",
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
                TypeParameter("var"),
                Local("x"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WorkItem(545610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545610")]
        [Theory]
        [CombinatorialData]
        public async Task TestVarAttribute1(TestHost testHost)
        {
            await TestAsync(
@"using System;

[var]
class var : Attribute
{
}",
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
        }

        [WorkItem(545610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545610")]
        [Theory]
        [CombinatorialData]
        public async Task TestVarAttribute2(TestHost testHost)
        {
            await TestAsync(
@"using System;

[var]
class varAttribute : Attribute
{
}",
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
        }

        [WorkItem(546170, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546170")]
        [Theory]
        [CombinatorialData]
        public async Task TestStandaloneTypeName(TestHost testHost)
        {
            await TestAsync(
@"using System;

class C
{
    static void Main()
    {
        var tree = Console
    }
}",
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
        }

        [WorkItem(546403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")]
        [Theory]
        [CombinatorialData]
        public async Task TestNamespaceClassAmbiguities(TestHost testHost)
        {
            await TestAsync(
@"class C
{
}

namespace C
{
}",
                testHost,
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Keyword("namespace"),
                Namespace("C"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Theory]
        [CombinatorialData]
        public async Task NameAttributeValue(TestHost testHost)
        {
            await TestAsync(
@"class Program<T>
{
    /// <param name=""x""/>
    void Goo(int x)
    {
    }
}",
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
                XmlDoc.AttributeQuotes("\""),
                Parameter("x"),
                XmlDoc.AttributeQuotes("\""),
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
        }

        [Theory]
        [CombinatorialData]
        public async Task Cref1(TestHost testHost)
        {
            await TestAsync(
@"/// <see cref=""Program{T}""/>
class Program<T>
{
    void Goo()
    {
    }
}",
                testHost,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("see"),
                XmlDoc.AttributeName("cref"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes("\""),
                Class("Program"),
                Punctuation.OpenCurly,
                TypeParameter("T"),
                Punctuation.CloseCurly,
                XmlDoc.AttributeQuotes("\""),
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
        }

        [Theory]
        [CombinatorialData]
        public async Task CrefNamespaceIsNotClass(TestHost testHost)
        {
            await TestAsync(
@"///  <see cref=""N""/>
namespace N
{
    class Program
    {
    }
}",
                testHost,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text("  "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("see"),
                XmlDoc.AttributeName("cref"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes("\""),
                Namespace("N"),
                XmlDoc.AttributeQuotes("\""),
                XmlDoc.Delimiter("/>"),
                Keyword("namespace"),
                Namespace("N"),
                Punctuation.OpenCurly,
                Keyword("class"),
                Class("Program"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [Theory]
        [CombinatorialData]
        public async Task InterfacePropertyWithSameNameShouldBePreferredToType(TestHost testHost)
        {
            await TestAsync(
@"interface IGoo
{
    int IGoo { get; set; }

    void Bar(int x = IGoo);
}",
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
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        public async Task XmlDocCref(TestHost testHost)
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
                XmlDoc.AttributeQuotes("\""),
                Class("MyClass"),
                Operators.Dot,
                Class("MyClass"),
                Punctuation.OpenParen,
                Keyword("int"),
                Punctuation.CloseParen,
                XmlDoc.AttributeQuotes("\""),
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestGenericTypeWithNoArity(TestHost testHost)
        {
            await TestAsync(
@"
using System.Collections.Generic;

class Program : IReadOnlyCollection
{
}",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestGenericTypeWithWrongArity(TestHost testHost)
        {
            await TestAsync(
@"
using System.Collections.Generic;

class Program : IReadOnlyCollection<int,string>
{
}",
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
                Identifier("IReadOnlyCollection"),
                Punctuation.OpenAngle,
                Keyword("int"),
                Punctuation.Comma,
                Keyword("string"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestExtensionMethodDeclaration(TestHost testHost)
        {
            await TestAsync(
@"static class ExtMethod
{
    public static void TestMethod(this C c)
    {
    }
}
",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestExtensionMethodUsage(TestHost testHost)
        {
            await TestAsync(
@"static class ExtMethod
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
",
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestConstLocals(TestHost testHost)
        {
            await TestInMethodAsync(
@"const int Number = 42;
var x = Number;",
                testHost,
                Keyword("const"),
                Keyword("int"),
                Constant("Number"),
                Operators.Equals,
                Number("42"),
                Punctuation.Semicolon,
                Keyword("var"),
                Local("x"),
                Operators.Equals,
                Constant("Number"),
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_InsideMethod(TestHost testHost)
        {
            await TestInMethodAsync(@"
var unmanaged = 0;
unmanaged++;",
                testHost,
                Keyword("var"),
                Local("unmanaged"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon,
                Local("unmanaged"),
                Operators.PlusPlus,
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Type_Keyword(TestHost testHost)
        {
            await TestAsync(
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Type_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface unmanaged {}
class X<T> where T : unmanaged { }",
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
                Interface("unmanaged"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
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
                Interface("unmanaged"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Delegate_Keyword(TestHost testHost)
        {
            await TestAsync(
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface unmanaged {}
delegate void D<T>() where T : unmanaged;",
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
                Interface("unmanaged"),
                Punctuation.Semicolon);
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
                Interface("unmanaged"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
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
        }

        [WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")]
        [Theory]
        [CombinatorialData]
        public async Task TestOperatorOverloading(TestHost testHost)
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_InsideMethod(TestHost testHost)
        {
            await TestInMethodAsync(@"
var notnull = 0;
notnull++;",
                testHost,
                Keyword("var"),
                Local("notnull"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon,
                Local("notnull"),
                Operators.PlusPlus,
                Punctuation.Semicolon);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Type_Keyword(TestHost testHost)
        {
            await TestAsync(
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Type_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface notnull {}
class X<T> where T : notnull { }",
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
                Interface("notnull"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
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
                Interface("notnull"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Delegate_Keyword(TestHost testHost)
        {
            await TestAsync(
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
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotNullConstraint_Delegate_ExistingInterface(TestHost testHost)
        {
            await TestAsync(@"
interface notnull {}
delegate void D<T>() where T : notnull;",
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
                Interface("notnull"),
                Punctuation.Semicolon);
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
                Interface("notnull"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
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
        }

        [WorkItem(10174, "https://github.com/dotnet/roslyn/issues/10174")]
        [Theory]
        [CombinatorialData]
        public async Task VarInPropertyPattern(TestHost testHost)
        {
            await TestAsync(
@"
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
}",
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
                Identifier("n"),
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
        }

        [WorkItem(42368, "https://github.com/dotnet/roslyn/issues/42368")]
        [Theory]
        [CombinatorialData]
        public async Task NotPattern(TestHost testHost)
        {
            await TestAsync(
@"
class Person
{
    void Goo(object o)
    {
        if (o is not Person p)
        {
        }
    }
}",
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
        }

        [WorkItem(42368, "https://github.com/dotnet/roslyn/issues/42368")]
        [Theory]
        [CombinatorialData]
        public async Task OrPattern(TestHost testHost)
        {
            await TestAsync(
@"
class Person
{
    void Goo(object o)
    {
        if (o is Person or int)
        {
        }
    }
}",
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
        }

        [WorkItem(42368, "https://github.com/dotnet/roslyn/issues/42368")]
        [Theory]
        [CombinatorialData]
        public async Task RelationalPattern(TestHost testHost)
        {
            await TestAsync(
@"
class Person
{
    void Goo(object o)
    {
        if (o is >= 0)
        {
        }
    }
}",
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
        }
    }
}

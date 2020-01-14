// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class TotalClassifierTests : AbstractCSharpClassifierTests
    {
        protected override Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, TextSpan span, ParseOptions options)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, options);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

            return GetAllClassificationsAsync(document, span);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsUsingAliasForNamespace()
        {
            await TestAsync(
@"using var = System;",
                Keyword("using"),
                Namespace("var"),
                Operators.Equals,
                Namespace("System"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification), WorkItem(547068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547068")]
        public async Task Bug17819()
        {
            await TestAsync(
@"_ _()
{
}
///<param name='_
}",
                Identifier("_"),
                Method("_"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                XmlDoc.Delimiter("///"),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("param"),
                XmlDoc.AttributeName(" "),
                XmlDoc.AttributeName("name"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes("'"),
                Identifier("_"),
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsUsingAliasForClass()
        {
            await TestAsync(
@"using var = System.Math;",
                Keyword("using"),
                Class("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Class("Math"),
                Static("Math"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsUsingAliasForDelegate()
        {
            await TestAsync(
@"using var = System.Action;",
                Keyword("using"),
                Delegate("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Delegate("Action"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsUsingAliasForStruct()
        {
            await TestAsync(
@"using var = System.DateTime;",
                Keyword("using"),
                Struct("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Struct("DateTime"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsUsingAliasForEnum()
        {
            await TestAsync(
@"using var = System.DayOfWeek;",
                Keyword("using"),
                Enum("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Enum("DayOfWeek"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsUsingAliasForInterface()
        {
            await TestAsync(
@"using var = System.IDisposable;",
                Keyword("using"),
                Interface("var"),
                Operators.Equals,
                Namespace("System"),
                Operators.Dot,
                Interface("IDisposable"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsConstructorName()
        {
            await TestAsync(
@"class var
{
    var()
    {
    }
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task UsingAliasGlobalNamespace()
        {
            await TestAsync(
@"using IO = global::System.IO;",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PartialDynamicWhere()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(543123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
        public async Task VarInForeach()
        {
            await TestInMethodAsync(@"foreach (var v in args) { }",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ValueInSetterAndAnonymousTypePropertyName()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestValueInEvent()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestValueInProperty()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ValueFieldInSetterAccessedThroughThis()
        {
            await TestInClassAsync(
@"int P
{
    set
    {
        this.value = value;
    }
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NewOfInterface()
        {
            await TestInMethodAsync(
@"object o = new System.IDisposable();",
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVarConstructor()
        {
            await TestAsync(
@"class var
{
    void Main()
    {
        new var();
    }
}",
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVarTypeParameter()
        {
            await TestAsync(
@"class X
{
    void Goo<var>()
    {
        var x;
    }
}",
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVarAttribute1()
        {
            await TestAsync(
@"using System;

[var]
class var : Attribute
{
}",
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVarAttribute2()
        {
            await TestAsync(
@"using System;

[var]
class varAttribute : Attribute
{
}",
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestStandaloneTypeName()
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNamespaceClassAmbiguities()
        {
            await TestAsync(
@"class C
{
}

namespace C
{
}",
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Keyword("namespace"),
                Namespace("C"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NameAttributeValue()
        {
            await TestAsync(
@"class Program<T>
{
    /// <param name=""x""/>
    void Goo(int x)
    {
    }
}",
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
                XmlDoc.AttributeName(" "),
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task Cref1()
        {
            await TestAsync(
@"/// <see cref=""Program{T}""/>
class Program<T>
{
    void Goo()
    {
    }
}",
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("see"),
                XmlDoc.AttributeName(" "),
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CrefNamespaceIsNotClass()
        {
            await TestAsync(
@"///  <see cref=""N""/>
namespace N
{
    class Program
    {
    }
}",
                XmlDoc.Delimiter("///"),
                XmlDoc.Text("  "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("see"),
                XmlDoc.AttributeName(" "),
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InterfacePropertyWithSameNameShouldBePreferredToType()
        {
            await TestAsync(
@"interface IGoo
{
    int IGoo { get; set; }

    void Bar(int x = IGoo);
}",
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

        [WorkItem(633, "https://github.com/dotnet/roslyn/issues/633")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocCref()
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
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("see"),
                XmlDoc.AttributeName(" "),
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestGenericTypeWithNoArity()
        {
            await TestAsync(
@"
using System.Collections.Generic;

class Program : IReadOnlyCollection
{
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestGenericTypeWithWrongArity()
        {
            await TestAsync(
@"
using System.Collections.Generic;

class Program : IReadOnlyCollection<int,string>
{
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestExtensionMethodDeclaration()
        {
            await TestAsync(
@"static class ExtMethod
{
    public static void TestMethod(this C c)
    {
    }
}
",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestExtensionMethodUsage()
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
                ParseOptions(Options.Regular),
                Keyword("static"),
                Keyword("class"),
                Static("ExtMethod"),
                Class("ExtMethod"),
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
                Static("ExtMethod"),
                Class("ExtMethod"),
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestConstLocals()
        {
            await TestInMethodAsync(
@"const int Number = 42;
var x = Number;",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_InsideMethod()
        {
            await TestInMethodAsync(@"
var unmanaged = 0;
unmanaged++;",
                Keyword("var"),
                Local("unmanaged"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon,
                Local("unmanaged"),
                Operators.PlusPlus,
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_Keyword()
        {
            await TestAsync(
                "class X<T> where T : unmanaged { }",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
class X<T> where T : unmanaged { }",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
class X<T> where T : unmanaged { }",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_Keyword()
        {
            await TestAsync(@"
class X
{
    void M<T>() where T : unmanaged { }
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
class X
{
    void M<T>() where T : unmanaged { }
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_Keyword()
        {
            await TestAsync(
                "delegate void D<T>() where T : unmanaged;",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
delegate void D<T>() where T : unmanaged;",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
delegate void D<T>() where T : unmanaged;",
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
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestOperatorOverloading()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_InsideMethod()
        {
            await TestInMethodAsync(@"
var notnull = 0;
notnull++;",
                Keyword("var"),
                Local("notnull"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon,
                Local("notnull"),
                Operators.PlusPlus,
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Type_Keyword()
        {
            await TestAsync(
                "class X<T> where T : notnull { }",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Type_ExistingInterface()
        {
            await TestAsync(@"
interface notnull {}
class X<T> where T : notnull { }",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Type_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
class X<T> where T : notnull { }",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Method_Keyword()
        {
            await TestAsync(@"
class X
{
    void M<T>() where T : notnull { }
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Method_ExistingInterface()
        {
            await TestAsync(@"
interface notnull {}
class X
{
    void M<T>() where T : notnull { }
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Method_ExistingInterfaceButOutOfScope()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Delegate_Keyword()
        {
            await TestAsync(
                "delegate void D<T>() where T : notnull;",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Delegate_ExistingInterface()
        {
            await TestAsync(@"
interface notnull {}
delegate void D<T>() where T : notnull;",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Delegate_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
delegate void D<T>() where T : notnull;",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_LocalFunction_Keyword()
        {
            await TestAsync(@"
class X
{
    void N()
    {
        void M<T>() where T : notnull { }
    }
}",
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_LocalFunction_ExistingInterface()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_LocalFunction_ExistingInterfaceButOutOfScope()
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
    }
}

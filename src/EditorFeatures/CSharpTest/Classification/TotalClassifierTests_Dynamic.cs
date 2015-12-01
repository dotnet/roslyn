// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class TotalClassifierTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsParamTypeAndDefault()
        {
            await TestInClassAsync(@"void M(dynamic d = default(dynamic",
                Keyword("void"),
                Identifier("M"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"),
                Operators.Equals,
                Keyword("default"),
                Punctuation.OpenParen,
                Keyword("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicExplicitConversion()
        {
            await TestInMethodAsync(@"dynamic d = (dynamic)a;",
                Keyword("dynamic"),
                Identifier("d"),
                Operators.Equals,
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Punctuation.CloseParen,
                Identifier("a"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicMethodCall()
        {
            await TestInMethodAsync(@"dynamic.Equals(1, 1);",
                Identifier("dynamic"),
                Operators.Dot,
                Identifier("Equals"),
                Punctuation.OpenParen,
                Number("1"),
                Punctuation.Comma,
                Number("1"),
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicNullable()
        {
            await TestInMethodAsync(@"dynamic? a",
                Keyword("dynamic"),
                Operators.QuestionMark,
                Identifier("a"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsUsingAliasForClass()
        {
            await TestAsync(@"using dynamic = System.EventArgs;",
                Keyword("using"),
                Class("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Class("EventArgs"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsUsingAliasForDelegate()
        {
            await TestAsync(@"using dynamic = System.Action;",
                Keyword("using"),
                Delegate("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Delegate("Action"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsUsingAliasForStruct()
        {
            await TestAsync(@"using dynamic = System.DateTime;",
                Keyword("using"),
                Struct("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Struct("DateTime"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsUsingAliasForEnum()
        {
            await TestAsync(@"using dynamic = System.DayOfWeek;",
                Keyword("using"),
                Enum("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Enum("DayOfWeek"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsUsingAliasForInterface()
        {
            await TestAsync(@"using dynamic = System.IDisposable;",
                Keyword("using"),
                Interface("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Interface("IDisposable"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsExternAlias()
        {
            await TestAsync(@"extern alias dynamic;
class C { dynamic::Foo a; }",
                Keyword("extern"),
                Keyword("alias"),
                Identifier("dynamic"),
                Punctuation.Semicolon,
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Identifier("dynamic"),
                Operators.Text("::"),
                Identifier("Foo"),
                Identifier("a"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsDelegateType()
        {
            await TestAsync(@"delegate void dynamic()",
                Keyword("delegate"),
                Keyword("void"),
                Delegate("dynamic"),
                Punctuation.OpenParen,
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsDelegateReturnTypeAndParam()
        {
            await TestAsync(@"delegate dynamic MyDelegate (dynamic d)",
                Keyword("delegate"),
                Keyword("dynamic"),
                Delegate("MyDelegate"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsDelegateLocalVariable()
        {
            await TestInMethodAsync(@"Func<string> f = delegate { int dynamic = 10; return dynamic.ToString();};",
                Identifier("Func"),
                Punctuation.OpenAngle,
                Keyword("string"),
                Punctuation.CloseAngle,
                Identifier("f"),
                Operators.Equals,
                Keyword("delegate"),
                Punctuation.OpenCurly,
                Keyword("int"),
                Identifier("dynamic"),
                Operators.Equals,
                Number("10"),
                Punctuation.Semicolon,
                Keyword("return"),
                Identifier("dynamic"),
                Operators.Dot,
                Identifier("ToString"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon,
                Punctuation.CloseCurly,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericTypeName()
        {
            await TestAsync(@"partial class dynamic<T> { } class C { dynamic<int> d; }",
                Keyword("partial"),
                Keyword("class"),
                Class("dynamic"),
                Punctuation.OpenAngle,
                TypeParameter("T"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Class("dynamic"),
                Punctuation.OpenAngle,
                Keyword("int"),
                Punctuation.CloseAngle,
                Identifier("d"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericField()
        {
            await TestAsync(@"class A<T> { T dynamic; }",
                Keyword("class"),
                Class("A"),
                Punctuation.OpenAngle,
                TypeParameter("T"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                TypeParameter("T"),
                Identifier("dynamic"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsIndexerTypeAndParameter()
        {
            await TestInClassAsync(@"dynamic this[dynamic i]",
                Keyword("dynamic"),
                Keyword("this"),
                Punctuation.OpenBracket,
                Keyword("dynamic"),
                Identifier("i"),
                Punctuation.CloseBracket);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsOperatorTypeAndParameter()
        {
            await TestInClassAsync(@"static dynamic operator +(dynamic d1)",
                Keyword("static"),
                Keyword("dynamic"),
                Keyword("operator"),
                Operators.Text("+"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d1"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsOperatorName()
        {
            await TestInClassAsync(@"static explicit operator dynamic(dynamic s)",
                Keyword("static"),
                Keyword("explicit"),
                Keyword("operator"),
                Keyword("dynamic"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("s"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsPropertyTypeAndName()
        {
            await TestInClassAsync(@"dynamic dynamic { get; set; }",
                Keyword("dynamic"),
                Identifier("dynamic"),
                Punctuation.OpenCurly,
                Keyword("get"),
                Punctuation.Semicolon,
                Keyword("set"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsEventName()
        {
            await TestInClassAsync(@"event Action dynamic",
                Keyword("event"),
                Identifier("Action"),
                Identifier("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsLinqLocalVariable()
        {
            await TestInMethodAsync(@"var v = from dynamic in names",
                Keyword("var"),
                Identifier("v"),
                Operators.Equals,
                Keyword("from"),
                Identifier("dynamic"),
                Keyword("in"),
                Identifier("names"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsAnonymousTypePropertyName()
        {
            await TestInMethodAsync(@"var v = from dynamic in names select new { dynamic = dynamic};",
                Keyword("var"),
                Identifier("v"),
                Operators.Equals,
                Keyword("from"),
                Identifier("dynamic"),
                Keyword("in"),
                Identifier("names"),
                Keyword("select"),
                Keyword("new"),
                Punctuation.OpenCurly,
                Identifier("dynamic"),
                Operators.Equals,
                Identifier("dynamic"),
                Punctuation.CloseCurly,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsArgumentToLambdaExpression()
        {
            await TestInMethodAsync(@"var p = names.Select(dynamic => dynamic.Length);",
                Keyword("var"),
                Identifier("p"),
                Operators.Equals,
                Identifier("names"),
                Operators.Dot,
                Identifier("Select"),
                Punctuation.OpenParen,
                Identifier("dynamic"),
                Operators.Text("=>"),
                Identifier("dynamic"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsAnonymousMethodLocalVariable()
        {
            await TestInMethodAsync(@"D f = delegate { string dynamic = ""a""; return dynamic.Length; };",
                Identifier("D"),
                Identifier("f"),
                Operators.Equals,
                Keyword("delegate"),
                Punctuation.OpenCurly,
                Keyword("string"),
                Identifier("dynamic"),
                Operators.Equals,
                String(@"""a"""),
                Punctuation.Semicolon,
                Keyword("return"),
                Identifier("dynamic"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsMethodName()
        {
            await TestInClassAsync(@"dynamic dynamic () { }",
                Keyword("dynamic"),
                Identifier("dynamic"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsStaticMethodTypeAndParams()
        {
            await TestInClassAsync(@"static dynamic dynamic(params dynamic[] dynamic){}",
                Keyword("static"),
                Keyword("dynamic"),
                Identifier("dynamic"),
                Punctuation.OpenParen,
                Keyword("params"),
                Keyword("dynamic"),
                Punctuation.OpenBracket,
                Punctuation.CloseBracket,
                Identifier("dynamic"),
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicArraysInMethodSignature()
        {
            await TestInClassAsync(@"dynamic[] M(dynamic[] p, params dynamic[] pa) { }",
                Keyword("dynamic"),
                Punctuation.OpenBracket,
                Punctuation.CloseBracket,
                Identifier("M"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Punctuation.OpenBracket,
                Punctuation.CloseBracket,
                Identifier("p"),
                Punctuation.Comma,
                Keyword("params"),
                Keyword("dynamic"),
                Punctuation.OpenBracket,
                Punctuation.CloseBracket,
                Identifier("pa"),
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicInPartialMethods()
        {
            await TestInClassAsync(@"partial void F(dynamic d); partial void F(dynamic d) { }",
                Keyword("partial"),
                Keyword("void"),
                Identifier("F"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"),
                Punctuation.CloseParen,
                Punctuation.Semicolon,
                Keyword("partial"),
                Keyword("void"),
                Identifier("F"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"),
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicRefAndOutParameters()
        {
            await TestInClassAsync(@"void F(ref dynamic r, out dynamic o) { }",
                Keyword("void"),
                Identifier("F"),
                Punctuation.OpenParen,
                Keyword("ref"),
                Keyword("dynamic"),
                Identifier("r"),
                Punctuation.Comma,
                Keyword("out"),
                Keyword("dynamic"),
                Identifier("o"),
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicInExtensionMethod()
        {
            await TestInClassAsync(@"dynamic F(this dynamic self, dynamic p) { }",
                Keyword("dynamic"),
                Identifier("F"),
                Punctuation.OpenParen,
                Keyword("this"),
                Keyword("dynamic"),
                Identifier("self"),
                Punctuation.Comma,
                Keyword("dynamic"),
                Identifier("p"),
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsBaseClass()
        {
            await TestAsync(@"class C : dynamic { }",
                Keyword("class"),
                Class("C"),
                Punctuation.Colon,
                Keyword("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericConstraint()
        {
            await TestAsync(@"class C<T> where T : dynamic { }",
                Keyword("class"),
                Class("C"),
                Punctuation.OpenAngle,
                TypeParameter("T"),
                Punctuation.CloseAngle,
                Keyword("where"),
                TypeParameter("T"),
                Punctuation.Colon,
                Keyword("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicSizeOf()
        {
            await TestInClassAsync(@"unsafe int M() { return sizeof(dynamic); }",
                Keyword("unsafe"),
                Keyword("int"),
                Identifier("M"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Keyword("return"),
                Keyword("sizeof"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Punctuation.CloseParen,
                Punctuation.Semicolon,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicTypeOf()
        {
            await TestInMethodAsync(@"typeof(dynamic)",
                Keyword("typeof"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsArrayName()
        {
            await TestAsync(@"int[] dynamic = { 1 };",
                Keyword("int"),
                Punctuation.OpenBracket,
                Punctuation.CloseBracket,
                Identifier("dynamic"),
                Operators.Equals,
                Punctuation.OpenCurly,
                Number("1"),
                Punctuation.CloseCurly,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicInForeach()
        {
            await TestInMethodAsync(@"foreach (dynamic dynamic in dynamic",
                Keyword("foreach"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("dynamic"),
                Keyword("in"),
                Identifier("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicInUsing()
        {
            await TestInMethodAsync(@"using(dynamic d",
                Keyword("using"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsLocalVariableName()
        {
            await TestInMethodAsync(@"dynamic dynamic;",
                Keyword("dynamic"),
                Identifier("dynamic"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsNamespaceName()
        {
            await TestAsync(@"namespace dynamic { }",
                Keyword("namespace"),
                Identifier("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsClassName()
        {
            await TestAsync(@"class dynamic { }",
                Keyword("class"),
                Class("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsConstructorDeclarationName()
        {
            await TestAsync(@"class dynamic { dynamic() { } }",
                Keyword("class"),
                Class("dynamic"),
                Punctuation.OpenCurly,
                Identifier("dynamic"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsNamespaceAlias()
        {
            await TestInMethodAsync(@"dynamic.FileInfo file;",
                Identifier("dynamic"),
                Operators.Dot,
                Identifier("FileInfo"),
                Identifier("file"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGotoLabel()
        {
            await TestInMethodAsync(@"dynamic: int i = 0;
        goto dynamic;",
                Identifier("dynamic"),
                Punctuation.Colon,
                Keyword("int"),
                Identifier("i"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon,
                Keyword("goto"),
                Identifier("dynamic"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsEnumField()
        {
            await TestInMethodAsync(@"A a = A.dynamic;",
                Identifier("A"),
                Identifier("a"),
                Operators.Equals,
                Identifier("A"),
                Operators.Dot,
                Identifier("dynamic"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsEnumFieldDefinition()
        {
            await TestAsync(@"enum A { dynamic }",
                Keyword("enum"),
                Enum("A"),
                Punctuation.OpenCurly,
                Identifier("dynamic"),
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsEnumType()
        {
            await TestAsync(@"enum dynamic { }",
                Keyword("enum"),
                Enum("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericTypeParameter()
        {
            await TestAsync(@"class C<dynamic, T> where dynamic : T { dynamic d; }",
                Keyword("class"),
                Class("C"),
                Punctuation.OpenAngle,
                TypeParameter("dynamic"),
                Punctuation.Comma,
                TypeParameter("T"),
                Punctuation.CloseAngle,
                Keyword("where"),
                TypeParameter("dynamic"),
                Punctuation.Colon,
                TypeParameter("T"),
                Punctuation.OpenCurly,
                TypeParameter("dynamic"),
                Identifier("d"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsFieldType()
        {
            await TestInClassAsync(@"dynamic d",
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsStaticFieldType()
        {
            await TestInClassAsync(@"static dynamic d",
                Keyword("static"),
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsLocalVariableType()
        {
            await TestInMethodAsync(@"dynamic d",
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsArrayLocalVariableType()
        {
            await TestInMethodAsync(@"dynamic[] d",
                Keyword("dynamic"),
                Punctuation.OpenBracket,
                Punctuation.CloseBracket,
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsLambdaParameterType()
        {
            await TestInMethodAsync(@"var q = a.Where((dynamic d) => d == dynamic);",
                Keyword("var"),
                Identifier("q"),
                Operators.Equals,
                Identifier("a"),
                Operators.Dot,
                Identifier("Where"),
                Punctuation.OpenParen,
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"),
                Punctuation.CloseParen,
                Operators.Text("=>"),
                Identifier("d"),
                Operators.Text("=="),
                Identifier("dynamic"),
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicArray()
        {
            await TestInMethodAsync(@"dynamic d = new dynamic[5];",
                Keyword("dynamic"),
                Identifier("d"),
                Operators.Equals,
                Keyword("new"),
                Keyword("dynamic"),
                Punctuation.OpenBracket,
                Number("5"),
                Punctuation.CloseBracket,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicConstructor()
        {
            await TestInMethodAsync(@"dynamic d = new dynamic();",
                Keyword("dynamic"),
                Identifier("d"),
                Operators.Equals,
                Keyword("new"),
                Keyword("dynamic"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAfterIs()
        {
            await TestInMethodAsync(@"if (a is dynamic)",
                Keyword("if"),
                Punctuation.OpenParen,
                Identifier("a"),
                Keyword("is"),
                Keyword("dynamic"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAfterAs()
        {
            await TestInMethodAsync(@"a = a as dynamic",
                Identifier("a"),
                Operators.Equals,
                Identifier("a"),
                Keyword("as"),
                Keyword("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsGenericTypeArgument()
        {
            await TestInMethodAsync(@"List<dynamic> l = new List<dynamic>();",
                Identifier("List"),
                Punctuation.OpenAngle,
                Keyword("dynamic"),
                Punctuation.CloseAngle,
                Identifier("l"),
                Operators.Equals,
                Keyword("new"),
                Identifier("List"),
                Punctuation.OpenAngle,
                Keyword("dynamic"),
                Punctuation.CloseAngle,
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsSecondGenericTypeArgument()
        {
            await TestInMethodAsync(@"KVP<string, dynamic> kvp;",
                Identifier("KVP"),
                Punctuation.OpenAngle,
                Keyword("string"),
                Punctuation.Comma,
                Keyword("dynamic"),
                Punctuation.CloseAngle,
                Identifier("kvp"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsRegionLabel()
        {
            var code =
@"#region dynamic
#endregion";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("dynamic"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsInterfaceType()
        {
            await TestAsync(@"interface dynamic{}",
                Keyword("interface"),
                Interface("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsStructType()
        {
            await TestAsync(@"struct dynamic {  }",
                Keyword("struct"),
                Struct("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DynamicAsUndefinedGenericType()
        {
            await TestInMethodAsync(@"dynamic<int> d;",
                Identifier("dynamic"),
                Punctuation.OpenAngle,
                Keyword("int"),
                Punctuation.CloseAngle,
                Identifier("d"),
                Punctuation.Semicolon);
        }
    }
}

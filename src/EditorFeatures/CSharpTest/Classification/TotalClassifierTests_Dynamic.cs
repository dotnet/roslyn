// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class TotalClassifierTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsParamTypeAndDefault()
        {
            TestInClass(@"void M(dynamic d = default(dynamic",
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
        public void DynamicExplicitConversion()
        {
            TestInMethod(@"dynamic d = (dynamic)a;",
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
        public void DynamicMethodCall()
        {
            TestInMethod(@"dynamic.Equals(1, 1);",
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
        public void DynamicNullable()
        {
            TestInMethod(@"dynamic? a",
                Keyword("dynamic"),
                Operators.QuestionMark,
                Identifier("a"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsUsingAliasForClass()
        {
            Test(@"using dynamic = System.EventArgs;",
                Keyword("using"),
                Class("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Class("EventArgs"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsUsingAliasForDelegate()
        {
            Test(@"using dynamic = System.Action;",
                Keyword("using"),
                Delegate("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Delegate("Action"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsUsingAliasForStruct()
        {
            Test(@"using dynamic = System.DateTime;",
                Keyword("using"),
                Struct("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Struct("DateTime"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsUsingAliasForEnum()
        {
            Test(@"using dynamic = System.DayOfWeek;",
                Keyword("using"),
                Enum("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Enum("DayOfWeek"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsUsingAliasForInterface()
        {
            Test(@"using dynamic = System.IDisposable;",
                Keyword("using"),
                Interface("dynamic"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Interface("IDisposable"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsExternAlias()
        {
            Test(@"extern alias dynamic;
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
        public void DynamicAsDelegateType()
        {
            Test(@"delegate void dynamic()",
                Keyword("delegate"),
                Keyword("void"),
                Delegate("dynamic"),
                Punctuation.OpenParen,
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsDelegateReturnTypeAndParam()
        {
            Test(@"delegate dynamic MyDelegate (dynamic d)",
                Keyword("delegate"),
                Keyword("dynamic"),
                Delegate("MyDelegate"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsDelegateLocalVariable()
        {
            TestInMethod(@"Func<string> f = delegate { int dynamic = 10; return dynamic.ToString();};",
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
        public void DynamicAsGenericTypeName()
        {
            Test(@"partial class dynamic<T> { } class C { dynamic<int> d; }",
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
        public void DynamicAsGenericField()
        {
            Test(@"class A<T> { T dynamic; }",
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
        public void DynamicAsIndexerTypeAndParameter()
        {
            TestInClass(@"dynamic this[dynamic i]",
                Keyword("dynamic"),
                Keyword("this"),
                Punctuation.OpenBracket,
                Keyword("dynamic"),
                Identifier("i"),
                Punctuation.CloseBracket);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsOperatorTypeAndParameter()
        {
            TestInClass(@"static dynamic operator +(dynamic d1)",
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
        public void DynamicAsOperatorName()
        {
            TestInClass(@"static explicit operator dynamic(dynamic s)",
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
        public void DynamicAsPropertyTypeAndName()
        {
            TestInClass(@"dynamic dynamic { get; set; }",
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
        public void DynamicAsEventName()
        {
            TestInClass(@"event Action dynamic",
                Keyword("event"),
                Identifier("Action"),
                Identifier("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsLinqLocalVariable()
        {
            TestInMethod(@"var v = from dynamic in names",
                Keyword("var"),
                Identifier("v"),
                Operators.Equals,
                Keyword("from"),
                Identifier("dynamic"),
                Keyword("in"),
                Identifier("names"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsAnonymousTypePropertyName()
        {
            TestInMethod(@"var v = from dynamic in names select new { dynamic = dynamic};",
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
        public void DynamicAsArgumentToLambdaExpression()
        {
            TestInMethod(@"var p = names.Select(dynamic => dynamic.Length);",
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
        public void DynamicAsAnonymousMethodLocalVariable()
        {
            TestInMethod(@"D f = delegate { string dynamic = ""a""; return dynamic.Length; };",
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
        public void DynamicAsMethodName()
        {
            TestInClass(@"dynamic dynamic () { }",
                Keyword("dynamic"),
                Identifier("dynamic"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsStaticMethodTypeAndParams()
        {
            TestInClass(@"static dynamic dynamic(params dynamic[] dynamic){}",
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
        public void DynamicArraysInMethodSignature()
        {
            TestInClass(@"dynamic[] M(dynamic[] p, params dynamic[] pa) { }",
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
        public void DynamicInPartialMethods()
        {
            TestInClass(@"partial void F(dynamic d); partial void F(dynamic d) { }",
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
        public void DynamicRefAndOutParameters()
        {
            TestInClass(@"void F(ref dynamic r, out dynamic o) { }",
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
        public void DynamicInExtensionMethod()
        {
            TestInClass(@"dynamic F(this dynamic self, dynamic p) { }",
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
        public void DynamicAsBaseClass()
        {
            Test(@"class C : dynamic { }",
                Keyword("class"),
                Class("C"),
                Punctuation.Colon,
                Keyword("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGenericConstraint()
        {
            Test(@"class C<T> where T : dynamic { }",
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
        public void DynamicSizeOf()
        {
            TestInClass(@"unsafe int M() { return sizeof(dynamic); }",
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
        public void DynamicTypeOf()
        {
            TestInMethod(@"typeof(dynamic)",
                Keyword("typeof"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsArrayName()
        {
            Test(@"int[] dynamic = { 1 };",
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
        public void DynamicInForeach()
        {
            TestInMethod(@"foreach (dynamic dynamic in dynamic",
                Keyword("foreach"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("dynamic"),
                Keyword("in"),
                Identifier("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicInUsing()
        {
            TestInMethod(@"using(dynamic d",
                Keyword("using"),
                Punctuation.OpenParen,
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsLocalVariableName()
        {
            TestInMethod(@"dynamic dynamic;",
                Keyword("dynamic"),
                Identifier("dynamic"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsNamespaceName()
        {
            Test(@"namespace dynamic { }",
                Keyword("namespace"),
                Identifier("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsClassName()
        {
            Test(@"class dynamic { }",
                Keyword("class"),
                Class("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsConstructorDeclarationName()
        {
            Test(@"class dynamic { dynamic() { } }",
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
        public void DynamicAsNamespaceAlias()
        {
            TestInMethod(@"dynamic.FileInfo file;",
                Identifier("dynamic"),
                Operators.Dot,
                Identifier("FileInfo"),
                Identifier("file"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGotoLabel()
        {
            TestInMethod(@"dynamic: int i = 0;
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
        public void DynamicAsEnumField()
        {
            TestInMethod(@"A a = A.dynamic;",
                Identifier("A"),
                Identifier("a"),
                Operators.Equals,
                Identifier("A"),
                Operators.Dot,
                Identifier("dynamic"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsEnumFieldDefinition()
        {
            Test(@"enum A { dynamic }",
                Keyword("enum"),
                Enum("A"),
                Punctuation.OpenCurly,
                Identifier("dynamic"),
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsEnumType()
        {
            Test(@"enum dynamic { }",
                Keyword("enum"),
                Enum("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGenericTypeParameter()
        {
            Test(@"class C<dynamic, T> where dynamic : T { dynamic d; }",
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
        public void DynamicAsFieldType()
        {
            TestInClass(@"dynamic d",
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsStaticFieldType()
        {
            TestInClass(@"static dynamic d",
                Keyword("static"),
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsLocalVariableType()
        {
            TestInMethod(@"dynamic d",
                Keyword("dynamic"),
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsArrayLocalVariableType()
        {
            TestInMethod(@"dynamic[] d",
                Keyword("dynamic"),
                Punctuation.OpenBracket,
                Punctuation.CloseBracket,
                Identifier("d"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsLambdaParameterType()
        {
            TestInMethod(@"var q = a.Where((dynamic d) => d == dynamic);",
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
        public void DynamicArray()
        {
            TestInMethod(@"dynamic d = new dynamic[5];",
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
        public void DynamicConstructor()
        {
            TestInMethod(@"dynamic d = new dynamic();",
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
        public void DynamicAfterIs()
        {
            TestInMethod(@"if (a is dynamic)",
                Keyword("if"),
                Punctuation.OpenParen,
                Identifier("a"),
                Keyword("is"),
                Keyword("dynamic"),
                Punctuation.CloseParen);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAfterAs()
        {
            TestInMethod(@"a = a as dynamic",
                Identifier("a"),
                Operators.Equals,
                Identifier("a"),
                Keyword("as"),
                Keyword("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsGenericTypeArgument()
        {
            TestInMethod(@"List<dynamic> l = new List<dynamic>();",
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
        public void DynamicAsSecondGenericTypeArgument()
        {
            TestInMethod(@"KVP<string, dynamic> kvp;",
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
        public void DynamicAsRegionLabel()
        {
            var code =
@"#region dynamic
#endregion";
            Test(code,
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("dynamic"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsInterfaceType()
        {
            Test(@"interface dynamic{}",
                Keyword("interface"),
                Interface("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsStructType()
        {
            Test(@"struct dynamic {  }",
                Keyword("struct"),
                Struct("dynamic"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void DynamicAsUndefinedGenericType()
        {
            TestInMethod(@"dynamic<int> d;",
                Identifier("dynamic"),
                Punctuation.OpenAngle,
                Keyword("int"),
                Punctuation.CloseAngle,
                Identifier("d"),
                Punctuation.Semicolon);
        }
    }
}

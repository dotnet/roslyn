// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class TotalClassifierTests : AbstractCSharpClassifierTests
    {
        internal override IEnumerable<ClassifiedSpan> GetClassificationSpans(
            string code, TextSpan textSpan, CSharpParseOptions options)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code, options))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

                var syntaxTree = document.GetSyntaxTreeAsync().Result;

                var service = document.GetLanguageService<IClassificationService>();
                var classifiers = service.GetDefaultSyntaxClassifiers();
                var extensionManager = workspace.Services.GetService<IExtensionManager>();

                var semanticClassifications = new List<ClassifiedSpan>();
                var syntacticClassifications = new List<ClassifiedSpan>();
                service.AddSemanticClassificationsAsync(document, textSpan,
                    extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes),
                    extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds),
                    semanticClassifications, CancellationToken.None).Wait();
                service.AddSyntacticClassifications(syntaxTree, textSpan, syntacticClassifications, CancellationToken.None);

                var classificationsSpans = new HashSet<TextSpan>();

                // Add all the semantic classifications in.
                var allClassifications = new List<ClassifiedSpan>(semanticClassifications);
                classificationsSpans.AddRange(allClassifications.Select(t => t.TextSpan));

                // Add the syntactic classifications.  But only if they don't conflict with a semantic
                // classification.
                allClassifications.AddRange(
                    from t in syntacticClassifications
                    where !classificationsSpans.Contains(t.TextSpan)
                    select t);

                return allClassifications;
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VarAsUsingAliasForNamespace()
        {
            Test(@"using var = System;",
                Keyword("using"),
                Identifier("var"),
                Operators.Equals,
                Identifier("System"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification), WorkItem(547068)]
        public void Bug17819()
        {
            Test(@"_ _(){}
///<param name='_
}",
                Identifier("_"),
                Identifier("_"),
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VarAsUsingAliasForClass()
        {
            Test(@"using var = System.Math;",
                Keyword("using"),
                Class("var"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Class("Math"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VarAsUsingAliasForDelegate()
        {
            Test(@"using var = System.Action;",
                Keyword("using"),
                Delegate("var"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Delegate("Action"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VarAsUsingAliasForStruct()
        {
            Test(@"using var = System.DateTime;",
                Keyword("using"),
                Struct("var"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Struct("DateTime"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VarAsUsingAliasForEnum()
        {
            Test(@"using var = System.DayOfWeek;",
                Keyword("using"),
                Enum("var"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Enum("DayOfWeek"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VarAsUsingAliasForInterface()
        {
            Test(@"using var = System.IDisposable;",
                Keyword("using"),
                Interface("var"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Interface("IDisposable"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VarAsConstructorName()
        {
            Test(@"class var { var() { } }",
                Keyword("class"),
                Class("var"),
                Punctuation.OpenCurly,
                Identifier("var"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void UsingAliasGlobalNamespace()
        {
            Test(@"using IO = global::System.IO;",
                Keyword("using"),
                Identifier("IO"),
                Operators.Equals,
                Keyword("global"),
                Operators.Text("::"),
                Identifier("System"),
                Operators.Dot,
                Identifier("IO"),
                Punctuation.Semicolon);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PartialDynamicWhere()
        {
            var code = @"partial class partial<where> where where : partial<where>
{
    static dynamic dynamic<partial>()
    {
        return dynamic<dynamic>();
    }
}
";
            Test(code,
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
                Identifier("dynamic"),
                Punctuation.OpenAngle,
                TypeParameter("partial"),
                Punctuation.CloseAngle,
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Keyword("return"),
                Identifier("dynamic"),
                Punctuation.OpenAngle,
                Keyword("dynamic"),
                Punctuation.CloseAngle,
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(543123)]
        public void VarInForeach()
        {
            TestInMethod(@"foreach (var v in args) { }",
                Keyword("foreach"),
                Punctuation.OpenParen,
                Keyword("var"),
                Identifier("v"),
                Keyword("in"),
                Identifier("args"),
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ValueInSetterAndAnonymousTypePropertyName()
        {
            Test(@"class C { int P { set { var t = new { value = value }; } } }",
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Keyword("int"),
                Identifier("P"),
                Punctuation.OpenCurly,
                Keyword("set"),
                Punctuation.OpenCurly,
                Keyword("var"),
                Identifier("t"),
                Operators.Equals,
                Keyword("new"),
                Punctuation.OpenCurly,
                Identifier("value"),
                Operators.Equals,
                Keyword("value"),
                Punctuation.CloseCurly,
                Punctuation.Semicolon,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestValueInEvent()
        {
            TestInClass(
@"event int Bar {
   add {
     this.value = value;
   }
   remove {
     this.value = value;
   }
}",

                Keyword("event"),
                Keyword("int"),
                Identifier("Bar"),
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestValueInProperty()
        {
            TestInClass(
@"int Foo {
   get {
     this.value = value;
   }
   set {
     this.value = value;
   }
}",
                Keyword("int"),
                Identifier("Foo"),
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void ValueFieldInSetterAccessedThroughThis()
        {
            TestInClass(@"int P { set { this.value = value; } }",
                Keyword("int"),
                Identifier("P"),
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NewOfInterface()
        {
            TestInMethod(@"object o = new System.IDisposable();",
                Keyword("object"),
                Identifier("o"),
                Operators.Equals,
                Keyword("new"),
                Identifier("System"),
                Operators.Dot,
                Interface("IDisposable"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [WorkItem(545611)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestVarConstructor()
        {
            Test(@"class var
{
    void Main()
    {
        new var();
    }
}
",
                Keyword("class"),
                Class("var"),
                Punctuation.OpenCurly,
                Keyword("void"),
                Identifier("Main"),
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

        [WorkItem(545609)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestVarTypeParameter()
        {
            Test(@"class X
{
    void Foo<var>()
    {
        var x;
    }
}
",
                Keyword("class"),
                Class("X"),
                Punctuation.OpenCurly,
                Keyword("void"),
                Identifier("Foo"),
                Punctuation.OpenAngle,
                TypeParameter("var"),
                Punctuation.CloseAngle,
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                TypeParameter("var"),
                Identifier("x"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WorkItem(545610)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestVarAttribute1()
        {
            Test(@"using System;
 
[var]
class var : Attribute { }
",
                Keyword("using"),
                Identifier("System"),
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

        [WorkItem(545610)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestVarAttribute2()
        {
            Test(@"using System;
 
[var]
class varAttribute : Attribute { }
",
                Keyword("using"),
                Identifier("System"),
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

        [WorkItem(546170)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestStandaloneTypeName()
        {
            Test(@"using System;
class C
{
    static void Main()
    {
        var tree = Console
    }
}",
                Keyword("using"),
                Identifier("System"),
                Punctuation.Semicolon,
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Keyword("static"),
                Keyword("void"),
                Identifier("Main"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Keyword("var"),
                Identifier("tree"),
                Operators.Equals,
                Class("Console"),
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WorkItem(546403)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void TestNamespaceClassAmbiguities()
        {
            Test(@"class C
{
}
 
namespace C
{
}
",
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Keyword("namespace"),
                Identifier("C"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NameAttributeValue()
        {
            Test(@"
class Program<T>
{
    /// <param name=""x""/>
    void Foo(int x) { }
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
                Identifier("x"),
                XmlDoc.AttributeQuotes("\""),
                XmlDoc.Delimiter("/>"),
                Keyword("void"),
                Identifier("Foo"),
                Punctuation.OpenParen,
                Keyword("int"),
                Identifier("x"),
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Cref1()
        {
            Test(@"/// <see cref=""Program{T}""/>
class Program<T>
{
    void Foo() { }
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
                Identifier("Foo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void CrefNamespaceIsNotClass()
        {
            Test(@"///  <see cref=""N""/>
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
                Identifier("N"),
                XmlDoc.AttributeQuotes("\""),
                XmlDoc.Delimiter("/>"),
                Keyword("namespace"),
                Identifier("N"),
                Punctuation.OpenCurly,
                Keyword("class"),
                Class("Program"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Punctuation.CloseCurly);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void InterfacePropertyWithSameNameShouldBePreferredToType()
        {
            Test(@"interface IFoo
{
    int IFoo { get; set; }
    void Bar(int x = IFoo);
}",
                Keyword("interface"),
                Interface("IFoo"),
                Punctuation.OpenCurly,
                Keyword("int"),
                Identifier("IFoo"),
                Punctuation.OpenCurly,
                Keyword("get"),
                Punctuation.Semicolon,
                Keyword("set"),
                Punctuation.Semicolon,
                Punctuation.CloseCurly,
                Keyword("void"),
                Identifier("Bar"),
                Punctuation.OpenParen,
                Keyword("int"),
                Identifier("x"),
                Operators.Equals,
                Identifier("IFoo"),
                Punctuation.CloseParen,
                Punctuation.Semicolon,
                Punctuation.CloseCurly);
        }
    }
}

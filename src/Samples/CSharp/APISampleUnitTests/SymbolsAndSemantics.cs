// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace APISampleUnitTestsCS
{
    [TestClass]
    public class SymbolsAndSemantics
    {
        [TestMethod]
        public void GetExpressionType()
        {
            TestCode testCode = new TestCode(@"class Program
{
    public static void Method()
    {
        var local = new Program().ToString() + string.Empty;
    } 
}");

            TypeSyntax varNode = testCode.SyntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .First()
                .Declaration
                .Type;

            var semanticInfo = testCode.SemanticModel.GetTypeInfo(varNode);
            Assert.AreEqual("String", semanticInfo.Type.Name);
        }

        [TestMethod]
        public void BindNameToSymbol()
        {
            TestCode testCode = new TestCode("using System;");
            CompilationUnitSyntax compilationUnit = testCode.SyntaxTree.GetRoot() as CompilationUnitSyntax;
            NameSyntax node = compilationUnit.Usings[0].Name;

            var semanticInfo = testCode.SemanticModel.GetSymbolInfo(node);
            var namespaceSymbol = semanticInfo.Symbol as INamespaceSymbol;

            Assert.IsTrue(namespaceSymbol.GetNamespaceMembers().Any(
                symbol => symbol.Name == "Collections"));
        }

        [TestMethod]
        public void GetDeclaredSymbol()
        {
            TestCode testCode = new TestCode("namespace Acme { internal class C$lass1 { } }");
            var symbol = testCode.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)testCode.SyntaxNode);

            Assert.AreEqual(true, symbol.CanBeReferencedByName);
            Assert.AreEqual("Acme", symbol.ContainingNamespace.Name);
            Assert.AreEqual(Accessibility.Internal, symbol.DeclaredAccessibility);
            Assert.AreEqual(SymbolKind.NamedType, symbol.Kind);
            Assert.AreEqual("Class1", symbol.Name);
            Assert.AreEqual("Acme.Class1", symbol.ToDisplayString());
            Assert.AreEqual("Acme.Class1", symbol.ToString());
        }

        [TestMethod]
        public void GetSymbolXmlDocComments()
        {
            TestCode testCode = new TestCode(@"
/// <summary>
/// This is a test class!
/// </summary>
class C$lass1 { }");
            var symbol = testCode.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)testCode.SyntaxNode);

            string actualXml = symbol.GetDocumentationCommentXml();
            string expectedXml = 
@"<member name=""T:Class1"">
    <summary>
    This is a test class!
    </summary>
</member>
";
            Assert.AreEqual(expectedXml, actualXml);
        }

        [TestMethod]
        public void SymbolDisplayFormatTest()
        {
            TestCode testCode = new TestCode(@"
class C1<T> { }
class C2 {
    public static TSource M<TSource>(this C1<TSource> source, // comment here
int index) {} }");

            SymbolDisplayFormat format = new SymbolDisplayFormat(
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var symbol = testCode.Compilation
                .SourceModule
                .GlobalNamespace
                .GetTypeMembers("C2")[0]
                .GetMembers("M")[0];

            Assert.AreEqual("public static TSource C2.M<TSource>(this C1<TSource> source, int index)", symbol.ToDisplayString(format));
        }

        [TestMethod]
        public void EnumerateSymbolsInCompilation()
        {
            string file1 = "public class Animal { public virtual void MakeSound() { } }";
            string file2 = "class Cat : Animal { public override void MakeSound() { } }";
            var compilation = CSharpCompilation.Create("test")
                    .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(file1), SyntaxFactory.ParseSyntaxTree(file2))
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            var globalNamespace = compilation.SourceModule.GlobalNamespace;

            StringBuilder sb = new StringBuilder();
            EnumSymbols(globalNamespace, symbol => sb.AppendLine(symbol.ToString()));

            Assert.AreEqual(@"<global namespace>
Animal
Animal.MakeSound()
Animal.Animal()
Cat
Cat.MakeSound()
Cat.Cat()
", sb.ToString());
        }

        private void EnumSymbols(ISymbol symbol, Action<ISymbol> callback)
        {
            callback(symbol);
            foreach (var childSymbol in GetMembers(symbol))
            {
                EnumSymbols(childSymbol, callback);
            }
        }

        private IEnumerable<ISymbol> GetMembers(ISymbol parent)
        {
            INamespaceOrTypeSymbol container = parent as INamespaceOrTypeSymbol;
            if (container != null)
            {
                return container.GetMembers().AsEnumerable();
            }

            return Enumerable.Empty<ISymbol>();
        }

        [TestMethod]
        public void AnalyzeRegionControlFlow()
        {
            TestCode testCode = new TestCode(@"
class C {
    public void F()
    {
        goto L1; // 1
/*start*/
        L1: ;
        if (false) return;
/*end*/
        goto L1; // 2
    }
}");
            StatementSyntax firstStatement, lastStatement;
            testCode.GetStatementsBetweenMarkers(out firstStatement, out lastStatement);
            ControlFlowAnalysis regionControlFlowAnalysis =
                testCode.SemanticModel.AnalyzeControlFlow(firstStatement, lastStatement);

            Assert.AreEqual(1, regionControlFlowAnalysis.EntryPoints.Count());
            Assert.AreEqual(1, regionControlFlowAnalysis.ExitPoints.Count());
            Assert.IsTrue(regionControlFlowAnalysis.EndPointIsReachable);

            BlockSyntax methodBody = testCode.SyntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First()
                .Body;

            regionControlFlowAnalysis = testCode.SemanticModel.AnalyzeControlFlow(methodBody, methodBody);

            Assert.IsFalse(regionControlFlowAnalysis.EndPointIsReachable);
        }

        [TestMethod]
        public void AnalyzeRegionDataFlow()
        {
            TestCode testCode = new TestCode(@"
class C {
    public void F(int x)
    {
        int a;
/*start*/
        int b;
        int x, y = 1;
        { var z = ""a""; }
/*end*/
        int c;
    }
}");
            StatementSyntax firstStatement, lastStatement;
            testCode.GetStatementsBetweenMarkers(out firstStatement, out lastStatement);
            DataFlowAnalysis regionDataFlowAnalysis = testCode.SemanticModel.AnalyzeDataFlow(firstStatement, lastStatement);

            Assert.AreEqual("b,x,y,z", string.Join(",", regionDataFlowAnalysis
                .VariablesDeclared
                .Select(symbol => symbol.Name)));
        }

        [TestMethod]
        public void FailedOverloadResolution()
        {
            TestCode testCode = new TestCode(@"
class Program
{
    static void Main(string[] args)
    {
        int i = 8;
        int j = i + q;
        X$.f(""hello"");
    }
}

class X
{
    public static void f() { }
    public static void f(int i) { }
}
");
            var typeInfo = testCode.SemanticModel.GetTypeInfo((ExpressionSyntax)testCode.SyntaxNode);
            var semanticInfo = testCode.SemanticModel.GetSymbolInfo((ExpressionSyntax)testCode.SyntaxNode);

            Assert.IsNull(typeInfo.Type);
            Assert.IsNull(typeInfo.ConvertedType);

            Assert.IsNull(semanticInfo.Symbol);
            Assert.AreEqual(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.AreEqual(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(s => s.ToDisplayString()).ToArray();
            Assert.AreEqual("X.f()", sortedCandidates[0].ToDisplayString());
            Assert.AreEqual(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.AreEqual("X.f(int)", sortedCandidates[1].ToDisplayString());
            Assert.AreEqual(SymbolKind.Method, sortedCandidates[1].Kind);

            var memberGroup = testCode.SemanticModel.GetMemberGroup((ExpressionSyntax)testCode.SyntaxNode);

            Assert.AreEqual(2, memberGroup.Length);
            var sortedMemberGroup = memberGroup.AsEnumerable().OrderBy(s => s.ToDisplayString()).ToArray();
            Assert.AreEqual("X.f()", sortedMemberGroup[0].ToDisplayString());
            Assert.AreEqual("X.f(int)", sortedMemberGroup[1].ToDisplayString());
        }

        /// <summary>
        /// Helper class to bundle together information about a piece of analyzed test code.
        /// </summary>
        private class TestCode
        {
            public int Position { get; private set; }
            public string Text { get; private set; }

            public SyntaxTree SyntaxTree { get; private set; }

            public SyntaxToken Token { get; private set; }
            public SyntaxNode SyntaxNode { get; private set; }

            public Compilation Compilation { get; private set; }
            public SemanticModel SemanticModel { get; private set; }

            public TestCode(string textWithMarker)
            {
                // $ marks the position in source code. It's better than passing a manually calculated
                // int, and is just for test convenience. $ is a char that is used nowhere in the C#
                // language.
                Position = textWithMarker.IndexOf('$');
                if (Position != -1)
                {
                    textWithMarker = textWithMarker.Remove(Position, 1);
                }

                Text = textWithMarker;
                SyntaxTree = SyntaxFactory.ParseSyntaxTree(Text);

                if (Position != -1)
                {
                    Token = SyntaxTree.GetRoot().FindToken(Position);
                    SyntaxNode = Token.Parent;
                }

                Compilation = CSharpCompilation
                    .Create("test")
                    .AddSyntaxTrees(SyntaxTree)
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

                SemanticModel = Compilation.GetSemanticModel(SyntaxTree);
            }

            public void GetStatementsBetweenMarkers(out StatementSyntax firstStatement, out StatementSyntax lastStatement)
            {
                TextSpan span = GetSpanBetweenMarkers();
                var statementsInside = SyntaxTree
                    .GetRoot()
                    .DescendantNodes(span)
                    .OfType<StatementSyntax>()
                    .Where(s => span.Contains(s.Span));
                var first = firstStatement = statementsInside
                    .First();
                lastStatement = statementsInside
                    .Where(s => s.Parent == first.Parent)
                    .Last();
            }

            public TextSpan GetSpanBetweenMarkers()
            {
                SyntaxTrivia startComment = SyntaxTree
                    .GetRoot()
                    .DescendantTrivia()
                    .First(syntaxTrivia => syntaxTrivia.ToString().Contains("start"));
                SyntaxTrivia endComment = SyntaxTree
                    .GetRoot()
                    .DescendantTrivia()
                    .First(syntaxTrivia => syntaxTrivia.ToString().Contains("end"));

                TextSpan textSpan = TextSpan.FromBounds(
                    startComment.FullSpan.End,
                    endComment.FullSpan.Start);
                return textSpan;
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace APISampleUnitTestsCS
{
    public class SymbolsAndSemantics
    {
        [Fact]
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
            Assert.Equal("String", semanticInfo.Type.Name);
        }

        [Fact]
        public void BindNameToSymbol()
        {
            TestCode testCode = new TestCode("using System;");
            CompilationUnitSyntax compilationUnit = testCode.SyntaxTree.GetRoot() as CompilationUnitSyntax;
            NameSyntax node = compilationUnit.Usings[0].Name;

            var semanticInfo = testCode.SemanticModel.GetSymbolInfo(node);
            var namespaceSymbol = semanticInfo.Symbol as INamespaceSymbol;

            Assert.True(namespaceSymbol.GetNamespaceMembers().Any(
                symbol => symbol.Name == "Collections"));
        }

        [Fact]
        public void GetDeclaredSymbol()
        {
            TestCode testCode = new TestCode("namespace Acme { internal class C$lass1 { } }");
            var symbol = testCode.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)testCode.SyntaxNode);

            Assert.Equal(true, symbol.CanBeReferencedByName);
            Assert.Equal("Acme", symbol.ContainingNamespace.Name);
            Assert.Equal(Accessibility.Internal, symbol.DeclaredAccessibility);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("Class1", symbol.Name);
            Assert.Equal("Acme.Class1", symbol.ToDisplayString());
            Assert.Equal("Acme.Class1", symbol.ToString());
        }

        [Fact]
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
            Assert.Equal(expectedXml, actualXml);
        }

        [Fact]
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

            Assert.Equal("public static TSource C2.M<TSource>(this C1<TSource> source, int index)", symbol.ToDisplayString(format));
        }

        [Fact]
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

            Assert.Equal(@"<global namespace>
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

        [Fact]
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

            Assert.Equal(1, regionControlFlowAnalysis.EntryPoints.Count());
            Assert.Equal(1, regionControlFlowAnalysis.ExitPoints.Count());
            Assert.True(regionControlFlowAnalysis.EndPointIsReachable);

            BlockSyntax methodBody = testCode.SyntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First()
                .Body;

            regionControlFlowAnalysis = testCode.SemanticModel.AnalyzeControlFlow(methodBody, methodBody);

            Assert.False(regionControlFlowAnalysis.EndPointIsReachable);
        }

        [Fact]
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

            Assert.Equal("b,x,y,z", string.Join(",", regionDataFlowAnalysis
                .VariablesDeclared
                .Select(symbol => symbol.Name)));
        }

        [Fact]
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

            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(s => s.ToDisplayString()).ToArray();
            Assert.Equal("X.f()", sortedCandidates[0].ToDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("X.f(int)", sortedCandidates[1].ToDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            var memberGroup = testCode.SemanticModel.GetMemberGroup((ExpressionSyntax)testCode.SyntaxNode);

            Assert.Equal(2, memberGroup.Length);
            var sortedMemberGroup = memberGroup.AsEnumerable().OrderBy(s => s.ToDisplayString()).ToArray();
            Assert.Equal("X.f()", sortedMemberGroup[0].ToDisplayString());
            Assert.Equal("X.f(int)", sortedMemberGroup[1].ToDisplayString());
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

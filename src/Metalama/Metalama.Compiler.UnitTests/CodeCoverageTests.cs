// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Compiler.UnitTests
{
    public class CodeCoverageTests : CSharpTestBase
    {
        const string InstrumentationHelperSource = @"
namespace Microsoft.CodeAnalysis.Runtime
{
    public static class Instrumentation
    {
        public static bool[] CreatePayload(System.Guid mvid, int methodToken, int fileIndex, ref bool[] payload, int payloadLength)
        {
            return payload;
        }

        public static bool[] CreatePayload(System.Guid mvid, int methodToken, int[] fileIndices, ref bool[] payload, int payloadLength)
        {
            return payload;
        }

        public static void FlushPayload()
        {
        }
    }
}
";
        private readonly ITestOutputHelper _log;

        public CodeCoverageTests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void InsertStatement()
        {
            var source = @"
using System;

public class C
{
    public static void Main(string[] args)                           
    {
        if ( args.Length == 1 )
        {
            Console.WriteLine(""X"");
        }
    }
}

";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            c = (CSharpCompilation)MetalamaCompilerTest.ExecuteTransformer(c, new InsertStatementTransformer());

            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            var sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[2], sourceLines,
                new SpanResult(5, 4, 11, 5, "public static void Main("),
                new SpanResult(9, 12, 9, 35, "Console.WriteLine"),
                new SpanResult(7, 13, 7, 29, "args.Length == 1") );
        }

        private class InsertStatementTransformer : CSharpSyntaxRewriter, ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                foreach ( var tree in context.Compilation.SyntaxTrees )
                {
                    var transformedTree = tree.WithRootAndOptions( this.Visit( tree.GetRoot()), tree.Options);
                    context.ReplaceSyntaxTree(tree, transformedTree);
                }
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.Text == "Main")
                {
                    var insertedStatement = SyntaxFactory.ParseStatement("if ( args == null ) throw new Exception();");
                    return node.WithBody(node.Body!.WithStatements(node.Body.Statements.Insert(0, insertedStatement)) );
                }
                else
                {
                    return node;
                }
            }
        }


        [Fact]
        public void WrapWithIf()
        {
            var source = @"
using System;

public class C
{
    public static void Main(string[] args)                           
    {
        if ( args.Length == 1 )
        {
            Console.WriteLine(""X"");
        }
    }
}

";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            c = (CSharpCompilation)MetalamaCompilerTest.ExecuteTransformer(c, new WrapWithIfTransformer());

            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            var sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[2], sourceLines,
                new SpanResult(5, 4, 11, 5, "public static void Main("),
                new SpanResult(9, 12, 9, 35, "Console.WriteLine"),
                new SpanResult(7, 13, 7, 29, "args.Length == 1") );
        }

        private class WrapWithIfTransformer : CSharpSyntaxRewriter, ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                foreach ( var tree in context.Compilation.SyntaxTrees )
                {
                    var transformedTree = tree.WithRootAndOptions( this.Visit( tree.GetRoot()), tree.Options);
                    context.ReplaceSyntaxTree(tree, transformedTree);
                }
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.Text == "Main")
                {
                    var ifStatement = IfStatement(
                        BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            IdentifierName("args"),
                            LiteralExpression(
                                SyntaxKind.NullLiteralExpression)),
                        node.Body!);
                    return node.WithBody(Block(ifStatement));
                }
                else
                {
                    return node;
                }
            }
        }
        
        [Fact]
        public void InsertMethod()
        {
            var source = @"
using System;

public class C
{
    public static void Main(string[] args)                           
    {
        if ( args.Length == 1 )
        {
            Console.WriteLine(""X"");
        }
    }
}

";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            c = (CSharpCompilation)MetalamaCompilerTest.ExecuteTransformer(c, new InsertMethodTransformer());

            _ = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            // We only check that the compilation does not have exceptions.

        }

        private class InsertMethodTransformer : CSharpSyntaxRewriter, ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    var transformedTree = tree.WithRootAndOptions(this.Visit(tree.GetRoot()), tree.Options);
                    context.ReplaceSyntaxTree(tree, transformedTree);
                }
            }
            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if ( node.Identifier.Text == "C")
                {
                    var insertedMember = SyntaxFactory.ParseMemberDeclaration("public static void M() => Console.WriteLine();")!;
                    return node.WithMembers(node.Members.Add(insertedMember));
                }
                else
                {
                    return node;
                }
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.Text == "Main")
                {
                    var insertedStatement = SyntaxFactory.ParseStatement("if ( args == null ) throw new Exception();")!;
                    return node.WithBody(node.Body!.WithStatements(node.Body.Statements.Insert(0, insertedStatement)));
                }
                else
                {
                    return node;
                }
            }
        }

         [Fact]
        public void MoveMethodBody()
        {
            var source = @"
using System;

public class C
{
    public static void Main(string[] args)                           
    {
        if ( args.Length == 1 )
        {
            Console.WriteLine(""X"");
        }
    }
}

";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            c = (CSharpCompilation)MetalamaCompilerTest.ExecuteTransformer(c, new MoveMethodBodyTransformer());

            var verifier = CompileAndVerify(c, emitOptions:EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
            verifier.VerifyIL("C.Main", @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void C.Main_Source(string[])""
            IL_0006:  ret
        }");
            verifier.VerifyIL("C.Main_Source", @"{
  // Code size       81 (0x51)
  .maxstack  5
  .locals init (bool[] V_0)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
            IL_0005:  ldtoken    ""void C.Main(string[])""
            IL_000a:  ldelem.ref
            IL_000b:  stloc.0
            IL_000c:  ldloc.0
            IL_000d:  brtrue.s   IL_0034
            IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
            IL_0014:  ldtoken    ""void C.Main(string[])""
            IL_0019:  ldtoken    Source Document 0
            IL_001e:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
            IL_0023:  ldtoken    ""void C.Main(string[])""
            IL_0028:  ldelema    ""bool[]""
            IL_002d:  ldc.i4.3
            IL_002e:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)""
            IL_0033:  stloc.0
            IL_0034:  ldloc.0
            IL_0035:  ldc.i4.0
            IL_0036:  ldc.i4.1
            IL_0037:  stelem.i1
            IL_0038:  ldloc.0
            IL_0039:  ldc.i4.2
            IL_003a:  ldc.i4.1
            IL_003b:  stelem.i1
            IL_003c:  ldarg.0
            IL_003d:  ldlen
            IL_003e:  conv.i4
            IL_003f:  ldc.i4.1
            IL_0040:  bne.un.s   IL_0050
            IL_0042:  ldloc.0
            IL_0043:  ldc.i4.1
            IL_0044:  ldc.i4.1
            IL_0045:  stelem.i1
            IL_0046:  ldstr      ""X""
            IL_004b:  call       ""void System.Console.WriteLine(string)""
            IL_0050:  ret
        }");

            var peImage = verifier.EmittedAssemblyData;
            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            var sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[3], sourceLines,
                new SpanResult(5, 4, 11, 5, "public static void Main("),
                new SpanResult(9, 12, 9, 35, "Console.WriteLine"),
                new SpanResult(7, 13, 7, 29, "args.Length == 1") );
        }

        private class MoveMethodBodyTransformer : CSharpSyntaxRewriter, ISourceTransformer
        {
            private Compilation? _compilation;

            public void Execute(TransformerContext context)
            {
                this._compilation = context.Compilation;
                foreach ( var tree in context.Compilation.SyntaxTrees )
                {
                    var transformedTree = tree.WithRootAndOptions( this.Visit( tree.GetRoot()), tree.Options);
                    context.ReplaceSyntaxTree(tree, transformedTree);
                }
            }

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (node.Identifier.Text == "C")
                {
                    var originalMethod = (MethodDeclarationSyntax) node.Members[0];
                    var semanticModel = this._compilation!.GetSemanticModel(node.SyntaxTree);
                    var originalMethodSymbol = semanticModel.GetDeclaredSymbol(originalMethod)!;

                    var newMethod = MethodDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.VoidKeyword)),
                            Identifier("Main_Source"))
                        .WithParameterList(
                            ParameterList(
                                SingletonSeparatedList(
                                    Parameter(
                                            Identifier("args"))
                                        .WithType(
                                            ArrayType(
                                                    PredefinedType(
                                                        Token(SyntaxKind.StringKeyword)))
                                                .WithRankSpecifiers(
                                                    SingletonList(
                                                        ArrayRankSpecifier(
                                                            SingletonSeparatedList<ExpressionSyntax>(
                                                                OmittedArraySizeExpression()))))))))
                        .NormalizeWhitespace()
                        .WithBody(originalMethod.Body)
                        .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                        .WithRedirectCodeCoverageAnnotation(originalMethodSymbol);

                    var newBodyOfOriginalMethod = InvocationExpression(
                            IdentifierName("Main_Source"))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        IdentifierName("args")))))
                        
                        .NormalizeWhitespace();

                    return node.WithMembers(
                        List(new MemberDeclarationSyntax[]
                        {
                            originalMethod.WithBody(null).WithExpressionBody(ArrowExpressionClause(newBodyOfOriginalMethod)).WithIgnoreCodeCoverageAnnotation(),
                            newMethod
                        }));
                }
                else
                {
                    return node;
                }
            }
        }

        private class SpanResult
        {
            public int StartLine { get; }
            public int StartColumn { get; }
            public int EndLine { get; }
            public int EndColumn { get; }
            public string TextStart { get; }
            public SpanResult(int startLine, int startColumn, int endLine, int endColumn, string textStart)
            {
                StartLine = startLine;
                StartColumn = startColumn;
                EndLine = endLine;
                EndColumn = endColumn;
                TextStart = textStart;
            }
        }

        private void VerifySpans(DynamicAnalysisDataReader reader, DynamicAnalysisMethod methodData, string[] sourceLines, params SpanResult[] expected)
        {
            foreach ( var s in reader.GetSpans(methodData.Blob))
            {
                _log.WriteLine(
            $"({s.StartLine},{s.StartColumn})-({s.EndLine},{s.EndColumn}): >>{sourceLines[s.StartLine].Substring(s.StartColumn).Trim()}<<" );
            }
            var expectedSpanSpellings = new List<string>();
            foreach (var expectedSpanResult in expected)
            {
                var text = sourceLines[expectedSpanResult.StartLine].Substring(expectedSpanResult.StartColumn);
                Assert.True(text.StartsWith(expectedSpanResult.TextStart), $"Text doesn't start with {expectedSpanResult.TextStart}. Text is: {text}");

                expectedSpanSpellings.Add(string.Format("({0},{1})-({2},{3})", expectedSpanResult.StartLine, expectedSpanResult.StartColumn, expectedSpanResult.EndLine, expectedSpanResult.EndColumn));
            }

            VerifySpans(reader, methodData, expectedSpanSpellings.ToArray());
        }

        private static void VerifySpans(DynamicAnalysisDataReader reader, DynamicAnalysisMethod methodData, params string[] expected)
        {
            AssertEx.Equal(expected, reader.GetSpans(methodData.Blob).Select(s => $"({s.StartLine},{s.StartColumn})-({s.EndLine},{s.EndColumn})"));
        }
    }
}

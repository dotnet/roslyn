// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    public partial class StackFrameParserTests
    {
        [Fact]
        public void TestNoParams()
            => Verify(
                @"at ConsoleApp4.MyClass.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                    argumentList: EmptyParams)
                );

        [Theory]
        [InlineData("C", 1)]
        [InlineData("C", 100)]
        [InlineData("a‿", 5)] // Unicode character with connection
        [InlineData("abcdefg", 99999)]
        public void TestArity(string typeName, int arity)
            => Verify(
                $"at ConsoleApp4.{typeName}`{arity}.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                            GenericType(typeName, arity)),
                        Identifier("M")),

                    argumentList: EmptyParams)
                );

        [Fact]
        public void TestTrailingTrivia()
            => Verify(
                @"at ConsoleApp4.MyClass.M() some other text",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                    argumentList: EmptyParams),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(" some other text"))
                );

        [Fact]
        public void TestTrailingTrivia_InTriviaNoSpace()
            => Verify(
                @"at ConsoleApp4.MyClass.M() inC:\My\Path\C.cs:line 26",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                    argumentList: EmptyParams),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(@" inC:\My\Path\C.cs:line 26"))
                );

        [Fact]
        public void TestTrailingTrivia_InTriviaNoSpace2()
            => Verify(
                @"at ConsoleApp4.MyClass.M()in C:\My\Path\C.cs:line 26",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                    argumentList: EmptyParams),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(@"in C:\My\Path\C.cs:line 26"))
                );

        [Fact]
        public void TestNoParams_NoAtTrivia()
            => Verify(
                @"ConsoleApp4.MyClass.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M"),
                    argumentList: EmptyParams)
                );

        [Fact]
        public void TestNoParams_SpaceInParams_NoAtTrivia()
            => Verify(
                @"ConsoleApp4.MyClass.M(  )",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M"),
                    argumentList: ParameterList(
                        OpenParenToken.With(trailingTrivia: CreateTriviaArray(SpaceTrivia(2))),
                        CloseParenToken))
                );

        [Fact]
        public void TestNoParams_SpaceTrivia()
            => Verify(
                @" ConsoleApp4.MyClass.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: SpaceTrivia()),
                    argumentList: EmptyParams)
                );

        [Fact]
        public void TestNoParams_SpaceTrivia2()
            => Verify(
                @"  ConsoleApp4.MyClass.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: SpaceTrivia(2)),
                    argumentList: EmptyParams)
                );

        [Fact]
        public void TestMethodOneParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string s)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ParameterList(
                        OpenParenToken,
                        CloseParenToken,
                        Parameter(
                            Identifier("string"),
                            Identifier("s", leadingTrivia: SpaceTrivia())))
                    )
                );

        [Fact]
        public void TestMethodTwoParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string s, string t)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ParameterList(
                        OpenParenToken,
                        CloseParenToken,
                        Parameter(
                            Identifier("string"),
                            Identifier("s", leadingTrivia: SpaceTrivia())),
                        Parameter(
                            Identifier("string", leadingTrivia: SpaceTrivia()),
                            Identifier("t", leadingTrivia: SpaceTrivia())))
                    )
                );

        [Fact]
        public void TestMethodArrayParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string[] s)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ParameterList(
                        OpenParenToken,
                        CloseParenToken,
                            Parameter(ArrayExpression(Identifier("string"), ArrayRankSpecifier(trailingTrivia: SpaceTrivia())),
                            Identifier("s")))
                )
            );

        [Fact]
        public void TestCommaArrayParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string[,] s)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ParameterList(
                        OpenParenToken,
                        CloseParenToken,
                        Parameter(
                            ArrayExpression(Identifier("string"), ArrayRankSpecifier(1, trailingTrivia: SpaceTrivia())),
                            Identifier("s")))
                )
            );

        [Fact]
        public void TestInvalidParameterIdentifier_MemberAccess()
            => Verify("at ConsoleApp4.MyClass(string my.string.name)", expectFailure: true);

        [Fact]
        public void TestInvalidParameterIdentifier_TypeArity()
            => Verify("at ConsoleApp4.MyClass(string s`1)", expectFailure: true);

        [Fact]
        public void TestGenericMethod_Brackets()
            => Verify(
                @"at ConsoleApp4.MyClass.M[T](T t)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                            Identifier("MyClass")),
                        Identifier("M")),
                    typeArguments: TypeArgumentList(TypeArgument("T")),
                    argumentList: ParameterList(
                        OpenParenToken,
                        CloseParenToken,
                        Parameter(
                            Identifier("T"),
                            Identifier("t", leadingTrivia: SpaceTrivia())))
                )
            );

        [Fact]
        public void TestGenericMethod()
            => Verify(
                @"at ConsoleApp4.MyClass.M<T>(T t)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                            Identifier("MyClass")),
                        Identifier("M")),
                    typeArguments: TypeArgumentList(useBrackets: false, TypeArgument("T")),
                    argumentList: ParameterList(
                        OpenParenToken,
                        CloseParenToken,
                        Parameter(
                            Identifier("T"),
                            Identifier("t", leadingTrivia: SpaceTrivia())))
                )
            );

        [Theory]
        [InlineData("_")]
        [InlineData("_s")]
        [InlineData("S0m3th1ng")]
        [InlineData("ü")] // Unicode character
        [InlineData("uʶ")] // character and modifier character
        [InlineData("a\u00AD")] // Soft hyphen formatting character
        [InlineData("a‿")] // Connecting punctuation (combining character)

        public void TestIdentifierNames(string identifierName)
            => Verify(
                @$"at {identifierName}.{identifierName}[{identifierName}]({identifierName} {identifierName})",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression($"{identifierName}.{identifierName}", leadingTrivia: AtTrivia),
                    typeArguments: TypeArgumentList(TypeArgument(identifierName)),
                    argumentList: ParameterList(
                        OpenParenToken,
                        CloseParenToken,
                        Parameter(
                            Identifier(identifierName),
                            Identifier(identifierName, leadingTrivia: SpaceTrivia())))
                )
            );

        [Fact]
        public void TestAnonymousMethod()
            => Verify(
                @"Microsoft.VisualStudio.DesignTools.SurfaceDesigner.Tools.EventRouter.ScopeElement_MouseUp.AnonymousMethod__0()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("Microsoft.VisualStudio.DesignTools.SurfaceDesigner.Tools.EventRouter.ScopeElement_MouseUp.AnonymousMethod__0"),
                    argumentList: EmptyParams)
            );

        [Fact]
        public void TestFileInformation()
            => Verify(
                @"M.M() in C:\folder\m.cs:line 1",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: EmptyParams),

                fileInformation: FileInformation(
                    Path(@"C:\folder\m.cs"),
                    ColonToken,
                    Line(1))
            );

        [Fact]
        public void TestFileInformation_PartialPath()
            => Verify(@"M.M() in C:\folder\m.cs:line", expectFailure: true);

        [Fact]
        public void TestFileInformation_PartialPath2()
            => Verify(@"M.M() in C:\folder\m.cs:", expectFailure: true);

        [Fact]
        public void TestFileInformation_PartialPath3()
            => Verify(@"M.M() in C:\folder\m.cs:[trailingtrivia]", expectFailure: true);

        [Theory]
        [InlineData(@"C:\folder\m.cs", 1)]
        [InlineData(@"m.cs", 1)]
        [InlineData(@"C:\folder\m.cs", 123456789)]
        [InlineData(@"..\m.cs", 1)]
        [InlineData(@".\m.cs", 1)]
        public void TestFilePaths(string path, int line)
            => Verify(
                $"M.M() in {path}:line {line}",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: EmptyParams),

                fileInformation: FileInformation(
                    Path(path),
                    ColonToken,
                    Line(line))
            );

        [Fact]
        public void TestFileInformation_TrailingTrivia()
            => Verify(
                @"M.M() in C:\folder\m.cs:line 1[trailingtrivia]",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: EmptyParams),

                fileInformation: FileInformation(
                    Path(@"C:\folder\m.cs"),
                    ColonToken,
                    Line(1).With(trailingTrivia: CreateTriviaArray("[trailingtrivia]"))),

                eolTokenOpt: EOLToken
            );

        [Fact]
        public void TestFileInformation_InvalidDirectory()
            => Verify(@"M.M() in <\m.cs", expectFailure: true);

        [Theory]
        [InlineData("")]
        [InlineData("lkasjdlkfjalskdfj")]
        [InlineData("\n")]
        [InlineData("at ")]
        [InlineData(@"at M()")] // Method with no class is invalid
        [InlineData(@"at M.1c()")] // Invalid start character for identifier
        [InlineData(@"at 1M.C()")]
        [InlineData(@"at M.C(string& s)")] // "string&" represents a reference (ref, out) and is not supported yet
        [InlineData(@"at StreamJsonRpc.JsonRpc.<InvokeCoreAsync>d__139`1.MoveNext()")] // Generated/Inline methods are not supported yet
        [InlineData(@"at M(")] // Missing closing paren
        [InlineData(@"at M)")] // MIssing open paren
        [InlineData(@"at M.M[T>(T t)")] // Mismatched generic opening/close
        [InlineData(@"at M.M<T](T t)")] // Mismatched generic opening/close
        [InlineData(@"at M.M(string[ s)")] // Opening array bracket no close
        [InlineData(@"at M.M(string] s)")] // Close only array bracket
        [InlineData(@"at M.M(string[][][ s)")]
        [InlineData(@"at M.M(string[[]] s)")]
        [InlineData(@"at M.N`.P()")] // Missing numeric for arity 
        [InlineData(@"at M.N`9N.P()")] // Invalid character after arity
        public void TestInvalidInputs(string input)
            => Verify(input, expectFailure: true);
    }
}

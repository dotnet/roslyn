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
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: CreateTriviaArray(AtTrivia)),
                    argumentList: ArgumentList())
                );

        [Fact]
        public void TestTrailingTrivia()
            => Verify(
                @"at ConsoleApp4.MyClass.M() some other text",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: CreateTriviaArray(AtTrivia)),
                    argumentList: ArgumentList()),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(" some other text"))
                );

        [Fact]
        public void TestTrailingTrivia_InTriviaNoSpace()
            => Verify(
                @"at ConsoleApp4.MyClass.M() inC:\My\Path\C.cs:line 26",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: CreateTriviaArray(AtTrivia)),
                    argumentList: ArgumentList()),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(@" inC:\My\Path\C.cs:line 26"))
                );

        [Fact]
        public void TestTrailingTrivia_InTriviaNoSpace2()
            => Verify(
                @"at ConsoleApp4.MyClass.M()in C:\My\Path\C.cs:line 26",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: CreateTriviaArray(AtTrivia)),
                    argumentList: ArgumentList()),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(@"in C:\My\Path\C.cs:line 26"))
                );

        [Fact]
        public void TestNoParams_NoAtTrivia()
            => Verify(
                @"ConsoleApp4.MyClass.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M"),
                    argumentList: ArgumentList())
                );

        [Fact]
        public void TestNoParams_SpaceInParams_NoAtTrivia()
            => Verify(
                @"ConsoleApp4.MyClass.M(  )",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M"),
                    argumentList: ArgumentList(
                        OpenParenToken.With(trailingTrivia: CreateTriviaArray(SpaceTrivia(2))),
                        CloseParenToken))
                );

        [Fact]
        public void TestNoParams_SpaceTrivia()
            => Verify(
                @" ConsoleApp4.MyClass.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: CreateTriviaArray(SpaceTrivia())),
                    argumentList: ArgumentList())
                );

        [Fact]
        public void TestNoParams_SpaceTrivia2()
            => Verify(
                @"  ConsoleApp4.MyClass.M()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("ConsoleApp4.MyClass.M", leadingTrivia: CreateTriviaArray(SpaceTrivia(2))),
                    argumentList: ArgumentList())
                );

        [Fact]
        public void TestMethodOneParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string s)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: CreateTriviaArray(AtTrivia)),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ArgumentList(
                        Identifier("string", trailingTrivia: CreateTriviaArray(SpaceTrivia())),
                        Identifier("s"))
                    )
                );

        [Fact]
        public void TestMethodTwoParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string s, string t)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: CreateTriviaArray(AtTrivia)),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ArgumentList(
                        Identifier("string", trailingTrivia: CreateTriviaArray(SpaceTrivia())),
                        Identifier("s"),
                        CommaToken,
                        Identifier("string", leadingTrivia: CreateTriviaArray(SpaceTrivia()), trailingTrivia: CreateTriviaArray(SpaceTrivia())),
                        Identifier("t"))
                    )
                );

        [Fact]
        public void TestMethodArrayParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string[] s)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: CreateTriviaArray(AtTrivia)),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ArgumentList(
                        ArrayExpression(Identifier("string"), OpenBracketToken, CloseBracketToken.With(trailingTrivia: CreateTriviaArray(SpaceTrivia()))),
                        Identifier("s"))
                )
            );

        [Fact]
        public void TestCommaArrayParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string[,] s)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: CreateTriviaArray(AtTrivia)),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ArgumentList(
                        ArrayExpression(Identifier("string"), OpenBracketToken, CommaToken, CloseBracketToken.With(trailingTrivia: CreateTriviaArray(SpaceTrivia()))),
                        Identifier("s"))
                )
            );

        [Fact]
        public void TestGenericMethod_Brackets()
            => Verify(
                @"at ConsoleApp4.MyClass.M[T](T t)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: CreateTriviaArray(AtTrivia)),
                            Identifier("MyClass")),
                        Identifier("M")),
                    typeArguments: TypeArgumentList(TypeArgument("T")),
                    argumentList: ArgumentList(
                        Identifier("T", trailingTrivia: CreateTriviaArray(SpaceTrivia())),
                        Identifier("t"))
                )
            );

        [Fact]
        public void TestGenericMethod()
            => Verify(
                @"at ConsoleApp4.MyClass.M<T>(T t)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4", leadingTrivia: CreateTriviaArray(AtTrivia)),
                            Identifier("MyClass")),
                        Identifier("M")),
                    typeArguments: TypeArgumentList(useBrackets: false, TypeArgument("T")),
                    argumentList: ArgumentList(
                        Identifier("T", trailingTrivia: CreateTriviaArray(SpaceTrivia())),
                        Identifier("t"))
                )
            );

        [Theory]
        [InlineData("_")]
        [InlineData("_s")]
        [InlineData("S0m3th1ng")]
        [InlineData("ü")] // Unicode character
        [InlineData("uʶ")] // character and modifier character
        [InlineData("a\u00AD")] // Soft hyphen formatting character
        [InlineData("a‿")] // Connecting punctuation (combining character

        public void TestIdentifierNames(string identifierName)
            => Verify(
                @$"at {identifierName}.{identifierName}[{identifierName}]({identifierName} {identifierName})",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression($"{identifierName}.{identifierName}", leadingTrivia: CreateTriviaArray(AtTrivia)),
                    typeArguments: TypeArgumentList(TypeArgument(identifierName)),
                    argumentList: ArgumentList(
                        Identifier(identifierName, trailingTrivia: CreateTriviaArray(SpaceTrivia())),
                        Identifier(identifierName))
                )
            );

        [Fact]
        public void TestAnonymousMethod()
            => Verify(
                @"Microsoft.VisualStudio.DesignTools.SurfaceDesigner.Tools.EventRouter.ScopeElement_MouseUp.AnonymousMethod__0()",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("Microsoft.VisualStudio.DesignTools.SurfaceDesigner.Tools.EventRouter.ScopeElement_MouseUp.AnonymousMethod__0"),
                    argumentList: ArgumentList())
            );

        [Fact]
        public void TestFileInformation()
            => Verify(
                @"M.M() in C:\folder\m.cs:line 1",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: ArgumentList()),

                fileInformation: FileInformation(
                    Path(@"C:\folder\m.cs"),
                    ColonToken,
                    Line(1))
            );

        [Fact]
        public void TestFileInformation_PartialPath()
            => Verify(
                @"M.M() in C:\folder\m.cs:line",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: ArgumentList()),

                fileInformation: FileInformation(
                    Path(@"C:\folder\m.cs").With(trailingTrivia: CreateTriviaArray(":"))),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray("line")
                )
            );

        [Fact]
        public void TestFileInformation_PartialPath2()
            => Verify(
                @"M.M() in C:\folder\m.cs:",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: ArgumentList()),

                fileInformation: FileInformation(
                    Path(@"C:\folder\m.cs").With(trailingTrivia: CreateTriviaArray(":"))
                )
            );

        [Fact]
        public void TestFileInformation_TrailingTrivia()
            => Verify(
                @"M.M() in C:\folder\m.cs:line 1[trailingtrivia]",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: ArgumentList()),

                fileInformation: FileInformation(
                    Path(@"C:\folder\m.cs"),
                    ColonToken,
                    Line(1)),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray("[trailingtrivia]"))
            );

        [Fact]
        public void TestFileInformation_TrailingTrivia2()
            => Verify(
                @"M.M() in C:\folder\m.cs:[trailingtrivia]",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: ArgumentList()),

                fileInformation: FileInformation(
                    Path(@"C:\folder\m.cs").With(trailingTrivia: CreateTriviaArray(":"))),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray("[trailingtrivia]"))
            );

        [Fact]
        public void TestFileInformation_InvalidDirectory()
            => Verify(
                @"M.M() in C:\<\m.cs",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("M.M"),
                    argumentList: ArgumentList()),

                eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(" in ", @"C:\", @"<\m.cs"))
            );

        [Theory]
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

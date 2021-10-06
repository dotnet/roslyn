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
                        ArrayExpression(Identifier("string"), OpenBracketToken, CloseBracketToken),
                        Identifier("s", leadingTrivia: CreateTriviaArray(SpaceTrivia())))
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

        [Fact]
        public void TestUnderscoreNames()
            => Verify(
                @"at _._[_](_ _)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression("_._", leadingTrivia: CreateTriviaArray(AtTrivia)),
                    typeArguments: TypeArgumentList(TypeArgument("_")),
                    argumentList: ArgumentList(
                        Identifier("_", trailingTrivia: CreateTriviaArray(SpaceTrivia())),
                        Identifier("_"))
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

        [Theory]
        [InlineData(@"at M()")] // Method with no class is invalid
        [InlineData(@"at M.1c()")] // Invalid start character for identifier
        [InlineData(@"at 1M.C()")]
        [InlineData(@"at M.C(string& s)")] // "string&" represents a reference (ref, out) and is not supported yet
        [InlineData(@"at StreamJsonRpc.JsonRpc.<InvokeCoreAsync>d__139`1.MoveNext()")] // Generated/Inline methods are not supported yet
        public void TestInvalidInputs(string input)
            => Verify(input, expectFailure: true);
    }
}

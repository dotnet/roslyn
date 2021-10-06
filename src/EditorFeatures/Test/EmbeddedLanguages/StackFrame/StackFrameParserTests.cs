// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    public partial class StackFrameParserTests
    {
        [Fact]
        public void TestMethodOneParam()
            => Verify(
                @"at ConsoleApp4.MyClass.M(string s)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4"),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ArgumentList(
                        Identifier("string", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
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
                            Identifier("ConsoleApp4"),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ArgumentList(
                        Identifier("string", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                        Identifier("s"),
                        CommaToken,
                        Identifier("string", leadingTrivia: CreateTriviaArray(SpaceTrivia), trailingTrivia: CreateTriviaArray(SpaceTrivia)),
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
                            Identifier("ConsoleApp4"),
                            Identifier("MyClass")),
                        Identifier("M")),

                    argumentList: ArgumentList(
                        ArrayExpression(Identifier("string"), OpenBracketToken, CloseBracketToken),
                        Identifier("s", leadingTrivia: CreateTriviaArray(SpaceTrivia)))
                )
            );

        [Fact]
        public void TestGenericMethod_Brackets()
            => Verify(
                @"at ConsoleApp4.MyClass.M[T](T t)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        MemberAccessExpression(
                            Identifier("ConsoleApp4"),
                            Identifier("MyClass")),
                        Identifier("M")),
                    typeArguments: TypeArgumentList(TypeArgument("T")),
                    argumentList: ArgumentList(
                        Identifier("T", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
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
                            Identifier("ConsoleApp4"),
                            Identifier("MyClass")),
                        Identifier("M")),
                    typeArguments: TypeArgumentList(useBrackets: false, TypeArgument("T")),
                    argumentList: ArgumentList(
                        Identifier("T", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                        Identifier("t"))
                )
            );

        [Fact]
        public void TestUnderscoreNames()
            => Verify(
                @"at _._[_](_ _)",
                methodDeclaration: MethodDeclaration(
                    MemberAccessExpression(
                        Identifier("_"),
                        Identifier("_")),
                    typeArguments: TypeArgumentList(TypeArgument("_")),
                    argumentList: ArgumentList(
                        Identifier("_", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                        Identifier("_"))
                )
            );

        [Theory]
        [InlineData(@"at M()")] // Method with no class is invalid
        [InlineData(@"at M.1c()")] // Invalid start character for identifier
        [InlineData(@"at 1M.C()")]
        [InlineData(@"at M.C(string& s)")] // "string&" represents a reference (ref, out) and is not supported yet
        public void TestInvalidInputs(string input)
            => Verify(input, expectFailure: true);
    }
}

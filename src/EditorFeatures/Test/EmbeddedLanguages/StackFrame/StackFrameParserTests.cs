// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    public partial class StackFrameParserTests
    {
        [Fact]
        public void TestMethodOneParam()
        {
            var input = @"at ConsoleApp4.MyClass.M(string s)";
            var tree = StackFrameParser.TryParse(input);
            AssertEx.NotNull(tree);

            Assert.True(tree.Root.AtTrivia.HasValue);
            Assert.False(tree.Root.InTrivia.HasValue);
            Assert.Null(tree.Root.FileInformationExpression);
            Assert.False(tree.Root.TrailingTrivia.HasValue);

            var methodDeclaration = tree.Root.MethodDeclaration;

            var expectedMethodDeclaration = MethodDeclaration(
                MemberAccessExpression(
                    MemberAccessExpression(
                        Identifier("ConsoleApp4"),
                        Identifier("MyClass")),
                    Identifier("M")),

                ArgumentList(
                    Identifier("string", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                    Identifier("s"))
                );

            AssertEqual(expectedMethodDeclaration, methodDeclaration);
        }

        [Fact]
        public void TestMethodTwoParam()
        {
            var input = @"at ConsoleApp4.MyClass.M(string s, string t)";
            var tree = StackFrameParser.TryParse(input);
            AssertEx.NotNull(tree);

            Assert.True(tree.Root.AtTrivia.HasValue);
            Assert.False(tree.Root.InTrivia.HasValue);
            Assert.Null(tree.Root.FileInformationExpression);
            Assert.False(tree.Root.TrailingTrivia.HasValue);

            var methodDeclaration = tree.Root.MethodDeclaration;

            var expectedMethodDeclaration = MethodDeclaration(
                MemberAccessExpression(
                    MemberAccessExpression(
                        Identifier("ConsoleApp4"),
                        Identifier("MyClass")),
                    Identifier("M")),

                ArgumentList(
                    Identifier("string", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                    Identifier("s"),
                    CommaToken,
                    Identifier("string", leadingTrivia: CreateTriviaArray(SpaceTrivia), trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                    Identifier("t"))
                );

            AssertEqual(expectedMethodDeclaration, methodDeclaration);
        }

        [Fact]
        public void TestMethodArrayParam()
        {
            var input = @"at ConsoleApp4.MyClass.M(string[] s)";
            var tree = StackFrameParser.TryParse(input);
            AssertEx.NotNull(tree);

            Assert.True(tree.Root.AtTrivia.HasValue);
            Assert.False(tree.Root.InTrivia.HasValue);
            Assert.Null(tree.Root.FileInformationExpression);
            Assert.False(tree.Root.TrailingTrivia.HasValue);

            var methodDeclaration = tree.Root.MethodDeclaration;

            var expectedMethodDeclaration = MethodDeclaration(
                MemberAccessExpression(
                    MemberAccessExpression(
                        Identifier("ConsoleApp4"),
                        Identifier("MyClass")),
                    Identifier("M")),

                ArgumentList(
                    ArrayExpression(Identifier("string"), OpenBracketToken, CloseBracketToken),
                    Identifier("s", leadingTrivia: CreateTriviaArray(SpaceTrivia)))
                );

            AssertEqual(expectedMethodDeclaration, methodDeclaration);
        }

        [Fact]
        public void TestGenericMethod_Brackets()
        {
            var input = @"at ConsoleApp4.MyClass.M[T](T t)";
            var tree = StackFrameParser.TryParse(input);
            AssertEx.NotNull(tree);

            Assert.True(tree.Root.AtTrivia.HasValue);
            Assert.False(tree.Root.InTrivia.HasValue);
            Assert.Null(tree.Root.FileInformationExpression);
            Assert.False(tree.Root.TrailingTrivia.HasValue);

            var methodDeclaration = tree.Root.MethodDeclaration;

            var expectedMethodDeclaration = MethodDeclaration(
                MemberAccessExpression(
                    MemberAccessExpression(
                        Identifier("ConsoleApp4"),
                        Identifier("MyClass")),
                    Identifier("M")),
                typeArgumnets: TypeArgumentList(useBrackets: true, TypeArgument("T")),
                argumentList: ArgumentList(
                    Identifier("T", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                    Identifier("t"))
                );

            AssertEqual(expectedMethodDeclaration, methodDeclaration);
        }

        [Fact]
        public void TestGenericMethod()
        {
            var input = @"at ConsoleApp4.MyClass.M<T>(T t)";
            var tree = StackFrameParser.TryParse(input);
            AssertEx.NotNull(tree);

            Assert.True(tree.Root.AtTrivia.HasValue);
            Assert.False(tree.Root.InTrivia.HasValue);
            Assert.Null(tree.Root.FileInformationExpression);
            Assert.False(tree.Root.TrailingTrivia.HasValue);

            var methodDeclaration = tree.Root.MethodDeclaration;

            var expectedMethodDeclaration = MethodDeclaration(
                MemberAccessExpression(
                    MemberAccessExpression(
                        Identifier("ConsoleApp4"),
                        Identifier("MyClass")),
                    Identifier("M")),
                typeArgumnets: TypeArgumentList(useBrackets: false, TypeArgument("T")),
                argumentList: ArgumentList(
                    Identifier("T", trailingTrivia: CreateTriviaArray(SpaceTrivia)),
                    Identifier("t"))
                );

            AssertEqual(expectedMethodDeclaration, methodDeclaration);
        }
    }
}

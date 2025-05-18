// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class IgnoredDirectiveParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    private const string FeatureName = "FileBasedProgram";

    [Theory, CombinatorialData]
    public void FeatureFlag(bool script)
    {
        var options = script ? TestOptions.Script : TestOptions.Regular;

        var source = """
            #!xyz
            #:name value
            """;

        VerifyTrivia();
        UsingTree(source, options,
            // (2,2): error CS9282: '#:' directives can be only used in file-based programs ('/feature:FileBasedProgram')
            // #:name value
            Diagnostic(ErrorCode.ERR_PPIgnoredNeedsFileBasedProgram, ":").WithLocation(2, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.ShebangDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ExclamationToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "xyz");
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "name value");
                    }
                }
            }
        }
        EOF();

        UsingTree(source, options.WithFeature(FeatureName));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.ShebangDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ExclamationToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "xyz");
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "name value");
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Semantics()
    {
        var source = """
            #!xyz
            #:name value
            System.Console.WriteLine(123);
            """;
        CompileAndVerify(source,
            parseOptions: TestOptions.Regular.WithFeature(FeatureName),
            expectedOutput: "123").VerifyDiagnostics();
    }

    [Fact]
    public void Api()
    {
        var source = """
            #:abc
            """;
        var root = SyntaxFactory.ParseCompilationUnit(source, options: TestOptions.Regular.WithFeature(FeatureName));
        var trivia = root.EndOfFileToken.GetLeadingTrivia().Single();
        Assert.Equal(SyntaxKind.IgnoredDirectiveTrivia, trivia.Kind());
        Assert.True(SyntaxFacts.IsPreprocessorDirective(trivia.Kind()));
        Assert.True(SyntaxFacts.IsTrivia(trivia.Kind()));
        var structure = (IgnoredDirectiveTriviaSyntax)trivia.GetStructure()!;
        Assert.Equal(":", structure.DirectiveNameToken.ToFullString());
        var messageTrivia = structure.EndOfDirectiveToken.GetLeadingTrivia().Single();
        Assert.Equal(SyntaxKind.PreprocessingMessageTrivia, messageTrivia.Kind());
        Assert.Equal("abc", messageTrivia.ToString());
        trivia.GetDiagnostics().Verify();
    }

    [Fact]
    public void Api_Diagnostics()
    {
        var source = """
            #if X
            #endif
            #:abc
            """;
        var root = SyntaxFactory.ParseCompilationUnit(source, options: TestOptions.Regular.WithFeature(FeatureName));
        var trivia = root.EndOfFileToken.GetLeadingTrivia().Last();
        Assert.Equal(SyntaxKind.IgnoredDirectiveTrivia, trivia.Kind());
        Assert.True(SyntaxFacts.IsPreprocessorDirective(trivia.Kind()));
        Assert.True(SyntaxFacts.IsTrivia(trivia.Kind()));
        var structure = (IgnoredDirectiveTriviaSyntax)trivia.GetStructure()!;
        Assert.Equal(":", structure.DirectiveNameToken.ToFullString());
        var messageTrivia = structure.EndOfDirectiveToken.GetLeadingTrivia().Single();
        Assert.Equal(SyntaxKind.PreprocessingMessageTrivia, messageTrivia.Kind());
        Assert.Equal("abc", messageTrivia.ToString());
        trivia.GetDiagnostics().Verify(
            // (3,2): error CS9283: '#:' directives cannot be after '#if' directive
            // #:abc
            Diagnostic(ErrorCode.ERR_PPIgnoredFollowsIf, ":").WithLocation(3, 2));
    }

    [Theory, CombinatorialData]
    public void ShebangNotFirst(bool script, bool featureFlag)
    {
        var options = script ? TestOptions.Script : TestOptions.Regular;

        if (featureFlag)
        {
            options = options.WithFeature(FeatureName);
        }

        var source = """
             #!xyz
            """;

        VerifyTrivia();
        UsingTree(source, options,
            // (1,2): error CS1024: Preprocessor directive expected
            //  #!xyz
            Diagnostic(ErrorCode.ERR_PPDirectiveExpected, "#").WithLocation(1, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.WhitespaceTrivia, " ");
                L(SyntaxKind.BadDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.SkippedTokensTrivia);
                        {
                            N(SyntaxKind.ExclamationToken);
                            N(SyntaxKind.IdentifierToken, "xyz");
                        }
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void AfterToken()
    {
        var source = """
            #:x
            M();
            #:y
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName),
            // (3,2): error CS9281: '#:' directives cannot be after first token in file
            // #:y
            Diagnostic(ErrorCode.ERR_PPIgnoredFollowsToken, ":").WithLocation(3, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M");
                            {
                                L(SyntaxKind.IgnoredDirectiveTrivia);
                                {
                                    N(SyntaxKind.HashToken);
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.EndOfDirectiveToken);
                                    {
                                        L(SyntaxKind.PreprocessingMessageTrivia, "x");
                                        T(SyntaxKind.EndOfLineTrivia, "\n");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                    {
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "y");
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void AfterIf()
    {
        var source = """
            #:x
            #if X
            #:y
            #endif
            #:z
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName),
            // (3,2): error CS9283: '#:' directives cannot be after '#if' directive
            // #:y
            Diagnostic(ErrorCode.ERR_PPIgnoredFollowsIf, ":").WithLocation(3, 2),
            // (5,2): error CS9283: '#:' directives cannot be after '#if' directive
            // #:z
            Diagnostic(ErrorCode.ERR_PPIgnoredFollowsIf, ":").WithLocation(5, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "x");
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.IfDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.IfKeyword);
                    {
                        T(SyntaxKind.WhitespaceTrivia, " ");
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "y");
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.EndIfDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.EndIfKeyword);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "z");
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void AfterComment()
    {
        var source = """
            #:x
            // comment
            #:y
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "x");
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.SingleLineCommentTrivia);
                L(SyntaxKind.EndOfLineTrivia, "\n");
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "y");
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void AfterDefine()
    {
        var source = """
            #:x
            #define y
            #:y
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "x");
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.DefineDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.DefineKeyword);
                    {
                        T(SyntaxKind.WhitespaceTrivia, " ");
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        T(SyntaxKind.EndOfLineTrivia, "\n");
                    }
                }
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "y");
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void SpaceBeforeHash()
    {
        var source = """
             #:x
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.WhitespaceTrivia, " ");
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.PreprocessingMessageTrivia, "x");
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void SpacesAfterHash()
    {
        var source = """
             # : x
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName),
            // (1,2): error CS1024: Preprocessor directive expected
            //  # : x
            Diagnostic(ErrorCode.ERR_PPDirectiveExpected, "#").WithLocation(1, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.WhitespaceTrivia, " ");
                L(SyntaxKind.BadDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    {
                        T(SyntaxKind.WhitespaceTrivia, " ");
                    }
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                    {
                        L(SyntaxKind.SkippedTokensTrivia);
                        {
                            N(SyntaxKind.ColonToken);
                            {
                                T(SyntaxKind.WhitespaceTrivia, " ");
                            }
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void NoMessage()
    {
        var source = """
            #:
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.IgnoredDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void NoColon()
    {
        var source = """
            #
            """;

        VerifyTrivia();
        UsingTree(source, TestOptions.Regular.WithFeature(FeatureName),
            // (1,1): error CS1024: Preprocessor directive expected
            // #
            Diagnostic(ErrorCode.ERR_PPDirectiveExpected, "#").WithLocation(1, 1));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EndOfFileToken);
            {
                L(SyntaxKind.BadDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.EndOfDirectiveToken);
                }
            }
        }
        EOF();
    }
}

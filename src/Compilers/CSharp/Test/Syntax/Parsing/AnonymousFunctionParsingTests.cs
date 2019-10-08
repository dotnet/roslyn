// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [WorkItem(275, "https://github.com/dotnet/csharplang/issues/275")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class AnonymousFunctionParsingTests : ParsingTests
    {
        public AnonymousFunctionParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void MultipleAsyncModifiersOnAnonymousMethod()
        {
            var test = @"
using System;

class C
{
    void M1()
    {
        Action v = async async delegate() { };
    }
}";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M1");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Action");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.AnonymousMethodExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.DelegateKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.Block);
                                                {
                                                    N(SyntaxKind.OpenBraceToken);
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (8,26): error CS1004: Duplicate 'async' modifier
                //         Action v = async async delegate() { };
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(8, 26),
                // (8,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action v = async async delegate() { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(8, 32));
        }

        [Fact]
        public void StaticAnonymousMethod()
        {
            var test = @"
using System;

class C
{
    void M1()
    {
        Action v = static delegate() { };
    }
}";

            UsingTree(test);



            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M1");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Action");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.AnonymousMethodExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.DelegateKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.Block);
                                                {
                                                    N(SyntaxKind.OpenBraceToken);
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (8,20): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Action v = static delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 20));
        }

        [Fact]
        public void StaticAsyncAnonymousMethod()
        {
            var test = @"
using System;

class C
{
    void M1()
    {
        Action v = static async delegate() { };
    }
}";

            UsingTree(test);



            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M1");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Action");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.AnonymousMethodExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.DelegateKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.Block);
                                                {
                                                    N(SyntaxKind.OpenBraceToken);
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (8,20): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 20),
                // (8,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(8, 33));
        }

        [Fact]
        public void AsyncStaticAnonymousMethod()
        {
            var test = @"
using System;

class C
{
    void M1()
    {
        Action v = static async delegate() { };
    }
}";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M1");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Action");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.AnonymousMethodExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.DelegateKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.Block);
                                                {
                                                    N(SyntaxKind.OpenBraceToken);
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (8,20): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 20),
                // (8,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(8, 33));
        }

        [Fact]
        public void MultipleStaticAnonymousMethod()
        {
            var test = @"
using System;

class C
{
    void M1()
    {
        Action v = static static delegate() { };
    }
}";

            UsingTree(test);



            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M1");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Action");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.AnonymousMethodExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.DelegateKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.Block);
                                                {
                                                    N(SyntaxKind.OpenBraceToken);
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (8,20): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Action v = static static delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 20),
                // (8,27): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Action v = static static delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 27),
                // (8,27): error CS1004: Duplicate 'static' modifier
                //         Action v = static static delegate() { };
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(8, 27));
        }
    }
}

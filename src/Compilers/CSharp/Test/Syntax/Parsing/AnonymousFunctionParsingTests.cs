// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

public class C
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
                    N(SyntaxKind.PublicKeyword);
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
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

public class C
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
                    N(SyntaxKind.PublicKeyword);
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (8,20): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Action v = static delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(8, 20));
        }

        [Fact]
        public void StaticAsyncAnonymousMethod()
        {
            var test = @"
using System;

public class C
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
                    N(SyntaxKind.PublicKeyword);
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (8,20): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(8, 20),
                // (8,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(8, 33));
        }

        [Fact]
        public void AsyncStaticAnonymousMethod()
        {
            var test = @"
using System;

public class C
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
                    N(SyntaxKind.PublicKeyword);
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (8,20): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(8, 20),
                // (8,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action v = static async delegate() { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(8, 33));
        }

        [Fact]
        public void MultipleStaticAnonymousMethod()
        {
            var test = @"
using System;

public class C
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
                    N(SyntaxKind.PublicKeyword);
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (8,20): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Action v = static static delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(8, 20),
                // (8,27): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Action v = static static delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(8, 27),
                // (8,27): error CS1004: Duplicate 'static' modifier
                //         Action v = static static delegate() { };
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(8, 27));
        }

        [Fact]
        public void SimpleLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;

public class C
{
    void M1()
    {
        Func<int, int> v = async => async;
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
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify();
        }

        [Fact]
        public void AsyncSimpleLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = async async => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,46): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = async async => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 46));
        }

        [Fact]
        public void StaticSimpleLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;

public class C
{
    void M1()
    {
        Func<int, int> v = static async => async;
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
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (8,28): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, int> v = static async => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(8, 28));
        }

        [Fact]
        public void StaticLambdaPassedAsArgument()
        {
            var test = @"M1(static x => x);";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,4): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // M1(static x => x);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 4)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M1");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.SimpleLambdaExpression);
                                {
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticLambdaPassedAsRefArgument()
        {
            var test = @"M1(ref static x => x);";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,8): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // M1(ref static x => x);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 8)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M1");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.SimpleLambdaExpression);
                                {
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticLambdaPassedAsLabeledArgument()
        {
            var test = @"M1(param: static x => x);";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,11): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // M1(param: static x => x);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 11)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M1");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "param");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.SimpleLambdaExpression);
                                {
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticAnonymousFunctionPassedAsArgument()
        {
            var test = @"M1(static delegate(int x) { });";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,4): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // M1(static delegate(int x) { });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 4)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M1");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.AnonymousMethodExpression);
                                {
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.ParameterList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Parameter);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticLambdaRankSpecifier()
        {
            var test = @"_ = new int[static x => x];";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,13): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // _ = new int[static x => x];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 13)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.SimpleLambdaExpression);
                                    {
                                        N(SyntaxKind.StaticKeyword);
                                        N(SyntaxKind.Parameter);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.EqualsGreaterThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Theory]
        [CombinatorialData]
        public void StaticLambdaRankSpecifier_Incomplete_01(bool useCsharp9)
        {
            var test = @"_ = new int[static];";

            UsingStatement(test, options: useCsharp9 ? TestOptions.Regular9 : TestOptions.Regular8,
                // (1,13): error CS1003: Syntax error, ',' expected
                // _ = new int[static];
                Diagnostic(ErrorCode.ERR_SyntaxError, "static").WithArguments(",").WithLocation(1, 13)
                );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ArrayCreationExpression);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void StaticLambdaRankSpecifier_Incomplete_02(bool useCsharp9)
        {
            var test = @"_ = new int[static x];";

            UsingStatement(test, options: useCsharp9 ? TestOptions.Regular9 : TestOptions.Regular8,
                // (1,13): error CS1003: Syntax error, ',' expected
                // _ = new int[static x];
                Diagnostic(ErrorCode.ERR_SyntaxError, "static").WithArguments(",").WithLocation(1, 13));

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ArrayCreationExpression);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void StaticLambdaRankSpecifier_Incomplete_03()
        {
            var test = @"_ = new int[static x =>];";

            UsingStatement(test,
                // (1,24): error CS1525: Invalid expression term ']'
                // _ = new int[static x =>];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "]").WithArguments("]").WithLocation(1, 24));
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,13): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // _ = new int[static x =>];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 13),
                // (1,24): error CS1525: Invalid expression term ']'
                // _ = new int[static x =>];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "]").WithArguments("]").WithLocation(1, 24)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.SimpleLambdaExpression);
                                    {
                                        N(SyntaxKind.StaticKeyword);
                                        N(SyntaxKind.Parameter);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.EqualsGreaterThanToken);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticDelegateRankSpecifier_Incomplete()
        {
            var test = @"_ = new int[static delegate];";

            UsingStatement(test,
                // (1,28): error CS1514: { expected
                // _ = new int[static delegate];
                Diagnostic(ErrorCode.ERR_LbraceExpected, "]").WithLocation(1, 28));
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,13): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // _ = new int[static delegate];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 13),
                // (1,28): error CS1514: { expected
                // _ = new int[static delegate];
                Diagnostic(ErrorCode.ERR_LbraceExpected, "]").WithLocation(1, 28)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.AnonymousMethodExpression);
                                    {
                                        N(SyntaxKind.StaticKeyword);
                                        N(SyntaxKind.DelegateKeyword);
                                        M(SyntaxKind.Block);
                                        {
                                            M(SyntaxKind.OpenBraceToken);
                                            M(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Theory]
        [CombinatorialData]
        public void StaticLambdaArrayInitializer_Incomplete_01(bool useCsharp9)
        {
            var test = @"_ = new Action[] { static }";

            UsingStatement(test, options: useCsharp9 ? TestOptions.Regular9 : TestOptions.Regular8,
                // (1,20): error CS1003: Syntax error, ',' expected
                // _ = new Action[] { static }
                Diagnostic(ErrorCode.ERR_SyntaxError, "static").WithArguments(",").WithLocation(1, 20),
                // (1,28): error CS1002: ; expected
                // _ = new Action[] { static }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 28)
                );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ArrayCreationExpression);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.ArrayInitializerExpression);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void StaticLambdaArrayInitializer_Incomplete_02(bool useCsharp9)
        {
            var test = @"_ = new Action[] { static x }";

            UsingStatement(test, options: useCsharp9 ? TestOptions.Regular9 : TestOptions.Regular8,
                // (1,20): error CS1003: Syntax error, ',' expected
                // _ = new Action[] { static x }
                Diagnostic(ErrorCode.ERR_SyntaxError, "static").WithArguments(",").WithLocation(1, 20),
                // (1,30): error CS1002: ; expected
                // _ = new Action[] { static x }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 30)
                );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ArrayCreationExpression);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.ArrayInitializerExpression);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void StaticLambdaArrayInitializer_Incomplete_03()
        {
            var test = @"_ = new Action[] { static x => }";

            UsingStatement(test,
                // (1,32): error CS1525: Invalid expression term '}'
                // _ = new Action[] { static x => }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(1, 32),
                // (1,33): error CS1002: ; expected
                // _ = new Action[] { static x => }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 33)
                );
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,20): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // _ = new Action[] { static x => }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 20),
                // (1,32): error CS1525: Invalid expression term '}'
                // _ = new Action[] { static x => }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(1, 32),
                // (1,33): error CS1002: ; expected
                // _ = new Action[] { static x => }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 33)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.ArrayInitializerExpression);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SimpleLambdaExpression);
                                {
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticDelegateArrayInitializer_Incomplete()
        {
            var test = @"_ = new Action[] { static delegate }";

            UsingStatement(test,
                // (1,36): error CS1514: { expected
                // _ = new Action[] { static delegate }
                Diagnostic(ErrorCode.ERR_LbraceExpected, "}").WithLocation(1, 36),
                // (1,37): error CS1002: ; expected
                // _ = new Action[] { static delegate }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 37)
                );
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,20): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // _ = new Action[] { static delegate }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 20),
                // (1,36): error CS1514: { expected
                // _ = new Action[] { static delegate }
                Diagnostic(ErrorCode.ERR_LbraceExpected, "}").WithLocation(1, 36),
                // (1,37): error CS1002: ; expected
                // _ = new Action[] { static delegate }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 37)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.ArrayInitializerExpression);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.AnonymousMethodExpression);
                                {
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.DelegateKeyword);
                                    M(SyntaxKind.Block);
                                    {
                                        M(SyntaxKind.OpenBraceToken);
                                        M(SyntaxKind.CloseBraceToken);
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticAnonymousFunctionRankSpecifier()
        {
            var test = @"_ = new int[static delegate(int x) { }];";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,13): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                // _ = new int[static delegate(int x) { }];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(1, 13)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.AnonymousMethodExpression);
                                    {
                                        N(SyntaxKind.StaticKeyword);
                                        N(SyntaxKind.DelegateKeyword);
                                        N(SyntaxKind.ParameterList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Parameter);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.Block);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void LambdaFunctionPointer()
        {
            var test = @"delegate*<void> ptr = &() => { };";

            UsingStatement(test,
                // (1,25): error CS1525: Invalid expression term ')'
                // delegate*<void> ptr = &() => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 25),
                // (1,27): error CS1003: Syntax error, ',' expected
                // delegate*<void> ptr = &() => { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(1, 27)
                );
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,1): error CS8400: Feature 'function pointers' is not available in C# 8.0. Please use language version 9.0 or greater.
                // delegate*<void> ptr = &() => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "delegate*<void>").WithArguments("function pointers", "9.0").WithLocation(1, 1),
                // (1,25): error CS1525: Invalid expression term ')'
                // delegate*<void> ptr = &() => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 25),
                // (1,27): error CS1003: Syntax error, ',' expected
                // delegate*<void> ptr = &() => { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(1, 27));
            verify();

            void verify()
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "ptr");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.AddressOfExpression);
                                {
                                    N(SyntaxKind.AmpersandToken);
                                    N(SyntaxKind.ParenthesizedExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticLambdaFunctionPointer()
        {
            var test = @"delegate*<void> ptr = &static () => { };";

            UsingStatement(test,
                // (1,1): error CS1073: Unexpected token ')'
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "delegate*<void> ptr = &static (").WithArguments(")").WithLocation(1, 1),
                // (1,24): error CS1525: Invalid expression term 'static'
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "static").WithArguments("static").WithLocation(1, 24),
                // (1,24): error CS1003: Syntax error, ',' expected
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "static").WithArguments(",").WithLocation(1, 24),
                // (1,32): error CS1002: ; expected
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(1, 32)
                );
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,1): error CS1073: Unexpected token ')'
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "delegate*<void> ptr = &static (").WithArguments(")").WithLocation(1, 1),
                // (1,1): error CS8400: Feature 'function pointers' is not available in C# 8.0. Please use language version 9.0 or greater.
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "delegate*<void>").WithArguments("function pointers", "9.0").WithLocation(1, 1),
                // (1,24): error CS1525: Invalid expression term 'static'
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "static").WithArguments("static").WithLocation(1, 24),
                // (1,24): error CS1003: Syntax error, ',' expected
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "static").WithArguments(",").WithLocation(1, 24),
                // (1,32): error CS1002: ; expected
                // delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(1, 32)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "ptr");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.AddressOfExpression);
                                {
                                    N(SyntaxKind.AmpersandToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void AnonymousMethodFunctionPointer()
        {
            var test = @"delegate*<void> ptr = &delegate() { };";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,1): error CS8400: Feature 'function pointers' is not available in C# 8.0. Please use language version 9.0 or greater.
                // delegate*<void> ptr = &delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "delegate*<void>").WithArguments("function pointers", "9.0").WithLocation(1, 1));
            verify();

            void verify()
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "ptr");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.AddressOfExpression);
                                {
                                    N(SyntaxKind.AmpersandToken);
                                    N(SyntaxKind.AnonymousMethodExpression);
                                    {
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
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void StaticAnonymousMethodFunctionPointer()
        {
            var test = @"delegate*<void> ptr = &delegate() { };";

            UsingStatement(test);
            verify();

            UsingStatement(test, options: TestOptions.Regular8,
                // (1,1): error CS8400: Feature 'function pointers' is not available in C# 8.0. Please use language version 9.0 or greater.
                // delegate*<void> ptr = &delegate() { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "delegate*<void>").WithArguments("function pointers", "9.0").WithLocation(1, 1)
                );
            verify();

            void verify()
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "ptr");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.AddressOfExpression);
                                {
                                    N(SyntaxKind.AmpersandToken);
                                    N(SyntaxKind.AnonymousMethodExpression);
                                    {
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
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void IncompleteAttributeFollowedByStaticMember()
        {
            var test = @"
class Program
{
    [ObsoleteAttribute(x
    static void Main()
    {
    }
}";
            var tree = UsingTree(test);
            tree.GetDiagnostics().Verify(
                // (4,25): error CS1026: ) expected
                //     [ObsoleteAttribute(x
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 25),
                // (4,25): error CS1003: Syntax error, ']' expected
                //     [ObsoleteAttribute(x
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(4, 25)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "ObsoleteAttribute");
                                }
                                N(SyntaxKind.AttributeArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.AttributeArgument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void IncompleteAttributeFollowedByStaticAsyncMember()
        {
            var test = @"
class Program
{
    [ObsoleteAttribute(x
    async static Task Main()
    {
    }
}";
            var tree = UsingTree(test);
            tree.GetDiagnostics().Verify(
                // (4,25): error CS1003: Syntax error, ',' expected
                //     [ObsoleteAttribute(x
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 25),
                // (5,11): error CS1026: ) expected
                //     async static Task Main()
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "static").WithLocation(5, 11),
                // (5,11): error CS1003: Syntax error, ']' expected
                //     async static Task Main()
                Diagnostic(ErrorCode.ERR_SyntaxError, "static").WithArguments("]").WithLocation(5, 11)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "ObsoleteAttribute");
                                }
                                N(SyntaxKind.AttributeArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.AttributeArgument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                    }
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.AttributeArgument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "async");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Task");
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void StaticAsyncSimpleLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = static async async => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,34): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, Task<int>> v = static async async => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(9, 34),
                // (9,53): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = static async async => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 53));
        }

        [Fact]
        public void AsyncStaticSimpleLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = async static async => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,40): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, Task<int>> v = async static async => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(9, 40),
                // (9,53): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = async static async => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 53));
        }

        [Fact]
        public void AsyncStaticAsyncSimpleLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = async static async async => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,40): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, Task<int>> v = async static async async => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(9, 40),
                // (9,47): error CS1004: Duplicate 'async' modifier
                //         Func<int, Task<int>> v = async static async async => async;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(9, 47),
                // (9,59): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = async static async async => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 59));
        }

        [Fact]
        public void ParenthesizedLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;

public class C
{
    void M1()
    {
        Func<int, int> v = (async) => async;
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
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Parameter);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "async");
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify();
        }

        [Fact]
        public void AsyncParenthesizedLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = async (async) => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Parameter);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "async");
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,48): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = async (async) => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 48));
        }

        [Fact]
        public void StaticParenthesizedLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;

public class C
{
    void M1()
    {
        Func<int, int> v = static (async) => async;
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
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Parameter);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "async");
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (8,28): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, int> v = static (async) => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(8, 28));
        }

        [Fact]
        public void StaticAsyncParenthesizedLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = static async (async) => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Parameter);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "async");
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,34): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, Task<int>> v = static async (async) => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(9, 34),
                // (9,55): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = static async (async) => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 55));
        }

        [Fact]
        public void AsyncStaticParenthesizedLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = async static (async) => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Parameter);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "async");
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,40): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, Task<int>> v = async static (async) => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(9, 40),
                // (9,55): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = async static (async) => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 55));
        }

        [Fact]
        public void AsyncStaticAsyncParenthesizedLambdaWithParameterCalledAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    void M1()
    {
        Func<int, Task<int>> v = async static async (async) => async;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Parameter);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "async");
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,40): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int, Task<int>> v = async static async (async) => async;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(9, 40),
                // (9,47): error CS1004: Duplicate 'async' modifier
                //         Func<int, Task<int>> v = async static async (async) => async;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(9, 47),
                // (9,61): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> v = async static async (async) => async;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(9, 61));
        }

        [Fact]
        public void ParenthesizedLambdaWithNoParameters()
        {
            var test = @"
using System;

public class C
{
    public static int a;
    void M1()
    {
        Func<int> v = () => a;
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
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify();
        }

        [Fact]
        public void AsyncParenthesizedLambdaWithNoParameters()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    public static int a;
    void M1()
    {
        Func<Task<int>> v = async () => a;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (10,38): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<Task<int>> v = async () => a;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(10, 38));
        }

        [Fact]
        public void StaticParenthesizedLambdaWithNoParameters()
        {
            var test = @"
using System;

public class C
{
    public static int a;
    void M1()
    {
        Func<int> v = static () => a;
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
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (9,23): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int> v = static () => a;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(9, 23));
        }

        [Fact]
        public void StaticAsyncParenthesizedLambdaWithNoParameters()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    public static int a;
    void M1()
    {
        Func<Task<int>> v = static async () => a;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (10,29): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<Task<int>> v = static async () => a;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(10, 29),
                // (10,45): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<Task<int>> v = static async () => a;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(10, 45));
        }

        [Fact]
        public void AsyncStaticParenthesizedLambdaWithNoParameters()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    public static int a;
    void M1()
    {
        Func<Task<int>> v = async static () => a;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (10,35): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<Task<int>> v = async static () => a;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(10, 35),
                // (10,45): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<Task<int>> v = async static () => a;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(10, 45));
        }

        [Fact]
        public void AsyncStaticAsyncParenthesizedLambdaWithNoParameters()
        {
            var test = @"
using System;
using System.Threading.Tasks;

public class C
{
    public static int a;
    void M1()
    {
        Func<Task<int>> v = async static async () => a;
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
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Threading");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Tasks");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Func");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedLambdaExpression);
                                            {
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.StaticKeyword);
                                                N(SyntaxKind.AsyncKeyword);
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test, parseOptions: TestOptions.Regular8).GetDiagnostics().Verify(
                // (10,35): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<Task<int>> v = async static async () => a;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(10, 35),
                // (10,42): error CS1004: Duplicate 'async' modifier
                //         Func<Task<int>> v = async static async () => a;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(10, 42),
                // (10,51): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<Task<int>> v = async static async () => a;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(10, 51));
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public class FunctionPointerTests : ParsingTests
    {
        public FunctionPointerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void SimpleFunctionPointerTest()
        {
            UsingStatement("delegate*<string, Goo, int> ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Goo");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [InlineData("cdecl", SyntaxKind.CdeclKeyword)]
        [InlineData("managed", SyntaxKind.ManagedKeyword)]
        [InlineData("stdcall", SyntaxKind.StdcallKeyword)]
        [InlineData("thiscall", SyntaxKind.ThiscallKeyword)]
        [InlineData("unmanaged", SyntaxKind.UnmanagedKeyword)]
        public void SupportedCallingConventions(string conventionString, SyntaxKind conventionKind)
        {
            UsingStatement($"delegate* {conventionString}<string, Goo, int> ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(conventionKind);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Goo");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void InvalidCallingConventionTupleReturnType()
        {
            UsingStatement($"delegate* invalidcallingconvention<int, string> ptr;", options: TestOptions.RegularPreview,
                    // (1,11): error CS8752: 'invalidcallingconvention' is not a valid calling convention for a function pointer. Valid conventions are 'cdecl', 'managed', 'unmanaged', 'thiscall', and 'stdcall'.
                    // delegate* invalidcallingconvention<int, string> ptr;
                    Diagnostic(ErrorCode.ERR_InvalidFunctionPointerCallingConvention, "invalidcallingconvention").WithArguments("invalidcallingconvention").WithLocation(1, 11));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.CdeclKeyword);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void LangVersion8()
        {
            UsingStatement("delegate* cdecl<string, Goo, int> ptr;", options: TestOptions.Regular8,
                    // (1,1): error CS8652: The feature 'function pointers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // delegate* cdecl<string, Goo, int> ptr;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate* cdecl<string, Goo, int>").WithArguments("function pointers").WithLocation(1, 1));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.CdeclKeyword);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Goo");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void VoidsAsType()
        {
            // Void isn't allowed in anything but the return type, but that's a semantic error, not a syntax error
            UsingStatement("delegate*<void, void, void> ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void NestedFunctionPointers()
        {
            UsingStatement("delegate*<delegate* cdecl<int*, void*>, delegate* managed<string*>> ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.FunctionPointerType);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.CdeclKeyword);
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                {
                                    N(SyntaxKind.PointerType);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.AsteriskToken);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                {
                                    N(SyntaxKind.PointerType);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.VoidKeyword);
                                        }
                                        N(SyntaxKind.AsteriskToken);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.FunctionPointerType);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.ManagedKeyword);
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                {
                                    N(SyntaxKind.PointerType);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.AsteriskToken);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void PointerToAFunctionPointer()
        {
            UsingStatement("delegate*<Goo, void>* ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PointerType);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Goo");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.AsteriskToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RefModifiers()
        {
            UsingStatement("delegate*<ref Goo, in Bar, out Baz, readonly ref void*> ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Goo");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.InKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Bar");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.OutKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Baz");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                                N(SyntaxKind.AsteriskToken);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated()
        {
            UsingNode(@"
class C
{
    delegate*< ;
    delegate*<ref ;
    delegate*<ref bar ;
    delegate*<ref bar, ;
}", options: TestOptions.RegularPreview,
                    // (4,16): error CS1031: Type expected
                    //     delegate*< ;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(4, 16),
                    // (4,16): error CS1003: Syntax error, '>' expected
                    //     delegate*< ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">", ";").WithLocation(4, 16),
                    // (4,16): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     delegate*< ;
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 16),
                    // (5,19): error CS1031: Type expected
                    //     delegate*<ref ;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(5, 19),
                    // (5,19): error CS1003: Syntax error, '>' expected
                    //     delegate*<ref ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">", ";").WithLocation(5, 19),
                    // (5,19): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     delegate*<ref ;
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 19),
                    // (6,23): error CS1003: Syntax error, '>' expected
                    //     delegate*<ref bar ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">", ";").WithLocation(6, 23),
                    // (6,23): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     delegate*<ref bar ;
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(6, 23),
                    // (7,24): error CS1031: Type expected
                    //     delegate*<ref bar, ;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(7, 24),
                    // (7,24): error CS1003: Syntax error, '>' expected
                    //     delegate*<ref bar, ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">", ";").WithLocation(7, 24),
                    // (7,24): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     delegate*<ref bar, ;
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(7, 24));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.RefKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "bar");
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "bar");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            M(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void NoParamOrReturnTypes()
        {
            UsingStatement("delegate*<> ptr;", options: TestOptions.RegularPreview,
                    // (1,11): error CS1031: Type expected
                    // delegate*<> ptr;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(1, 11));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        M(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void UsingParensInsteadOfAngles()
        {
            UsingStatement("delegate*(int, void)", options: TestOptions.RegularPreview,
                    // (1,10): error CS1003: Syntax error, '<' expected
                    // delegate*(int, void)
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("<", "(").WithLocation(1, 10),
                    // (1,16): error CS1547: Keyword 'void' cannot be used in this context
                    // delegate*(int, void)
                    Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(1, 16),
                    // (1,21): error CS1003: Syntax error, '>' expected
                    // delegate*(int, void)
                    Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">", "").WithLocation(1, 21),
                    // (1,21): error CS1001: Identifier expected
                    // delegate*(int, void)
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 21),
                    // (1,21): error CS1002: ; expected
                    // delegate*(int, void)
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 21));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.GreaterThanToken);
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void MethodTypes()
        {
            UsingTree(@"
class C
{
    public delegate*<int, string> M(delegate*<C, void> param1, delegate* cdecl<D> param2) {}
}",
                options: TestOptions.RegularPreview);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.VoidKeyword);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                                N(SyntaxKind.IdentifierToken, "param1");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.CdeclKeyword);
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "D");
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                                N(SyntaxKind.IdentifierToken, "param2");
                            }
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
        public void HardCast()
        {
            UsingExpression("(delegate* thiscall<int, C>)ptr", options: TestOptions.RegularPreview);
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.ThiscallKeyword);
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "ptr");
                }
            }
            EOF();
        }

        [Fact]
        public void AsCast()
        {
            UsingExpression("ptr as delegate* stdcall<int, void>", options: TestOptions.RegularPreview);
            N(SyntaxKind.AsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "ptr");
                }
                N(SyntaxKind.AsKeyword);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.StdcallKeyword);
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TupleType()
        {
            UsingExpression("((delegate*<int, void> i1, delegate* managed<C, D> i2))ptr", options: TestOptions.RegularPreview);
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.IdentifierToken, "i1");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.ManagedKeyword);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "D");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.IdentifierToken, "i2");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "ptr");
                }
            }
            EOF();
        }

        [Fact]
        public void GenericArguments()
        {
            UsingExpression("new M<delegate* thiscall<void>, delegate*<C, D>>()", options: TestOptions.RegularPreview);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.ThiscallKeyword);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "D");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TypeOf()
        {
            UsingExpression("typeof(delegate* cdecl<ref int, readonly ref D>)", options: TestOptions.RegularPreview);
            N(SyntaxKind.TypeOfExpression);
            {
                N(SyntaxKind.TypeOfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.CdeclKeyword);
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void ArrayType()
        {
            UsingStatement("delegate*<ref C>[] ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
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
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void DelegateTypes()
        {
            UsingNode(@"
class C
{
    delegate delegate* cdecl<void> M(delegate*<ref C, D> p);
}",
                options: TestOptions.RegularPreview);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.DelegateDeclaration);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.CdeclKeyword);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "D");
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                                N(SyntaxKind.IdentifierToken, "p");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void LambdaParameterType()
        {
            UsingExpression("(delegate*<void> p1) => {}", options: TestOptions.RegularPreview);
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.IdentifierToken, "p1");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void LocalVariableAndFunction()
        {
            UsingNode(@"
public void M()
{
    delegate*<void> l1;
    delegate*<void> L2() { }
    delegate*<void> l3;
}", options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
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
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.VoidKeyword);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "l1");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.FunctionPointerType);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                            N(SyntaxKind.IdentifierToken, "L2");
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
                        N(SyntaxKind.LocalDeclarationStatement);
                        {
                            N(SyntaxKind.VariableDeclaration);
                            {
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.VoidKeyword);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "l3");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void IsExpression()
        {
            UsingExpression("o is delegate*<void>", options: TestOptions.RegularPreview);
            N(SyntaxKind.IsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            EOF();
        }

        [Fact]
        public void IsNamedExpression()
        {
            UsingExpression("o is delegate*<void> ptr", options: TestOptions.RegularPreview);
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.DeclarationPattern);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.SingleVariableDesignation);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SwitchStatementCase()
        {
            UsingStatement(@"
switch (o)
{
    case delegate*<void> { } _:
    case delegate*<void> (var x, var y):
        break;
}", options: TestOptions.RegularPreview);

            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.FunctionPointerType);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.DiscardDesignation);
                            {
                                N(SyntaxKind.UnderscoreToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.FunctionPointerType);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameterOrReturnType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.VarPattern);
                                    {
                                        N(SyntaxKind.VarKeyword);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.VarPattern);
                                    {
                                        N(SyntaxKind.VarKeyword);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressions()
        {
            UsingExpression(@"
o switch
{
    delegate*<void> _ => 1,
    delegate*<void> (var a, 2) ptr => 2,
}", options: TestOptions.RegularPreview);

            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.DiscardDesignation);
                        {
                            N(SyntaxKind.UnderscoreToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameterOrReturnType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.VarPattern);
                                {
                                    N(SyntaxKind.VarKeyword);
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "2");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "ptr");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void UsingStatementType()
        {
            UsingStatement("using (delegate*<void> ptr = MyMethod()) {}", options: TestOptions.RegularPreview);
            N(SyntaxKind.UsingStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "MyMethod");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void UsingDeclarationType()
        {
            UsingStatement("using delegate*<void> ptr = MyMethod();", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "MyMethod");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
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

        [Fact]
        public void FixedStatement()
        {
            UsingStatement("fixed (delegate*<void> ptr = &MyMethod) {}", options: TestOptions.RegularPreview);
            N(SyntaxKind.FixedStatement);
            {
                N(SyntaxKind.FixedKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "MyMethod");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ForEachVariable()
        {
            UsingStatement("foreach (delegate*<void> ptr in ptrs) {}", options: TestOptions.RegularPreview);
            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.IdentifierToken, "ptr");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "ptrs");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ForVariable()
        {
            UsingStatement("for (delegate*<void> ptr = null;;) {}", options: TestOptions.RegularPreview);
            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameterOrReturnType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SizeOf()
        {
            UsingExpression("sizeof(delegate*<void>)", options: TestOptions.RegularPreview);
            N(SyntaxKind.SizeOfExpression);
            {
                N(SyntaxKind.SizeOfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.FunctionPointerParameterOrReturnType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }
    }
}

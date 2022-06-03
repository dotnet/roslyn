// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            UsingStatement("delegate*<string, Goo, int> ptr;", options: TestOptions.Regular9);
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
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Goo");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
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

        [Theory]
        [InlineData("Cdecl")]
        [InlineData("Stdcall")]
        [InlineData("Thiscall")]
        [InlineData("Fastcall")]
        [InlineData("invalidcallingconvention")] // This is a semantic error, not a syntax error
        public void UnmanagedCallingConventionSpecifiers(string conventionString)
        {
            UsingStatement($"delegate* unmanaged[{conventionString}]<string, Goo, int> ptr;", options: TestOptions.Regular9);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, conventionString);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.FunctionPointerParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Goo");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
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
        public void KeywordInCallingConventionList()
        {
            UsingStatement("delegate* unmanaged[void]<void> ptr;", options: TestOptions.RegularPreview,
                // (1,21): error CS1041: Identifier expected; 'void' is a keyword
                // delegate* unmanaged[void]<void> ptr;
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "void").WithArguments("", "void").WithLocation(1, 21),
                // (1,21): error CS1003: Syntax error, ',' expected
                // delegate* unmanaged[void]<void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "void").WithArguments(",").WithLocation(1, 21)
            );

            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                M(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [InlineData("managed")]
        [InlineData("unmanaged")]
        public void NoUnmanagedSpecifiers(string convention)
        {
            UsingStatement($"delegate* {convention}<void> ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(convention == "managed" ? SyntaxKind.ManagedKeyword : SyntaxKind.UnmanagedKeyword);
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ManagedWithUnmanagedSpecifiers()
        {
            UsingStatement("delegate* managed[Cdecl]<void> ptr;", options: TestOptions.RegularPreview,
                // (1,18): error CS8888: 'managed' calling convention cannot be combined with unmanaged calling convention specifiers.
                // delegate* managed[Cdecl]<void> ptr;
                Diagnostic(ErrorCode.ERR_CannotSpecifyManagedWithUnmanagedSpecifiers, "[Cdecl]").WithLocation(1, 18)
            );
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.ManagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void InvalidConventionWithUnmanagedSpecifiers()
        {
            UsingStatement("delegate* invalid[Cdecl]<void> ptr;", options: TestOptions.RegularPreview,
                // (1,11): error CS1003: Syntax error, 'unmanaged' expected
                // delegate* invalid[Cdecl]<void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "invalid").WithArguments("unmanaged").WithLocation(1, 11)
            );
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            M(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void InvalidConventionFollowedByTypeArguments()
        {
            UsingStatement("delegate* invalid<void> ptr;", options: TestOptions.RegularPreview,
                // (1,11): error CS1003: Syntax error, 'managed' expected
                // delegate* invalid<void>
                Diagnostic(ErrorCode.ERR_SyntaxError, "invalid").WithArguments("managed").WithLocation(1, 11));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            M(SyntaxKind.ManagedKeyword);
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void EmptyUnmanagedSpecifierBraces()
        {
            UsingStatement("delegate* unmanaged[]<void> ptr;", options: TestOptions.RegularPreview,
                // (1,21): error CS1001: Identifier expected
                // delegate* unmanaged[]<void> ptr;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "]").WithLocation(1, 21)
            );
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                M(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void MultipleUnmanagedSpecifiers()
        {
            UsingStatement("delegate* unmanaged[Cdecl, Thiscall, Stdcall, Fastcall, Vectorcall, SuppressGCTransition]<void> ptr;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Cdecl");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Thiscall");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Stdcall");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Fastcall");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Vectorcall");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "SuppressGCTransition");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void LangVersion8()
        {
            UsingStatement("delegate* unmanaged[cdecl]<string, Goo, int> ptr;", options: TestOptions.Regular8,
                // (1,1): error CS8400: Feature 'function pointers' is not available in C# 8.0. Please use language version 9.0 or greater.
                // delegate* unmanaged[cdecl]<string, Goo, int> ptr;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "delegate* unmanaged[cdecl]<string, Goo, int>").WithArguments("function pointers", "9.0").WithLocation(1, 1));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.FunctionPointerParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Goo");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
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
        public void VoidsAsType()
        {
            // Void isn't allowed in anything but the return type, but that's a semantic error, not a syntax error
            UsingStatement("delegate*<void, void, void> ptr;", options: TestOptions.Regular9);
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void NestedFunctionPointers()
        {
            UsingStatement("delegate*<delegate* unmanaged[cdecl]<int*, void*>, delegate* managed<string*>> ptr;", options: TestOptions.Regular9);
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
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.FunctionPointerCallingConvention);
                                    {
                                        N(SyntaxKind.UnmanagedKeyword);
                                        N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                            {
                                                N(SyntaxKind.IdentifierToken, "cdecl");
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.FunctionPointerParameterList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.FunctionPointerParameter);
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
                                        N(SyntaxKind.FunctionPointerParameter);
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
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.FunctionPointerCallingConvention);
                                    {
                                        N(SyntaxKind.ManagedKeyword);
                                    }
                                    N(SyntaxKind.FunctionPointerParameterList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.FunctionPointerParameter);
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
                            }
                            N(SyntaxKind.GreaterThanToken);
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
        public void PointerToAFunctionPointer()
        {
            UsingStatement("delegate*<Goo, void>* ptr;", options: TestOptions.Regular9);
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
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Goo");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
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
            UsingStatement("delegate*<ref Goo, in Bar, out Baz, ref readonly void*> ptr;", options: TestOptions.Regular9);
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
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Goo");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Bar");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.OutKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Baz");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
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
        public void Unterminated_01()
        {
            UsingStatement("delegate*< ;", options: TestOptions.Regular9,
                    // (1,12): error CS1031: Type expected
                    // delegate*< ;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 12),
                    // (1,12): error CS1003: Syntax error, '>' expected
                    // delegate*< ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">").WithLocation(1, 12),
                    // (1,12): error CS1001: Identifier expected
                    // delegate*< ;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 12));
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
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_02()
        {
            UsingStatement("delegate*<ref ;", options: TestOptions.Regular9,
                    // (1,15): error CS1031: Type expected
                    // delegate*<ref ;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 15),
                    // (1,15): error CS1003: Syntax error, '>' expected
                    // delegate*<ref ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">").WithLocation(1, 15),
                    // (1,15): error CS1001: Identifier expected
                    // delegate*<ref ;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 15));
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
                                N(SyntaxKind.RefKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_03()
        {
            UsingStatement("delegate*<ref bar ;", options: TestOptions.Regular9,
                    // (1,19): error CS1003: Syntax error, '>' expected
                    // delegate*<ref bar ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">").WithLocation(1, 19),
                    // (1,19): error CS1001: Identifier expected
                    // delegate*<ref bar ;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 19));
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
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "bar");
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_04()
        {
            UsingStatement("delegate*<ref bar, ;", options: TestOptions.Regular9,
                    // (1,20): error CS1031: Type expected
                    // delegate*<ref bar, ;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 20),
                    // (1,20): error CS1003: Syntax error, '>' expected
                    // delegate*<ref bar, ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">").WithLocation(1, 20),
                    // (1,20): error CS1001: Identifier expected
                    // delegate*<ref bar, ;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 20));
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
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "bar");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_05()
        {
            UsingStatement("delegate* unmanaged[ptr];", options: TestOptions.Regular9,
                // (1,25): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[ptr];
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("<").WithLocation(1, 25),
                // (1,25): error CS1001: Identifier expected
                // delegate* unmanaged[ptr];
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 25));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "ptr");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_06()
        {
            UsingStatement("delegate* unmanaged[cdecl] ;", options: TestOptions.Regular9,
                // (1,28): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[cdecl] ;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("<").WithLocation(1, 28),
                // (1,28): error CS1001: Identifier expected
                // delegate* unmanaged[cdecl] ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 28));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_07()
        {
            UsingStatement("delegate* unmanaged[cdecl] ptr;", options: TestOptions.Regular9,
                // (1,28): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[cdecl] ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "ptr").WithArguments("<").WithLocation(1, 28));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
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
        public void Unterminated_08()
        {
            UsingStatement("delegate* ;", options: TestOptions.Regular9,
                    // (1,11): error CS1003: Syntax error, '<' expected
                    // delegate* ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("<").WithLocation(1, 11),
                    // (1,11): error CS1001: Identifier expected
                    // delegate* ;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 11));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_09()
        {
            UsingStatement("delegate* unmanaged.Name[Dotted]<void> ptr;", options: TestOptions.Regular9,
                // (1,9): error CS1514: { expected
                // delegate* unmanaged.Name[Dotted]<void> ptr;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "*").WithLocation(1, 9),
                // (1,9): warning CS8848: Operator '*' cannot be used here due to precedence. Use parentheses to disambiguate.
                // delegate* unmanaged.Name[Dotted]<void> ptr;
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "*").WithArguments("*").WithLocation(1, 9),
                // (1,34): error CS1525: Invalid expression term 'void'
                // delegate* unmanaged.Name[Dotted]<void> ptr;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "void").WithArguments("void").WithLocation(1, 34));
            // This is ambiguous. However, the parser attempts to do better error recovery when the start
            // of a statement is MustBeType followed by a `.`, which while better for the general case, results
            // in this not being parsed as a function pointer.
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.GreaterThanExpression);
                {
                    N(SyntaxKind.LessThanExpression);
                    {
                        N(SyntaxKind.MultiplyExpression);
                        {
                            N(SyntaxKind.AnonymousMethodExpression);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                M(SyntaxKind.Block);
                                {
                                    M(SyntaxKind.OpenBraceToken);
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "unmanaged");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Name");
                                    }
                                }
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Dotted");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ptr");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_10()
        {
            UsingStatement("delegate*( ;", options: TestOptions.Regular9,
                    // (1,10): error CS1003: Syntax error, '<' expected
                    // delegate*( ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("<").WithLocation(1, 10),
                    // (1,12): error CS1031: Type expected
                    // delegate*( ;
                    Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 12),
                    // (1,12): error CS1003: Syntax error, '>' expected
                    // delegate*( ;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">").WithLocation(1, 12),
                    // (1,12): error CS1001: Identifier expected
                    // delegate*( ;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 12));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_11()
        {
            UsingStatement("delegate* @cdecl>", options: TestOptions.Regular9,
                // (1,11): error CS1003: Syntax error, '<' expected
                // delegate* @cdecl>
                Diagnostic(ErrorCode.ERR_SyntaxError, "@cdecl").WithArguments("<").WithLocation(1, 11),
                // (1,17): error CS1003: Syntax error, ',' expected
                // delegate* @cdecl>
                Diagnostic(ErrorCode.ERR_SyntaxError, ">").WithArguments(",").WithLocation(1, 17),
                // (1,18): error CS1002: ; expected
                // delegate* @cdecl>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 18));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "@cdecl");
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_12()
        {
            UsingStatement("delegate* unmanaged[ ;", options: TestOptions.RegularPreview,
                // (1,22): error CS1001: Identifier expected
                // delegate* unmanaged[ ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 22),
                // (1,22): error CS1003: Syntax error, ']' expected
                // delegate* unmanaged[ ;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(1, 22),
                // (1,22): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[ ;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("<").WithLocation(1, 22),
                // (1,22): error CS1001: Identifier expected
                // delegate* unmanaged[ ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 22)
            );
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                M(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_13()
        {
            UsingStatement("delegate* unmanaged[Cdecl ;", options: TestOptions.RegularPreview,
                // (1,27): error CS1003: Syntax error, ']' expected
                // delegate* unmanaged[Cdecl ;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(1, 27),
                // (1,27): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[Cdecl ;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("<").WithLocation(1, 27),
                // (1,27): error CS1001: Identifier expected
                // delegate* unmanaged[Cdecl ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 27)
            );
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Cdecl");
                                }
                                M(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Unterminated_14()
        {
            UsingStatement("delegate* unmanaged[Cdecl,", options: TestOptions.RegularPreview,
                // (1,27): error CS1001: Identifier expected
                // delegate* unmanaged[Cdecl,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 27),
                // (1,27): error CS1003: Syntax error, ']' expected
                // delegate* unmanaged[Cdecl,
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 27),
                // (1,27): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[Cdecl,
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("<").WithLocation(1, 27),
                // (1,27): error CS1001: Identifier expected
                // delegate* unmanaged[Cdecl,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 27),
                // (1,27): error CS1002: ; expected
                // delegate* unmanaged[Cdecl,
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 27)
            );
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "Cdecl");
                                }
                                N(SyntaxKind.CommaToken);
                                M(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
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
        public void NoParamOrReturnTypes()
        {
            UsingStatement("delegate*<> ptr;", options: TestOptions.Regular9,
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
                        N(SyntaxKind.FunctionPointerParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
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
        public void UsingParensInsteadOfAngles()
        {
            UsingStatement("delegate*(int, void)", options: TestOptions.Regular9,
                    // (1,10): error CS1003: Syntax error, '<' expected
                    // delegate*(int, void)
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("<").WithLocation(1, 10),
                    // (1,20): error CS1003: Syntax error, '>' expected
                    // delegate*(int, void)
                    Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(">").WithLocation(1, 20),
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
                        N(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
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
    public delegate*<int, string> M(delegate*<C, void> param1, delegate* unmanaged[cdecl]<D> param2) {}
}",
                options: TestOptions.Regular9);
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
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.StringKeyword);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
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
                                    N(SyntaxKind.FunctionPointerParameterList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.FunctionPointerParameter);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "C");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
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
                                N(SyntaxKind.IdentifierToken, "param1");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.FunctionPointerCallingConvention);
                                    {
                                        N(SyntaxKind.UnmanagedKeyword);
                                        N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                            {
                                                N(SyntaxKind.IdentifierToken, "cdecl");
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.FunctionPointerParameterList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.FunctionPointerParameter);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "D");
                                            }
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
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
            UsingExpression("(delegate* unmanaged[thiscall]<int, C>)ptr", options: TestOptions.Regular9);
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.FunctionPointerCallingConvention);
                    {
                        N(SyntaxKind.UnmanagedKeyword);
                        N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                            {
                                N(SyntaxKind.IdentifierToken, "thiscall");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.FunctionPointerParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
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
            UsingExpression("ptr as delegate* unmanaged[stdcall]<int, void>", options: TestOptions.Regular9);
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
                    N(SyntaxKind.FunctionPointerCallingConvention);
                    {
                        N(SyntaxKind.UnmanagedKeyword);
                        N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                            {
                                N(SyntaxKind.IdentifierToken, "stdcall");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.FunctionPointerParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
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
            }
            EOF();
        }

        [Fact]
        public void TupleType()
        {
            UsingExpression("((delegate*<int, void> i1, delegate* managed<C, D> i2))ptr", options: TestOptions.Regular9);
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
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
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
                        N(SyntaxKind.IdentifierToken, "i1");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.FunctionPointerCallingConvention);
                            {
                                N(SyntaxKind.ManagedKeyword);
                            }
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "D");
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
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
            UsingExpression("new M<delegate* unmanaged[thiscall]<void>, delegate*<C, D>>()", options: TestOptions.Regular9);
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
                            N(SyntaxKind.FunctionPointerCallingConvention);
                            {
                                N(SyntaxKind.UnmanagedKeyword);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                    {
                                        N(SyntaxKind.IdentifierToken, "thiscall");
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
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
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerType);
                        {
                            N(SyntaxKind.DelegateKeyword);
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "D");
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
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
            UsingExpression("typeof(delegate* unmanaged[cdecl]<ref int, readonly ref D>)", options: TestOptions.Regular9);
            N(SyntaxKind.TypeOfExpression);
            {
                N(SyntaxKind.TypeOfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.FunctionPointerCallingConvention);
                    {
                        N(SyntaxKind.UnmanagedKeyword);
                        N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                            {
                                N(SyntaxKind.IdentifierToken, "cdecl");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.FunctionPointerParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.FunctionPointerParameter);
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
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void ArrayType()
        {
            UsingStatement("delegate*<ref C>[] ptr;", options: TestOptions.Regular9);
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
                            N(SyntaxKind.FunctionPointerParameterList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
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
    delegate delegate* unmanaged[cdecl]<void> M(delegate*<ref C, D> p);
}",
                options: TestOptions.Regular9);
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
                            N(SyntaxKind.FunctionPointerCallingConvention);
                            {
                                N(SyntaxKind.UnmanagedKeyword);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                    {
                                        N(SyntaxKind.IdentifierToken, "cdecl");
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
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
                                    N(SyntaxKind.FunctionPointerParameterList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.FunctionPointerParameter);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "C");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.FunctionPointerParameter);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "D");
                                            }
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
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
            UsingExpression("(delegate*<void> p1) => {}", options: TestOptions.Regular9);
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
}", options: TestOptions.Regular9,
                // (2,1): error CS0106: The modifier 'public' is not valid for this item
                // public void M()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(2, 1)
            );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
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
                                        N(SyntaxKind.IdentifierToken, "l3");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void IsExpression()
        {
            UsingExpression("o is delegate*<void>", options: TestOptions.Regular9);
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
            }
            EOF();
        }

        [Fact]
        public void IsNamedExpression()
        {
            UsingExpression("o is delegate*<void> ptr", options: TestOptions.Regular9);
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
}", options: TestOptions.Regular9);

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
}", options: TestOptions.Regular9);

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
            UsingStatement("using (delegate*<void> ptr = MyMethod()) {}", options: TestOptions.Regular9);
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
            UsingStatement("using delegate*<void> ptr = MyMethod();", options: TestOptions.Regular9);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
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
            UsingStatement("fixed (delegate*<void> ptr = &MyMethod) {}", options: TestOptions.Regular9);
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
            UsingStatement("foreach (delegate*<void> ptr in ptrs) {}", options: TestOptions.Regular9);
            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
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
            UsingStatement("for (delegate*<void> ptr = null;;) {}", options: TestOptions.Regular9);
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
            UsingExpression("sizeof(delegate*<void>)", options: TestOptions.Regular9);
            N(SyntaxKind.SizeOfExpression);
            {
                N(SyntaxKind.SizeOfKeyword);
                N(SyntaxKind.OpenParenToken);
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
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void SpecifiedParameterNamesAndDefaults()
        {
            UsingStatement("delegate*<int param1, string param2 = default, void> ptr;", options: TestOptions.Regular9,
                    // (1,15): error CS1003: Syntax error, ',' expected
                    // delegate*<int param1, string param2 = default, void> ptr;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "param1").WithArguments(",").WithLocation(1, 15),
                    // (1,30): error CS1003: Syntax error, ',' expected
                    // delegate*<int param1, string param2 = default, void> ptr;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "param2").WithArguments(",").WithLocation(1, 30));
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
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void MissingListStart_01()
        {
            UsingStatement("delegate*void> ptr;", options: TestOptions.Regular9,
                    // (1,10): error CS1003: Syntax error, '<' expected
                    // delegate*void> ptr;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "void").WithArguments("<").WithLocation(1, 10),
                    // (1,10): error CS1001: Identifier expected
                    // delegate*void> ptr;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "void").WithLocation(1, 10),
                    // (1,10): error CS1003: Syntax error, ',' expected
                    // delegate*void> ptr;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "void").WithArguments(",").WithLocation(1, 10));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void MissingListStart_02()
        {
            UsingStatement("delegate* unmanaged[cdecl] void> ptr;", options: TestOptions.Regular9,
                // (1,28): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[cdecl] void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "void").WithArguments("<").WithLocation(1, 28),
                // (1,28): error CS1001: Identifier expected
                // delegate* unmanaged[cdecl] void> ptr;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "void").WithLocation(1, 28),
                // (1,28): error CS1003: Syntax error, ',' expected
                // delegate* unmanaged[cdecl] void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "void").WithArguments(",").WithLocation(1, 28));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void MissingListStart_03()
        {
            UsingStatement("delegate*> ptr;", options: TestOptions.Regular9,
                    // (1,10): error CS1003: Syntax error, '<' expected
                    // delegate*> ptr;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ">").WithArguments("<").WithLocation(1, 10));
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
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
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
        public void MissingListStart_04()
        {
            UsingStatement("delegate* unmanaged Cdecl]<void> ptr;", options: TestOptions.RegularPreview,
                // (1,21): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged Cdecl]<void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "Cdecl").WithArguments("<").WithLocation(1, 21),
                // (1,26): error CS1003: Syntax error, ',' expected
                // delegate* unmanaged Cdecl]<void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "]").WithArguments(",").WithLocation(1, 26)
            );
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "Cdecl");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ManyInvalidModifiers()
        {
            UsingStatement("delegate*<this params readonly ref ref this int> ptr;", options: TestOptions.Regular9);
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
                                N(SyntaxKind.ThisKeyword);
                                N(SyntaxKind.ParamsKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ThisKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
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
        public void AsyncInParameterList_InAsyncFunction()
        {
            UsingNode(@"
async void M()
{
    delegate*<async, async> ptr;
}", options: TestOptions.Regular9);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                                        N(SyntaxKind.FunctionPointerParameterList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ptr");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncInParameterList_InNonAsyncFunction()
        {
            UsingNode(@"
void M()
{
    delegate*<async, async> ptr;
}", options: TestOptions.Regular9);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
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
                                        N(SyntaxKind.FunctionPointerParameterList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "async");
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ptr");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AwaitInParameterList_InAsyncFunction()
        {
            UsingNode(@"
async void M()
{
    delegate*<await, await> ptr;
}", options: TestOptions.Regular9,
                    // (4,15): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                    //     delegate*<await, await> ptr;
                    Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(4, 15),
                    // (4,22): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                    //     delegate*<await, await> ptr;
                    Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(4, 22));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                                        N(SyntaxKind.FunctionPointerParameterList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "await");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "await");
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ptr");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AwaitInParameterList_InNonAsyncFunction()
        {
            UsingNode(@"
void M()
{
    delegate*<await, await> ptr;
}", options: TestOptions.Regular9);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
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
                                        N(SyntaxKind.FunctionPointerParameterList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "await");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.FunctionPointerParameter);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "await");
                                                }
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ptr");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void IncompleteAtEndOfFile()
        {
            UsingStatement("delegate*", options: TestOptions.Regular9,
                    // (1,10): error CS1003: Syntax error, '<' expected
                    // delegate*
                    Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("<").WithLocation(1, 10),
                    // (1,10): error CS1001: Identifier expected
                    // delegate*
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 10),
                    // (1,10): error CS1002: ; expected
                    // delegate*
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 10));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
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
        public void IncompleteAtEndOfFileWithCallingConvention()
        {
            UsingStatement("delegate* unmanaged[cdecl]", options: TestOptions.Regular9,
                // (1,27): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[cdecl]
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("<").WithLocation(1, 27),
                // (1,27): error CS1001: Identifier expected
                // delegate* unmanaged[cdecl]
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 27),
                // (1,27): error CS1002: ; expected
                // delegate* unmanaged[cdecl]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 27));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            M(SyntaxKind.FunctionPointerParameter);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
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
        public void MixedParensAndAngles_01()
        {
            UsingStatement("delegate* unmanaged[cdecl]<void) ptr;", options: TestOptions.Regular9,
                // (1,32): error CS1003: Syntax error, ',' expected
                // delegate* unmanaged[cdecl]<void) ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(1, 32),
                // (1,37): error CS1003: Syntax error, '>' expected
                // delegate* unmanaged[cdecl]<void) ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">").WithLocation(1, 37),
                // (1,37): error CS1001: Identifier expected
                // delegate* unmanaged[cdecl]<void) ptr;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 37));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
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
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void MixedParensAndAngles_02()
        {
            UsingStatement("delegate* unmanaged[cdecl](void> ptr;", options: TestOptions.Regular9,
                // (1,27): error CS1003: Syntax error, '<' expected
                // delegate* unmanaged[cdecl](void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("<").WithLocation(1, 27),
                // (1,32): error CS1003: Syntax error, ',' expected
                // delegate* unmanaged[cdecl](void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, ">").WithArguments(",").WithLocation(1, 32),
                // (1,37): error CS1003: Syntax error, '>' expected
                // delegate* unmanaged[cdecl](void> ptr;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(">").WithLocation(1, 37),
                // (1,37): error CS1001: Identifier expected
                // delegate* unmanaged[cdecl](void> ptr;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 37));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerCallingConvention);
                        {
                            N(SyntaxKind.UnmanagedKeyword);
                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                {
                                    N(SyntaxKind.IdentifierToken, "cdecl");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.FunctionPointerParameterList);
                        {
                            M(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void CallingConventionNextLine()
        {
            UsingNode($@"
void C()
{{
    delegate*
    unmanaged[Cdecl]
}}", options: TestOptions.Regular9,
                // (5,21): error CS1003: Syntax error, '<' expected
                //     unmanaged[Cdecl]
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("<").WithLocation(5, 21),
                // (5,21): error CS1001: Identifier expected
                //     unmanaged[Cdecl]
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(5, 21),
                // (5,21): error CS1002: ; expected
                //     unmanaged[Cdecl]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 21));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "C");
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
                                        N(SyntaxKind.FunctionPointerCallingConvention);
                                        {
                                            N(SyntaxKind.UnmanagedKeyword);
                                            N(SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.FunctionPointerUnmanagedCallingConvention);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Cdecl");
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                        M(SyntaxKind.FunctionPointerParameterList);
                                        {
                                            M(SyntaxKind.LessThanToken);
                                            M(SyntaxKind.FunctionPointerParameter);
                                            {
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            M(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void InvalidCallingConventionNextLine()
        {
            UsingNode(@"
void C()
{
    delegate*
    int[] ptr = 1;
}", options: TestOptions.Regular9,
                    // (4,14): error CS1003: Syntax error, '<' expected
                    //     delegate*
                    Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("<").WithLocation(4, 14),
                    // (4,14): error CS1001: Identifier expected
                    //     delegate*
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 14),
                    // (4,14): error CS1002: ; expected
                    //     delegate*
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 14));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "C");
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
                                        M(SyntaxKind.FunctionPointerParameterList);
                                        {
                                            M(SyntaxKind.LessThanToken);
                                            M(SyntaxKind.FunctionPointerParameter);
                                            {
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            M(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
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
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ptr");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "1");
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void SyntaxFacts()
        {
            Assert.True(CSharp.SyntaxFacts.IsTypeSyntax(SyntaxKind.FunctionPointerType));
            Assert.True(CSharp.SyntaxFacts.IsContextualKeyword(SyntaxKind.ManagedKeyword));
            Assert.True(CSharp.SyntaxFacts.IsContextualKeyword(SyntaxKind.UnmanagedKeyword));
        }

        [Fact]
        public void FunctionPointerArrayInTypeArgument()
        {
            UsingStatement("I<delegate*<void>[]> i;", options: TestOptions.Regular9);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "I");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.ArrayType);
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
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "i");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void UsingAlias()
        {
            UsingNode("using t = delegate*<void>;", options: TestOptions.Regular9,
                // (1,11): error CS1041: Identifier expected; 'delegate' is a keyword
                // using t = delegate*<void>;
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "delegate").WithArguments("", "delegate").WithLocation(1, 11),
                // (1,25): error CS0116: A namespace cannot directly contain members such as fields or methods
                // using t = delegate*<void>;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, ">").WithLocation(1, 25)
            );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "t");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.IncompleteMember);
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
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}

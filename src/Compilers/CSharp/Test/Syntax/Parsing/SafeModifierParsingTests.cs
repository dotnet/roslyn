// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class SafeModifierParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    [Fact]
    public void SafeModifier_Method()
    {
        UsingDeclaration("safe public extern void M();");

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.ExternKeyword);
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
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void SafeModifier_Property()
    {
        UsingDeclaration("safe public extern int P { get; }");

        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "P");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void SafeModifier_Event()
    {
        UsingDeclaration("safe public static extern event System.Action E;");

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
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
                        N(SyntaxKind.IdentifierToken, "Action");
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void SafeModifier_Constructor()
    {
        UsingDeclaration("safe public extern C();");

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory]
    [InlineData("safe public int F;", SyntaxKind.FieldDeclaration)]
    [InlineData("safe public extern int this[int i] { get; }", SyntaxKind.IndexerDeclaration)]
    [InlineData("safe public extern event EHandler E { add; remove; }", SyntaxKind.EventDeclaration)]
    [InlineData("safe public static C operator +(C x, C y);", SyntaxKind.OperatorDeclaration)]
    [InlineData("safe public static explicit operator int(C c);", SyntaxKind.ConversionOperatorDeclaration)]
    [InlineData("safe public class C { }", SyntaxKind.ClassDeclaration)]
    [InlineData("safe public struct S { }", SyntaxKind.StructDeclaration)]
    [InlineData("safe public record R;", SyntaxKind.RecordDeclaration)]
    [InlineData("safe public record struct RS;", SyntaxKind.RecordStructDeclaration)]
    [InlineData("safe public interface I { }", SyntaxKind.InterfaceDeclaration)]
    [InlineData("safe public enum E { }", SyntaxKind.EnumDeclaration)]
    [InlineData("safe public delegate void D();", SyntaxKind.DelegateDeclaration)]
    public void SafeModifier_OtherMemberKinds(string source, SyntaxKind declarationKind)
    {
        var declaration = SyntaxFactory.ParseMemberDeclaration(source);
        Assert.NotNull(declaration);
        Assert.Empty(declaration.GetDiagnostics());
        Assert.Equal(declarationKind, declaration.Kind());

        var modifiers = declaration switch
        {
            BaseFieldDeclarationSyntax field => field.Modifiers,
            BaseMethodDeclarationSyntax method => method.Modifiers,
            BasePropertyDeclarationSyntax property => property.Modifiers,
            BaseTypeDeclarationSyntax type => type.Modifiers,
            DelegateDeclarationSyntax @delegate => @delegate.Modifiers,
            _ => throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
        };

        Assert.Equal(SyntaxKind.SafeKeyword, modifiers[0].Kind());
    }

    [Fact]
    public void SafeIdentifier_Destructor()
    {
        UsingDeclaration("safe ~C() { }", options: null,
            // (1,1): error CS1073: Unexpected token '~'
            // safe ~C() { }
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "safe").WithArguments("~").WithLocation(1, 1),
            // (1,6): error CS1519: Invalid token '~' in a member declaration
            // safe ~C() { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "~").WithArguments("~").WithLocation(1, 6));

        N(SyntaxKind.IncompleteMember);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "safe");
            }
        }
        EOF();
    }

    [Fact]
    public void SafeIdentifier_PropertyAccessor()
    {
        UsingDeclaration("public int P { safe get; set; }", options: null,
            // (1,16): error CS1014: A get or set accessor expected
            // public int P { safe get; set; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "safe").WithLocation(1, 16));

        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "P");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "safe");
                }
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.SetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void SafeIdentifier_EventAccessor()
    {
        UsingDeclaration("public event EHandler E { safe add { } remove { } }", options: null,
            // (1,27): error CS1055: An add or remove accessor expected
            // public event EHandler E { safe add { } remove { } }
            Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "safe").WithLocation(1, 27));

        N(SyntaxKind.EventDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "EHandler");
            }
            N(SyntaxKind.IdentifierToken, "E");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "safe");
                }
                N(SyntaxKind.AddAccessorDeclaration);
                {
                    N(SyntaxKind.AddKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.RemoveAccessorDeclaration);
                {
                    N(SyntaxKind.RemoveKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void SafeIdentifier_ParameterType()
    {
        UsingDeclaration("public void M(safe x);");

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
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "safe");
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory]
    [InlineData("public safe extern void M();", SyntaxKind.PublicKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public extern safe void M();", SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword)]
    [InlineData("extern safe public void M();", SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword)]
    public void SafeModifier_Method_Ordering(string source, params SyntaxKind[] expectedModifiers)
    {
        UsingDeclaration(source);

        N(SyntaxKind.MethodDeclaration);
        {
            foreach (var expectedModifier in expectedModifiers)
            {
                N(expectedModifier);
            }

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
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory]
    [InlineData("public safe extern int P { get; }", SyntaxKind.PublicKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public extern safe int P { get; }", SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword)]
    public void SafeModifier_Property_Ordering(string source, params SyntaxKind[] expectedModifiers)
    {
        UsingDeclaration(source);

        N(SyntaxKind.PropertyDeclaration);
        {
            foreach (var expectedModifier in expectedModifiers)
            {
                N(expectedModifier);
            }

            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "P");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory]
    [InlineData("public static safe extern event EHandler E;", SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public extern safe static event EHandler E;", SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword, SyntaxKind.StaticKeyword)]
    public void SafeModifier_Event_Ordering(string source, params SyntaxKind[] expectedModifiers)
    {
        UsingDeclaration(source);

        N(SyntaxKind.EventFieldDeclaration);
        {
            foreach (var expectedModifier in expectedModifiers)
            {
                N(expectedModifier);
            }

            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "EHandler");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory]
    [InlineData("public safe extern C();", SyntaxKind.PublicKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("extern safe public C();", SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword)]
    public void SafeModifier_Constructor_Ordering(string source, params SyntaxKind[] expectedModifiers)
    {
        UsingDeclaration(source);

        N(SyntaxKind.ConstructorDeclaration);
        {
            foreach (var expectedModifier in expectedModifiers)
            {
                N(expectedModifier);
            }

            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void SafeModifier_Constructor_Ordering_AfterExternAmbiguousWithReturnType()
    {
        UsingDeclaration("public extern safe C();");

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "safe");
            }
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void SafeIdentifier_InvocationExpression()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    safe();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
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
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "safe");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
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
    }
}

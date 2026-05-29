// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class SafeModifierParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    [Theory]
    [InlineData("safe public extern void M();", SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public safe extern void M();", SyntaxKind.PublicKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public extern safe void M();", SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword)]
    [InlineData("extern safe public void M();", SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword)]
    public void Method(string source, params SyntaxKind[] expectedModifiers)
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

    [Fact]
    public void Method_Async_Before()
    {
        UsingDeclaration("public safe async(int i);", TestOptions.Regular14);

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "safe");
            }
            N(SyntaxKind.IdentifierToken, "async");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void Method_Async_After(
        [CombinatorialValues(LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
    {
        UsingDeclaration("public safe async(int i);", TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.IdentifierToken, "async");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory]
    [InlineData("safe public extern int P { get; }", SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public safe extern int P { get; }", SyntaxKind.PublicKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public extern safe int P { get; }", SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword)]
    public void Property(string source, params SyntaxKind[] expectedModifiers)
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
    [InlineData("safe public static extern event System.Action E;", SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public static safe extern event System.Action E;", SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public extern safe static event System.Action E;", SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword, SyntaxKind.StaticKeyword)]
    public void Event(string source, params SyntaxKind[] expectedModifiers)
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

    [Theory]
    [InlineData("safe public C();", SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword)]
    [InlineData("safe public extern C();", SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("public safe extern C();", SyntaxKind.PublicKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("extern safe public C();", SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword, SyntaxKind.PublicKeyword)]
    [InlineData("public safe extern partial C();", SyntaxKind.PublicKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword, SyntaxKind.PartialKeyword)]
    public void Constructor(string source, params SyntaxKind[] expectedModifiers)
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

    [Theory]
    [InlineData("safe static extern void Local();", SyntaxKind.SafeKeyword, SyntaxKind.StaticKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("static safe extern void Local();", SyntaxKind.StaticKeyword, SyntaxKind.SafeKeyword, SyntaxKind.ExternKeyword)]
    [InlineData("static extern safe void Local();", SyntaxKind.StaticKeyword, SyntaxKind.ExternKeyword, SyntaxKind.SafeKeyword)]
    public void LocalFunction(string localFunction, params SyntaxKind[] expectedModifiers)
    {
        UsingTree($$"""
            class C
            {
                void M()
                {
                    {{localFunction}}
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            foreach (var expectedModifier in expectedModifiers)
                            {
                                N(expectedModifier);
                            }

                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "Local");
                            N(SyntaxKind.ParameterList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
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
    public void OtherMemberKinds(string source, SyntaxKind declarationKind)
    {
        var declaration = SyntaxFactory.ParseMemberDeclaration(source);
        Assert.NotNull(declaration);
        declaration.GetDiagnostics().Verify();
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
    public void Destructor_Before()
    {
        UsingDeclaration("safe ~C() { }", TestOptions.Regular14,
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

    [Theory, CombinatorialData]
    public void Destructor_After(
        [CombinatorialValues(LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
    {
        UsingDeclaration("safe ~C() { }", TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.DestructorDeclaration);
        {
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.TildeToken);
            N(SyntaxKind.IdentifierToken, "C");
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
        EOF();
    }

    [Fact]
    public void Destructor_ExternSafe_Before()
    {
        UsingDeclaration("extern safe ~C();", TestOptions.Regular14,
            // (1,1): error CS1073: Unexpected token '~'
            // extern safe ~C();
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "extern safe").WithArguments("~").WithLocation(1, 1),
            // (1,13): error CS1519: Invalid token '~' in a member declaration
            // extern safe ~C();
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "~").WithArguments("~").WithLocation(1, 13));

        N(SyntaxKind.IncompleteMember);
        {
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "safe");
            }
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void Destructor_ExternSafe_After(
        [CombinatorialValues(LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
    {
        UsingDeclaration("extern safe ~C();", TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.DestructorDeclaration);
        {
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.TildeToken);
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
    public void Destructor_SafeExtern()
    {
        UsingDeclaration("safe extern ~C();");

        N(SyntaxKind.DestructorDeclaration);
        {
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.TildeToken);
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
    public void PropertyAccessor()
    {
        UsingDeclaration("public int P { safe get; set; }");

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
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.SafeKeyword);
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

    [Theory]
    [InlineData("public int P { private safe get; set; }", 0, SyntaxKind.PrivateKeyword, SyntaxKind.SafeKeyword)]
    [InlineData("public int P { safe private get; set; }", 0, SyntaxKind.SafeKeyword, SyntaxKind.PrivateKeyword)]
    [InlineData("public int P { get; private safe set; }", 1, SyntaxKind.PrivateKeyword, SyntaxKind.SafeKeyword)]
    public void PropertyAccessor_WithOtherModifiers(string source, int accessorIndex, params SyntaxKind[] expectedModifiers)
    {
        var declaration = Assert.IsType<PropertyDeclarationSyntax>(SyntaxFactory.ParseMemberDeclaration(source));
        declaration.GetDiagnostics().Verify();

        var modifiers = declaration.AccessorList!.Accessors[accessorIndex].Modifiers;
        Assert.Equal(expectedModifiers.Length, modifiers.Count);

        for (var i = 0; i < expectedModifiers.Length; i++)
        {
            Assert.Equal(expectedModifiers[i], modifiers[i].Kind());
        }
    }

    [Fact]
    public void EventAccessor()
    {
        UsingDeclaration("public event EHandler E { safe add { } remove { } }");

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
                N(SyntaxKind.AddAccessorDeclaration);
                {
                    N(SyntaxKind.SafeKeyword);
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
    public void ParameterType()
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

    [Fact]
    public void Constructor_BeforeName_Before()
    {
        UsingDeclaration("public safe C();", TestOptions.Regular14);

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
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

    [Theory, CombinatorialData]
    public void Constructor_BeforeName_After(
        [CombinatorialValues(LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
    {
        UsingDeclaration("public safe C();", TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.SafeKeyword);
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
    public void Constructor_AfterExtern_Before()
    {
        UsingDeclaration("public extern safe C();", TestOptions.Regular14);

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

    [Theory, CombinatorialData]
    public void Constructor_AfterExtern_After(
        [CombinatorialValues(LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
    {
        UsingDeclaration("public extern safe C();", TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.ExternKeyword);
            N(SyntaxKind.SafeKeyword);
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
    public void Constructor_BeforePartial_Before()
    {
        UsingDeclaration("public safe partial C();", TestOptions.Regular14,
            // (1,13): error CS1525: Invalid expression term 'partial'
            // public safe partial C();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 13),
            // (1,13): error CS1003: Syntax error, ',' expected
            // public safe partial C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 13));

        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "safe");
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

    [Theory, CombinatorialData]
    public void Constructor_BeforePartial_After(
        [CombinatorialValues(LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
    {
        UsingDeclaration("public safe partial C();", TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.SafeKeyword);
            N(SyntaxKind.PartialKeyword);
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
    public void InvocationExpression()
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

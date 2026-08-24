// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class AccessorDeclarationParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    [Fact]
    public void ContextualAndKeywordAccessorModifierOrderings()
    {
        const string source = """
            class C
            {
                int P { partial ref get; }
                int Q { async partial get; }
                int R { ref scoped get; }
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
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
                            N(SyntaxKind.PartialKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.AsyncKeyword);
                            N(SyntaxKind.PartialKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ScopedKeyword);
                            N(SyntaxKind.GetKeyword);
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

        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     int P { partial ref get; }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 13),
            // (3,25): error CS0106: The modifier 'ref' is not valid for this item
            //     int P { partial ref get; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 25),
            // (4,19): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     int Q { async partial get; }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 19),
            // (4,27): error CS0106: The modifier 'async' is not valid for this item
            //     int Q { async partial get; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("async").WithLocation(4, 27),
            // (5,24): error CS0106: The modifier 'ref' is not valid for this item
            //     int R { ref scoped get; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(5, 24),
            // (5,24): error CS0106: The modifier 'scoped' is not valid for this item
            //     int R { ref scoped get; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("scoped").WithLocation(5, 24));
    }

    [Fact]
    public void RefAccessorModifierBodyForms()
    {
        const string source = "int P { ref get { } ref get; ref get => 0; }";

        UsingDeclaration(source);
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ContextualKeywordWithoutAccessorNameBeforeBodyForms()
    {
        const string source = "int P { partial { } partial; }";

        UsingDeclaration(
            source,
            options: null,
            // (1,17): error CS1014: A get or set accessor expected
            // int P { partial { } partial; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "{").WithLocation(1, 17),
            // (1,28): error CS1014: A get or set accessor expected
            // int P { partial { } partial; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, ";").WithLocation(1, 28));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void EscapedContextualAndArbitraryAccessorNames()
    {
        const string source = "int P { @partial; @async; @scoped; unknown; }";

        UsingDeclaration(
            source,
            options: null,
            // (1,9): error CS1014: A get or set accessor expected
            // int P { @partial; @async; @scoped; unknown; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "@partial").WithLocation(1, 9),
            // (1,19): error CS1014: A get or set accessor expected
            // int P { @partial; @async; @scoped; unknown; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "@async").WithLocation(1, 19),
            // (1,27): error CS1014: A get or set accessor expected
            // int P { @partial; @async; @scoped; unknown; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "@scoped").WithLocation(1, 27),
            // (1,36): error CS1014: A get or set accessor expected
            // int P { @partial; @async; @scoped; unknown; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "unknown").WithLocation(1, 36));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.IdentifierToken, "@partial");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "@async");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "@scoped");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "unknown");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ContextualKeywordWithoutAccessorNameBeforeExpressionBody()
    {
        const string source = "int P { partial => 0; }";

        UsingDeclaration(
            source,
            options: null,
            // (1,17): error CS1014: A get or set accessor expected
            // int P { partial => 0; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "=>").WithLocation(1, 17));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ContextualKeywordBeforeNonAccessorTokens()
    {
        const string source = "int P { partial unknown; }";

        UsingDeclaration(
            source,
            options: null,
            // (1,17): error CS1014: A get or set accessor expected
            // int P { partial unknown; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "unknown").WithLocation(1, 17));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "unknown");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void AccessorModifierBeforeAttributeRecovery()
    {
        const string source = "class C { int P { private [System.Obsolete] get; } }";

        UsingTree(
            source,
            // (1,19): error CS1513: } expected
            // class C { int P { private [System.Obsolete] get; } }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "private").WithLocation(1, 19),
            // (1,27): error CS1031: Type expected
            // class C { int P { private [System.Obsolete] get; } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(1, 27),
            // (1,52): error CS1022: Type or namespace definition, or end-of-file expected
            // class C { int P { private [System.Obsolete] get; } }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 52));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "System");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Obsolete");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
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
    public void ValidPropertyAndIndexerAccessors()
    {
        const string source = """
            class C
            {
                public int P { get; private set; }
                public int Q { get; private init; }
                public int this[int i] { private get => 0; set { } }
            }
            """;

        UsingDeclaration("public int P { get; private set; }");
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
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.SetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();

        UsingDeclaration("public int Q { get; private init; }");
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "Q");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.InitAccessorDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.InitKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();

        UsingDeclaration("public int this[int i] { private get => 0; set { } }");
        N(SyntaxKind.IndexerDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.ThisKeyword);
            N(SyntaxKind.BracketedParameterList);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.SetKeyword);
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

        CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
    }

    [Fact]
    public void RefModifiersRemainOnPropertyAndEventAccessors()
    {
        const string source = """
            class C
            {
                int P { ref get => 0; abstract ref set { } }
                event System.Action E { ref add { } abstract ref remove { } }
            }
            """;

        UsingDeclaration("int P { ref get => 0; abstract ref set { } }");
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.AbstractKeyword);
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.SetKeyword);
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

        UsingDeclaration("event System.Action E { ref add { } abstract ref remove { } }");
        N(SyntaxKind.EventDeclaration);
        {
            N(SyntaxKind.EventKeyword);
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
            N(SyntaxKind.IdentifierToken, "E");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AddAccessorDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.AddKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.RemoveAccessorDeclaration);
                {
                    N(SyntaxKind.AbstractKeyword);
                    N(SyntaxKind.RefKeyword);
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

        CreateCompilation(source).VerifyDiagnostics(
            // (3,17): error CS0106: The modifier 'ref' is not valid for this item
            //     int P { ref get => 0; abstract ref set { } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 17),
            // (3,40): error CS0106: The modifier 'abstract' is not valid for this item
            //     int P { ref get => 0; abstract ref set { } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("abstract").WithLocation(3, 40),
            // (3,40): error CS0106: The modifier 'ref' is not valid for this item
            //     int P { ref get => 0; abstract ref set { } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("ref").WithLocation(3, 40),
            // (4,29): error CS1609: Modifiers cannot be placed on event accessor declarations
            //     event System.Action E { ref add { } abstract ref remove { } }
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "ref").WithLocation(4, 29),
            // (4,41): error CS1609: Modifiers cannot be placed on event accessor declarations
            //     event System.Action E { ref add { } abstract ref remove { } }
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "abstract").WithLocation(4, 41));
    }

    [Fact]
    public void RefReturningMembersWithAccessorKeywordType()
    {
        const string source = """
            #pragma warning disable CS8981
            class get { }
            class C
            {
                get _value;
                ref get A => ref _value;
                ref get B { }
                ref get C;
                ref get D() { }
                ref get E() => ref _value;
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "get");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_value");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_value");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "get");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "D");
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
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_value");
                            }
                        }
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
    public void IncompleteRefAccessorBeforeCloseBrace()
    {
        const string source = """
            class C
            {
                int P { ref get }
            }
            """;

        UsingDeclaration(
            source,
            options: null,
            // (3,21): error CS8180: { or ; or => expected
            //     int P { ref get }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "}").WithLocation(3, 21));
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.GetKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            // (3,17): error CS0106: The modifier 'ref' is not valid for this item
            //     int P { ref get }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 17),
            // (3,21): error CS8180: { or ; or => expected
            //     int P { ref get }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "}").WithLocation(3, 21));
    }

    [Fact]
    public void ScopedRefModifiersRemainOnAccessor()
    {
        const string declaration = "public int P { scoped ref get; set; }";
        var source = $$"""
            class C
            {
                {{declaration}}
            }
            """;

        UsingDeclaration(declaration);
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
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefKeyword);
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

        CreateCompilation(source).VerifyDiagnostics(
            // (3,31): error CS0106: The modifier 'ref' is not valid for this item
            //     public int P { scoped ref get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 31),
            // (3,31): error CS0106: The modifier 'scoped' is not valid for this item
            //     public int P { scoped ref get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("scoped").WithLocation(3, 31));
    }

    [Fact]
    public void SafeModifierOrderingAndAccessorName()
    {
        const string source = """
            class C
            {
                public int P { private safe get => 0; set { } }
                public int Q { get => 0; safe private set { } }
                int R { safe; get => 0; set { } }
            }
            """;

        UsingDeclaration(
            source,
            TestOptions.RegularPreview,
            // (5,17): error CS1014: A get or set accessor expected
            //     int R { safe; get => 0; set { } }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, ";").WithLocation(5, 17));
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.PrivateKeyword);
                        N(SyntaxKind.SafeKeyword);
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.SetAccessorDeclaration);
                    {
                        N(SyntaxKind.SetKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "Q");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.SetAccessorDeclaration);
                    {
                        N(SyntaxKind.SafeKeyword);
                        N(SyntaxKind.PrivateKeyword);
                        N(SyntaxKind.SetKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UnknownAccessorDeclaration);
                    {
                        N(SyntaxKind.SafeKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.SetAccessorDeclaration);
                    {
                        N(SyntaxKind.SetKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,17): error CS1014: A get or set accessor expected
            //     int R { safe; get => 0; set { } }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, ";").WithLocation(5, 17));
    }

    [Fact]
    public void ContextualKeywordsAreRecoveredBeforeAccessors()
    {
        const string source = """
            class C
            {
                int P { scoped get; set; }
                int Q { partial get; set; }
                int R { async get; set; }
                public int S { private async get; set; }
                public int T { private partial get; set; }
            }
            """;

        UsingDeclaration(source, TestOptions.RegularPreview);
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
                        N(SyntaxKind.ScopedKeyword);
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
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "Q");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.PartialKeyword);
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
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.PrivateKeyword);
                        N(SyntaxKind.AsyncKeyword);
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
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "T");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.PrivateKeyword);
                        N(SyntaxKind.PartialKeyword);
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
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (3,20): error CS0106: The modifier 'scoped' is not valid for this item
            //     int P { scoped get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("scoped").WithLocation(3, 20),
            // (4,13): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     int Q { partial get; set; }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 13),
            // (5,19): error CS0106: The modifier 'async' is not valid for this item
            //     int R { async get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("async").WithLocation(5, 19),
            // (6,34): error CS0106: The modifier 'async' is not valid for this item
            //     public int S { private async get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("async").WithLocation(6, 34),
            // (7,28): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     public int T { private partial get; set; }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(7, 28));
    }

    [Fact]
    public void InvalidModifiersRemainOnAccessors()
    {
        const string source = """
            class C
            {
                int P { required get; file set; }
                int Q { closed get; static set; }
                public int R { private private get; set; }
            }
            """;

        UsingDeclaration("int P { required get; file set; }", TestOptions.RegularPreview);
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.RequiredKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.SetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();

        UsingDeclaration("int Q { closed get; static set; }", TestOptions.RegularPreview);
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "Q");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.SetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();

        UsingDeclaration("public int R { private private get; set; }", TestOptions.RegularPreview);
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "R");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.PrivateKeyword);
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

        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (3,22): error CS0106: The modifier 'required' is not valid for this item
            //     int P { required get; file set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("required").WithLocation(3, 22),
            // (3,32): error CS0106: The modifier 'file' is not valid for this item
            //     int P { required get; file set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("file").WithLocation(3, 32),
            // (4,20): error CS0106: The modifier 'closed' is not valid for this item
            //     int Q { closed get; static set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("closed").WithLocation(4, 20),
            // (4,32): error CS0106: The modifier 'static' is not valid for this item
            //     int Q { closed get; static set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("static").WithLocation(4, 32),
            // (5,28): error CS1004: Duplicate 'private' modifier
            //     public int R { private private get; set; }
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "private").WithArguments("private").WithLocation(5, 28));
    }

    [Fact]
    public void AttributeBeforeReadonlyAccessorModifier()
    {
        const string declaration = "public int P { [System.Obsolete] readonly get => 0; set { } }";
        const string source = """
            struct S
            {
                public int P { [System.Obsolete] readonly get => 0; set { } }
            }
            """;

        UsingDeclaration(declaration);
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
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
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
                                    N(SyntaxKind.IdentifierToken, "Obsolete");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.SetKeyword);
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

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void AccessorRecoveryDoesNotConsumeFollowingMember()
    {
        const string source = """
            class C
            {
                int P
                {
                    get { return 0; }
                private int F;
            }
            """;

        UsingDeclaration(
            source,
            options: null,
            // (5,26): error CS1513: } expected
            //         get { return 0; }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 26));
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ReturnStatement);
                            {
                                N(SyntaxKind.ReturnKeyword);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.PrivateKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "F");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            // (5,26): error CS1513: } expected
            //         get { return 0; }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 26),
            // (6,17): warning CS0169: The field 'C.F' is never used
            //     private int F;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(6, 17));
    }

    [Fact]
    public void AccessorModifierBeforeFeature()
    {
        const string declaration = "public int P { get; private set; }";
        const string source = """
            class C
            {
                public int P { get; private set; }
            }
            """;
        var options = TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp1);

        UsingDeclaration(declaration, options);
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
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.SetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();

        CreateCompilation(source, parseOptions: options).VerifyDiagnostics(
            // (3,16): error CS8022: Feature 'automatically implemented properties' is not available in C# 1. Please use language version 3 or greater.
            //     public int P { get; private set; }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "P").WithArguments("automatically implemented properties", "3").WithLocation(3, 16),
            // (3,25): error CS8022: Feature 'access modifiers on properties' is not available in C# 1. Please use language version 2 or greater.
            //     public int P { get; private set; }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "private").WithArguments("access modifiers on properties", "2").WithLocation(3, 25));
    }

    [Fact]
    public void SafeAccessorModifierBeforeFeature()
    {
        const string declaration = "public int P { private safe get => 0; set { } }";
        const string source = """
            class C
            {
                public int P { private safe get => 0; set { } }
            }
            """;

        UsingDeclaration(declaration, TestOptions.Regular14);
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
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.SafeKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.SetKeyword);
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

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,28): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public int P { private safe get => 0; set { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "safe").WithArguments("updated memory safety rules").WithLocation(3, 28));
    }

    [Fact]
    public void AccessorModifiersRecognizedInOlderLanguageVersion()
    {
        const string source = """
            class C
            {
                int P { required get; }
                int Q { get; file set; }
                int R { get; closed init; }
            }
            """;

        UsingDeclaration(source, TestOptions.Regular10);
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
                        N(SyntaxKind.RequiredKeyword);
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "Q");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.SetAccessorDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.SetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.InitAccessorDeclaration);
                    {
                        N(SyntaxKind.ClosedKeyword);
                        N(SyntaxKind.InitKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
            // (3,22): error CS0106: The modifier 'required' is not valid for this item
            //     int P { required get; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("required").WithLocation(3, 22),
            // (4,23): error CS0106: The modifier 'file' is not valid for this item
            //     int Q { get; file set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("file").WithLocation(4, 23),
            // (5,25): error CS0106: The modifier 'closed' is not valid for this item
            //     int R { get; closed init; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "init").WithArguments("closed").WithLocation(5, 25),
            // (5,25): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
            //     int R { get; closed init; }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(5, 25));
    }

    [Fact]
    public void PartialAccessorModifierBeforeCloseBrace()
    {
        const string source = "int P { partial }";

        UsingDeclaration(
            source,
            options: null,
            // (1,17): error CS1014: A get or set accessor expected
            // int P { partial }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "}").WithLocation(1, 17));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PartialAccessorModifierBeforeEndOfFile()
    {
        const string source = "class C { int P { partial";

        UsingDeclaration(
            source,
            options: null,
            // (1,26): error CS1014: A get or set accessor expected
            // class C { int P { partial
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "").WithLocation(1, 26),
            // (1,26): error CS1513: } expected
            // class C { int P { partial
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 26),
            // (1,26): error CS1513: } expected
            // class C { int P { partial
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 26));
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
                        N(SyntaxKind.PartialKeyword);
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            M(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void AttributeAndPartialAccessorModifierBeforeCloseBrace()
    {
        const string source = "int P { [System.Obsolete] partial }";

        UsingDeclaration(
            source,
            options: null,
            // (1,35): error CS1014: A get or set accessor expected
            // int P { [System.Obsolete] partial }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "}").WithLocation(1, 35));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
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
                                    N(SyntaxKind.IdentifierToken, "Obsolete");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PartialFieldAfterIncompleteAccessorList()
    {
        const string source = """
            class C
            {
                int P
                {
                    get;
                partial int F;
            }
            """;

        UsingDeclaration(
            source,
            options: null,
            // (5,13): error CS1513: } expected
            //         get;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 13));
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "F");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void AttributeBeforeScopedAccessorModifier()
    {
        const string declaration = "int P { [System.Obsolete] scoped get; }";
        var source = $$"""class C { {{declaration}} }""";

        UsingDeclaration(declaration);
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
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
                                    N(SyntaxKind.IdentifierToken, "Obsolete");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            // (1,44): error CS0106: The modifier 'scoped' is not valid for this item
            // class C { int P { [System.Obsolete] scoped get; } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("scoped").WithLocation(1, 44));
    }

    [Fact]
    public void ContextualModifiersOnOtherAccessorKinds()
    {
        const string source = """
            class C
            {
                int P { partial init; }
                int this[int i] { scoped get; }
                event System.Action E { partial add { } scoped remove { } }
            }
            """;

        UsingDeclaration(source);
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "P");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.InitAccessorDeclaration);
                    {
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.InitKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.IndexerDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.ThisKeyword);
                N(SyntaxKind.BracketedParameterList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "i");
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.EventDeclaration);
            {
                N(SyntaxKind.EventKeyword);
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
                N(SyntaxKind.IdentifierToken, "E");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.AddAccessorDeclaration);
                    {
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.AddKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.RemoveAccessorDeclaration);
                    {
                        N(SyntaxKind.ScopedKeyword);
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
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     int P { partial init; }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 13),
            // (3,21): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
            //     int P { partial init; }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(3, 21),
            // (3,21): error CS8051: Auto-implemented properties must have get accessors.
            //     int P { partial init; }
            Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "init").WithLocation(3, 21),
            // (4,30): error CS0106: The modifier 'scoped' is not valid for this item
            //     int this[int i] { scoped get; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("scoped").WithLocation(4, 30),
            // (5,29): error CS1609: Modifiers cannot be placed on event accessor declarations
            //     event System.Action E { partial add { } scoped remove { } }
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "partial").WithLocation(5, 29),
            // (5,45): error CS1609: Modifiers cannot be placed on event accessor declarations
            //     event System.Action E { partial add { } scoped remove { } }
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "scoped").WithLocation(5, 45));
    }

    [Fact]
    public void AccessorWithoutBodyFollowedByAccessor_DirectMember()
    {
        const string source = "int P { get set { } }";

        UsingDeclaration(
            source,
            options: null,
            // (1,13): error CS8180: { or ; or => expected
            // int P { get set { } }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "set").WithLocation(1, 13));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.SetKeyword);
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
    public void AccessorWithoutBodyFollowedByAttributedAccessor_DirectMember()
    {
        const string source = "int P { get [A] set { } }";

        UsingDeclaration(
            source,
            options: null,
            // (1,13): error CS8180: { or ; or => expected
            // int P { get [A] set { } }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "[").WithLocation(1, 13));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.SetKeyword);
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
    public void AccessorWithoutBodyFollowedByModifiedAccessor_DirectMember()
    {
        const string source = "int P { get private set { } }";

        UsingDeclaration(
            source,
            options: null,
            // (1,13): error CS8180: { or ; or => expected
            // int P { get private set { } }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "private").WithLocation(1, 13));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.SetKeyword);
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
    public void AccessorWithoutBodyFollowedByModifiedExpressionBody_DirectMember()
    {
        const string source = "int P { get partial => 0; }";

        UsingDeclaration(
            source,
            options: null,
            // (1,13): error CS8180: { or ; or => expected
            // int P { get partial => 0; }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "partial").WithLocation(1, 13),
            // (1,21): error CS1014: A get or set accessor expected
            // int P { get partial => 0; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "=>").WithLocation(1, 21));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void AccessorWithoutBodyFollowedByUnknownAccessor_DirectMember()
    {
        const string source = "int P { get A; }";

        UsingDeclaration(
            source,
            options: null,
            // (1,13): error CS8180: { or ; or => expected
            // int P { get A; }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "A").WithLocation(1, 13),
            // (1,13): error CS1014: A get or set accessor expected
            // int P { get A; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "A").WithLocation(1, 13));
        N(SyntaxKind.PropertyDeclaration);
        {
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
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void RefGetFollowedByUnknownAccessor()
    {
        const string source = """
            class C
            {
                int P1 { ref get A; }
                int P2 { ref get A { } }
                int P3 { ref get A => 0; }
                int P4 { ref get A }
                int P5 { ref get A; set; }
            }
            """;

        UsingDeclaration(
            source,
            options: null,
            // (3,22): error CS8180: { or ; or => expected
            //     int P1 { ref get A; }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "A").WithLocation(3, 22),
            // (3,22): error CS1014: A get or set accessor expected
            //     int P1 { ref get A; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "A").WithLocation(3, 22),
            // (4,22): error CS8180: { or ; or => expected
            //     int P2 { ref get A { } }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "A").WithLocation(4, 22),
            // (4,22): error CS1014: A get or set accessor expected
            //     int P2 { ref get A { } }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "A").WithLocation(4, 22),
            // (5,22): error CS8180: { or ; or => expected
            //     int P3 { ref get A => 0; }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "A").WithLocation(5, 22),
            // (5,22): error CS1014: A get or set accessor expected
            //     int P3 { ref get A => 0; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "A").WithLocation(5, 22),
            // (6,22): error CS8180: { or ; or => expected
            //     int P4 { ref get A }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "A").WithLocation(6, 22),
            // (6,22): error CS1014: A get or set accessor expected
            //     int P4 { ref get A }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "A").WithLocation(6, 22),
            // (7,22): error CS8180: { or ; or => expected
            //     int P5 { ref get A; set; }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "A").WithLocation(7, 22),
            // (7,22): error CS1014: A get or set accessor expected
            //     int P5 { ref get A; set; }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "A").WithLocation(7, 22));
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "P1");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.GetKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.UnknownAccessorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "P2");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.GetKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.UnknownAccessorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "P3");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.GetKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.UnknownAccessorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "P4");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.GetKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.UnknownAccessorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "P5");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.GetKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.UnknownAccessorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
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
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void RefGetFollowedByParenthesizedUnknownAccessor()
    {
        const string source = "class C { int P { ref get A(); } }";

        UsingTree(
            source,
            // (1,27): error CS8180: { or ; or => expected
            // class C { int P { ref get A(); } }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "A").WithLocation(1, 27),
            // (1,27): error CS1014: A get or set accessor expected
            // class C { int P { ref get A(); } }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "A").WithLocation(1, 27),
            // (1,28): error CS1513: } expected
            // class C { int P { ref get A(); } }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 28),
            // (1,29): error CS8124: Tuple must contain at least two elements.
            // class C { int P { ref get A(); } }
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, 29),
            // (1,30): error CS1519: Invalid token ';' in a member declaration
            // class C { int P { ref get A(); } }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(1, 30),
            // (1,34): error CS1022: Type or namespace definition, or end-of-file expected
            // class C { int P { ref get A(); } }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 34));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
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
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            M(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.TupleElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        M(SyntaxKind.TupleElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ContextualKeywordTypeFollowingProperty()
    {
        const string source = "class C { int P { get; } partial unknown; }";

        UsingDeclaration(source);
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "unknown");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void KeywordModifierBeforeFollowingMember()
    {
        const string source = "class C { int P { private int F; } }";

        UsingTree(
            source,
            // (1,19): error CS1513: } expected
            // class C { int P { private int F; } }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "private").WithLocation(1, 19),
            // (1,36): error CS1022: Type or namespace definition, or end-of-file expected
            // class C { int P { private int F; } }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 36));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
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
    public void AccessorModifierWithBodyConsumesEnclosingTypeCloseBrace()
    {
        const string source = """
            class C
            {
                int P { get;
                unsafe { }
            }
            class D { }
            """;

        UsingTree(
            source,
            // (4,12): error CS1014: A get or set accessor expected
            //     unsafe { }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "{").WithLocation(4, 12),
            // (6,12): error CS1513: } expected
            // class D { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(6, 12));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.UnsafeKeyword);
                            M(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AccessorRecoveryDoesNotConsumeFollowingUnsafeMethod()
    {
        const string source = """
            class C
            {
                int P { get;
                unsafe void M() { }
            }
            class D { }
            """;

        UsingTree(
            source,
            // (3,17): error CS1513: } expected
            //     int P { get;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 17));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
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
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.UnsafeKeyword);
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "D");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void KeywordModifierWithMissingAccessorName()
    {
        const string source = "class C1 { int P { private { } } } class C2 { int P { private; } }";

        UsingTree(
            source,
            // (1,28): error CS1014: A get or set accessor expected
            // class C1 { int P { private { } } } class C2 { int P { private; } }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "{").WithLocation(1, 28),
            // (1,62): error CS1014: A get or set accessor expected
            // class C1 { int P { private { } } } class C2 { int P { private; } }
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, ";").WithLocation(1, 62));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C1");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
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
                            N(SyntaxKind.PrivateKeyword);
                            M(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C2");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
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
                            N(SyntaxKind.PrivateKeyword);
                            M(SyntaxKind.IdentifierToken);
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

    [Fact]
    public void TrailingContextualModifierInEventAccessorList()
    {
        const string source = "event System.Action E { scoped }";

        UsingDeclaration(
            source,
            options: null,
            // (1,32): error CS1055: An add or remove accessor expected
            // event System.Action E { scoped }
            Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "}").WithLocation(1, 32));
        N(SyntaxKind.EventDeclaration);
        {
            N(SyntaxKind.EventKeyword);
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
            N(SyntaxKind.IdentifierToken, "E");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    M(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ModifiedKnownAccessorWithForcedBody()
    {
        const string source = "class C { int P { ref get 0; } }";

        UsingDeclaration(
            source,
            options: null,
            // (1,27): error CS8180: { or ; or => expected
            // class C { int P { ref get 0; } }
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "0").WithLocation(1, 27),
            // (1,33): error CS1513: } expected
            // class C { int P { ref get 0; } }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 33));
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.PropertyDeclaration);
            {
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
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.Block);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            M(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class CompoundAssignmentInitializerParsingTests : ParsingTests
{
    public CompoundAssignmentInitializerParsingTests(ITestOutputHelper output) : base(output) { }

    // Derive the set of compound assignment operator tokens from `SyntaxFacts` rather than hand-listing
    // them: any token kind for which `IsAssignmentExpressionOperatorToken` returns true and which isn't
    // the simple `=` form is in scope. Per-test, the operator text and the corresponding
    // *AssignmentExpression kind are derived via `GetCompoundOperatorParts`. New compound operators
    // added to the language pick up coverage automatically.
    public static TheoryData<SyntaxKind> CompoundOperators
    {
        get
        {
            var data = new TheoryData<SyntaxKind>();
            foreach (var kind in Enum.GetValues<SyntaxKind>())
            {
                if (kind != SyntaxKind.EqualsToken && SyntaxFacts.IsAssignmentExpressionOperatorToken(kind))
                    data.Add(kind);
            }
            return data;
        }
    }

    private static (string text, SyntaxKind expressionKind) GetCompoundOperatorParts(SyntaxKind operatorTokenKind)
        => (SyntaxFacts.GetText(operatorTokenKind), SyntaxFacts.GetAssignmentExpression(operatorTokenKind));

    #region Object initializer: named member

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_AllCompoundOperators(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { Prop {{op}} 1 }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void ObjectInitializer_NamedMember_NoParseDiagnosticsAnyLangVersion(LanguageVersion languageVersion)
    {
        UsingExpression(
            "new Goo { Prop += 1 }",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AddAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(SyntaxKind.PlusEqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_MixOfSimpleAndCompound(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { Prop = 10, Prop {{op}} 5, Event {{op}} Handler }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "10");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "5");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Event");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Handler");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_TrailingComma(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { Prop {{op}} 1, }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_MissingRightHandSide(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        var source = $$"""new Goo { Prop {{op}} }""";
        var closeBracePosition = source.IndexOf('}') + 1;
        UsingExpression(source,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(1, closeBracePosition));

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_NestedInitializerOnRhs_PermissiveParse(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { Prop {{op}} { 1, 2 } }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.CollectionInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_RefOnRhs(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { Prop {{op}} ref x }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.RefExpression);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_GenericNameOnRhs(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { Prop {{op}} Bar<int>.Baz(x) }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Bar");
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
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Baz");
                            }
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Object initializer: dictionary (indexer) member

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_IndexerMember_MultipleArguments(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { [a, b] {{op}} mask }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.ImplicitElementAccess);
                    {
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "mask");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region With expression

    [Theory, MemberData(nameof(CompoundOperators))]
    public void WithExpression_AllCompoundOperators(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""r with { Value {{op}} 1 }""");

        N(SyntaxKind.WithExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "r");
            }
            N(SyntaxKind.WithKeyword);
            N(SyntaxKind.WithInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Value");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithExpression_NoParseDiagnosticsAnyLangVersion(LanguageVersion languageVersion)
    {
        UsingExpression(
            "r with { Value -= 1 }",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.WithExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "r");
            }
            N(SyntaxKind.WithKeyword);
            N(SyntaxKind.WithInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SubtractAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Value");
                    }
                    N(SyntaxKind.MinusEqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void WithExpression_MixOfSimpleAndCompound(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""r with { Value = 10, Value {{op}} 5, Changed {{op}} OnChanged }""");

        N(SyntaxKind.WithExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "r");
            }
            N(SyntaxKind.WithKeyword);
            N(SyntaxKind.WithInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Value");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "10");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Value");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "5");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Changed");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "OnChanged");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Object-vs-collection classification

    [Theory, MemberData(nameof(CompoundOperators))]
    public void Classifier_IndexerCompoundMembersAreObjectInitializer(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { [0] {{op}} a, [1] {{op}} b }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.ImplicitElementAccess);
                    {
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.ImplicitElementAccess);
                    {
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void Classifier_BareCompoundAssignmentOnNonMemberIsCollectionInitializer(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        // Left is a `SimpleMemberAccessExpression`, not `IdentifierName`/`ImplicitElementAccess`,
        // so this is not object-initializer evidence.
        UsingExpression($$"""new Goo { a.b {{op}} 1 }""");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.CollectionInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Full top-level usage

    [Fact]
    public void TopLevel_ObjectInitializer()
    {
        UsingTree("""
            var c = new Counter
            {
                Value = 10,
                Value += 5,
                Changed += OnChanged,
                Changed += OnChanged2,
            };
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.ObjectCreationExpression);
                                {
                                    N(SyntaxKind.NewKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Counter");
                                    }
                                    N(SyntaxKind.ObjectInitializerExpression);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Value");
                                            }
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "10");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.AddAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Value");
                                            }
                                            N(SyntaxKind.PlusEqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "5");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.AddAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Changed");
                                            }
                                            N(SyntaxKind.PlusEqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "OnChanged");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.AddAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Changed");
                                            }
                                            N(SyntaxKind.PlusEqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "OnChanged2");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TopLevel_WithExpression()
    {
        UsingTree("""
            var c = original with { Value -= 1, Changed += OnChanged };
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.WithExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "original");
                                    }
                                    N(SyntaxKind.WithKeyword);
                                    N(SyntaxKind.WithInitializerExpression);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.SubtractAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Value");
                                            }
                                            N(SyntaxKind.MinusEqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "1");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.AddAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Changed");
                                            }
                                            N(SyntaxKind.PlusEqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "OnChanged");
                                            }
                                        }
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void TopLevel_ImplicitObjectCreation(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new() { Prop {{op}} 1 }""");

        N(SyntaxKind.ImplicitObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(assignmentKind);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    N(operatorTokenKind);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Colon recovery

    [Fact]
    public void ColonRecovery_StillRecoversForSimpleAssignment()
    {
        UsingExpression("new Goo { Prop : 1 }",
            // (1,16): error CS1003: Syntax error, '=' expected
            // new Goo { Prop : 1 }
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=").WithLocation(1, 16));

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                    M(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion
}

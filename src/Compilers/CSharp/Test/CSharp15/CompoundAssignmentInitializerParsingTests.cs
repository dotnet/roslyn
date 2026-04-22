// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// Parsing tests for compound assignment in object initializer and <c>with</c> expression
/// (dotnet/csharplang#9896). The parser changes are permissive: any compound assignment operator
/// (including <c>??=</c>) is accepted after the target of a named or dictionary member initializer,
/// and the object-vs-collection classifier recognizes compound assignments as object-initializer
/// evidence. Language-version gating and target legality live in the binder.
/// </summary>
public sealed class CompoundAssignmentInitializerParsingTests : ParsingTests
{
    public CompoundAssignmentInitializerParsingTests(ITestOutputHelper output) : base(output) { }

    public static TheoryData<string, SyntaxKind, SyntaxKind> CompoundOperators => new()
    {
        { "+=",   SyntaxKind.PlusEqualsToken,                         SyntaxKind.AddAssignmentExpression },
        { "-=",   SyntaxKind.MinusEqualsToken,                        SyntaxKind.SubtractAssignmentExpression },
        { "*=",   SyntaxKind.AsteriskEqualsToken,                     SyntaxKind.MultiplyAssignmentExpression },
        { "/=",   SyntaxKind.SlashEqualsToken,                        SyntaxKind.DivideAssignmentExpression },
        { "%=",   SyntaxKind.PercentEqualsToken,                      SyntaxKind.ModuloAssignmentExpression },
        { "&=",   SyntaxKind.AmpersandEqualsToken,                    SyntaxKind.AndAssignmentExpression },
        { "|=",   SyntaxKind.BarEqualsToken,                          SyntaxKind.OrAssignmentExpression },
        { "^=",   SyntaxKind.CaretEqualsToken,                        SyntaxKind.ExclusiveOrAssignmentExpression },
        { "<<=",  SyntaxKind.LessThanLessThanEqualsToken,             SyntaxKind.LeftShiftAssignmentExpression },
        { ">>=",  SyntaxKind.GreaterThanGreaterThanEqualsToken,       SyntaxKind.RightShiftAssignmentExpression },
        { ">>>=", SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken, SyntaxKind.UnsignedRightShiftAssignmentExpression },
        // The parser is permissive: `??=` is accepted too. The binder rejects it as not a valid
        // compound_assignment_operator per the spec.
        { "??=",  SyntaxKind.QuestionQuestionEqualsToken,             SyntaxKind.CoalesceAssignmentExpression },
    };

    public static TheoryData<LanguageVersion> AllLanguageVersions => new()
    {
        LanguageVersion.CSharp1,
        LanguageVersion.CSharp2,
        LanguageVersion.CSharp3,
        LanguageVersion.CSharp4,
        LanguageVersion.CSharp5,
        LanguageVersion.CSharp6,
        LanguageVersion.CSharp7,
        LanguageVersion.CSharp7_1,
        LanguageVersion.CSharp7_2,
        LanguageVersion.CSharp7_3,
        LanguageVersion.CSharp8,
        LanguageVersion.CSharp9,
        LanguageVersion.CSharp10,
        LanguageVersion.CSharp11,
        LanguageVersion.CSharp12,
        LanguageVersion.CSharp13,
        LanguageVersion.CSharp14,
        LanguageVersion.Preview,
    };

    #region Object initializer: named member

    [Theory, MemberData(nameof(CompoundOperators))]
    public void ObjectInitializer_NamedMember_AllCompoundOperators(string op, SyntaxKind operatorTokenKind, SyntaxKind assignmentKind)
    {
        UsingExpression($"new Foo {{ Prop {op} 1 }}");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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

    [Theory, MemberData(nameof(AllLanguageVersions))]
    public void ObjectInitializer_NamedMember_NoParseDiagnosticsAnyLangVersion(LanguageVersion languageVersion)
    {
        UsingExpression(
            "new Foo { Prop += 1 }",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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

    [Fact]
    public void ObjectInitializer_NamedMember_MixOfSimpleAndCompound()
    {
        UsingExpression("new Foo { Prop = 10, Prop += 5, Event += Handler }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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
                N(SyntaxKind.AddAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
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
                        N(SyntaxKind.IdentifierToken, "Event");
                    }
                    N(SyntaxKind.PlusEqualsToken);
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

    [Fact]
    public void ObjectInitializer_NamedMember_TrailingComma()
    {
        UsingExpression("new Foo { Prop += 1, }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ObjectInitializer_NamedMember_MissingRightHandSide()
    {
        // Parser is resilient: it consumes the operator then expects an expression; on missing
        // expression it produces a missing identifier so the tree still shapes as compound.
        UsingExpression("new Foo { Prop += }",
            // (1,19): error CS1525: Invalid expression term '}'
            // new Foo { Prop += }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(1, 19));

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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

    [Fact]
    public void ObjectInitializer_NamedMember_NestedInitializerOnRhs_PermissiveParse()
    {
        // The spec note calls `Prop += { 1, 2 }` "syntactically ill-formed", but the parser is
        // permissive and builds a nested `ObjectInitializerExpression`/`CollectionInitializerExpression`
        // for resilience. The binder rejects this shape.
        UsingExpression("new Foo { Prop += { 1, 2 } }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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

    [Fact]
    public void ObjectInitializer_NamedMember_RefOnRhs()
    {
        // `Prop += ref x` parses: the parser uses `ParsePossibleRefExpression` for the RHS and
        // produces a `RefExpression` wrapping the identifier. The binder rejects compound+ref.
        UsingExpression("new Foo { Prop += ref x }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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

    [Fact]
    public void ObjectInitializer_NamedMember_GenericNameOnRhs()
    {
        // Arbitrary expression on the RHS: member access, invocation, generic name.
        UsingExpression("new Foo { Prop += Bar<int>.Baz(x) }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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
    public void ObjectInitializer_IndexerMember_AllCompoundOperators(string op, SyntaxKind operatorTokenKind, SyntaxKind assignmentKind)
    {
        UsingExpression($"new Foo {{ [0] {op} 1 }}");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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

    [Fact]
    public void ObjectInitializer_IndexerMember_MultipleArguments()
    {
        UsingExpression("new Foo { [a, b] |= mask }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.OrAssignmentExpression);
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
                    N(SyntaxKind.BarEqualsToken);
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
    public void WithExpression_AllCompoundOperators(string op, SyntaxKind operatorTokenKind, SyntaxKind assignmentKind)
    {
        UsingExpression($"r with {{ Value {op} 1 }}");

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

    [Theory, MemberData(nameof(AllLanguageVersions))]
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

    [Fact]
    public void WithExpression_MixOfSimpleAndCompound()
    {
        UsingExpression("r with { Value = 10, Value += 5, Changed += OnChanged }");

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
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Object-vs-collection classification

    [Fact]
    public void Classifier_AllCompoundMembersAreObjectInitializer()
    {
        // Prior to the feature, a brace list containing only non-`=` assignments classified as
        // `CollectionInitializerExpression`. With the feature, the classifier recognizes compound
        // assignments with identifier/implicit-element-access targets as object-initializer evidence.
        UsingExpression("new Foo { Prop += 1, Event += Handler }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.AddAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Event");
                    }
                    N(SyntaxKind.PlusEqualsToken);
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

    [Fact]
    public void Classifier_IndexerCompoundMembersAreObjectInitializer()
    {
        UsingExpression("new Foo { [0] |= a, [1] &= b }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.OrAssignmentExpression);
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
                    N(SyntaxKind.BarEqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.AndAssignmentExpression);
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
                    N(SyntaxKind.AmpersandEqualsToken);
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

    [Fact]
    public void Classifier_BareCompoundAssignmentOnNonMemberIsCollectionInitializer()
    {
        // `a.b += 1` is a compound assignment, but its left is a `SimpleMemberAccessExpression`,
        // not an `IdentifierName` or `ImplicitElementAccess`. Per the classifier, this alone is NOT
        // object-initializer evidence, so the brace list classifies as a collection initializer.
        UsingExpression("new Foo { a.b += 1 }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
            }
            N(SyntaxKind.CollectionInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AddAssignmentExpression);
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

    [Fact]
    public void Classifier_EmptyBracesIsObjectInitializer()
    {
        // Classifier pre-existing rule: empty brace list is always an object initializer.
        UsingExpression("new Foo { }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
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

    [Fact]
    public void TopLevel_ImplicitObjectCreation()
    {
        // `new() { Prop += 1 }` target-typed form.
        UsingExpression("new() { Prop += 1 }");

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

    #endregion

    #region Colon recovery

    [Fact]
    public void ColonRecovery_StillRecoversForSimpleAssignment()
    {
        // Pre-existing recovery: `Prop :` is treated as a missing `=`. We preserve this for the
        // simple-assignment path only; compound forms do not get colon recovery because the spec
        // has no ambiguity for them.
        UsingExpression("new Foo { Prop : 1 }",
            // (1,16): error CS1003: Syntax error, '=' expected
            // new Foo { Prop : 1 }
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=").WithLocation(1, 16));

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Foo");
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
                    // `EatTokenAsKind` produces a missing `=` with the `:` attached as skipped trivia.
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

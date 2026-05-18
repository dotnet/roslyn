// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

// Parser tree-shape tests for the "mixed" object initializer form: a `{ ... }` initializer body
// containing both member-shaped initializer elements (`Name = value`, `Name op= value`,
// `[args] = value`) and bare-expression element initializers (`Add` targets).
//
// The parser already accepts every mixed shape today. The classifier in
// `LanguageParser.ParseObjectOrCollectionInitializer` flips the wrapper to
// `ObjectInitializerExpression` iff at least one element is an `AssignmentExpressionSyntax` whose
// `Left.Kind` is `IdentifierName` or `ImplicitElementAccess`; otherwise the wrapper is
// `CollectionInitializerExpression`. The empty list is `ObjectInitializerExpression`. Assignments
// whose `Left.Kind` is anything else (e.g. `SimpleMemberAccessExpression`) do not count as
// "object" evidence even though they look member-like to a human. This file pins that exact rule
// across the mixed-shape cases.
//
// Other initializer-bearing productions (array initializers, stackalloc initializers, collection
// expressions, anonymous-object creation) use distinct grammar productions and are unaffected by
// this proposal; they are not covered here.
//
// Binding-time rejection of mixed lists continues at every language version until the binding PR
// flips the feature gate.
public sealed class MixedInitializerParsingTests : ParsingTests
{
    public MixedInitializerParsingTests(ITestOutputHelper output) : base(output) { }

    // Mirrors `CompoundAssignmentInitializerParsingTests.CompoundOperators` (and must stay in sync
    // with it) so future operators added to the language pick up coverage automatically.
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

    #region Sanity invariants

    [Fact]
    public void Empty_ClassifiesAsObjectInitializer()
    {
        UsingExpression("new Goo { }");

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
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PureMembers_ClassifiesAsObjectInitializer()
    {
        UsingExpression("new Goo { X = 1, Y = 2 }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Y");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PureElements_ClassifiesAsCollectionInitializer()
    {
        UsingExpression("new Goo { 1, 2, 3 }");

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
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Minimum-shape flips — one qualifying assignment is enough

    [Fact]
    public void MinimumFlip_LeadingMember_ObjectInitializer()
    {
        UsingExpression("new Goo { X = 1, 2 }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void MinimumFlip_TrailingMember_ObjectInitializer()
    {
        UsingExpression("new Goo { 1, X = 2 }");

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
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void MinimumFlip_SimpleIndexerMember_ObjectInitializer()
    {
        // `[0] = 1` is `SimpleAssignmentExpression` with `Left.Kind == ImplicitElementAccess`,
        // which qualifies under the classifier in the same way `IdentifierName` does.
        UsingExpression("""new Goo { [0] = 1, 2 }""");

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
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Classifier boundary cases — `Left.Kind` is what counts

    [Fact]
    public void Boundary_QualifierAssignmentDoesNotFlip_StaysCollection()
    {
        // `a.b += 2` is `AddAssignmentExpression` but `Left.Kind` is `SimpleMemberAccessExpression`,
        // not `IdentifierName` or `ImplicitElementAccess` — so it is *not* object-initializer
        // evidence. Mixed with bare elements only, the wrapper stays `CollectionInitializerExpression`.
        UsingExpression("new Goo { 1, a.b += 2 }");

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
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.CommaToken);
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
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Boundary_QualifierAssignmentAlongsideQualifyingMember_FlipsToObject()
    {
        // Same `a.b += 1` qualifier-style element as above; here a second element `X = 2` *does*
        // qualify, so the wrapper flips to `ObjectInitializerExpression`. Pins that the
        // classifier scans for *any* qualifying element, not a uniform per-element predicate.
        UsingExpression("new Goo { a.b += 1, X = 2 }");

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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Mixed shapes — wrapper kind is ObjectInitializerExpression

    [Fact]
    public void Mixed_MembersFirstElementsLast_ObjectInitializer()
    {
        UsingExpression("new Goo { X = 1, Y = 2, 3, 4 }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Y");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "4");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_ElementsFirstMembersLast_ObjectInitializer()
    {
        UsingExpression("new Goo { 1, 2, X = 3, Y = 4 }");

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
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "3");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Y");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "4");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_Interleaved_ObjectInitializer()
    {
        UsingExpression("new Goo { 1, X = 2, 3, Y = 4, 5 }");

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
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Y");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "4");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "5");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void Mixed_CompoundMemberInterleavedWithElements_ObjectInitializer(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { Prop {{op}} 1, 2, 3 }""");

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
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CompoundOperators))]
    public void Mixed_IndexerCompoundMemberInterleavedWithElements_ObjectInitializer(SyntaxKind operatorTokenKind)
    {
        var (op, assignmentKind) = GetCompoundOperatorParts(operatorTokenKind);
        UsingExpression($$"""new Goo { [0] {{op}} 1, 2, 3 }""");

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
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_BraceListElement_ObjectInitializer()
    {
        // The `{ a, b }` brace-list element initializer is still a `ComplexElementInitializerExpression`,
        // even when it appears inside the new mixed object-initializer wrapper.
        UsingExpression("new Goo { X = 1, { 2, 3 } }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ComplexElementInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "3");
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_NestedObjectCreationOnElementSide_ObjectInitializer()
    {
        // The element-side child can itself be an `ObjectCreationExpression` carrying its own initializer.
        UsingExpression("new Goo { X = 1, new Bar { Y = 2 } }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Bar");
                    }
                    N(SyntaxKind.ObjectInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Y");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
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
    public void Mixed_TrailingComma_ObjectInitializer()
    {
        UsingExpression("new Goo { X = 1, 2, 3, }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_TargetTypedNew_ObjectInitializer()
    {
        UsingExpression("new() { X = 1, 2, 3 }");

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
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_NewWithConstructorArgs_ObjectInitializer()
    {
        // The `new T(args) { ... }` form (constructor arg list present) carries the same initializer
        // body parsing as `new T { ... }`; classifier behavior is identical.
        UsingExpression("new Goo(1, 2) { X = 1, 2, 3 }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_NestedInitializerOnEqualsRhs_OuterObjectInnerCollection()
    {
        // `X = { 2, 3 }` is the "nested initializer" first-form member_initializer: its right-hand
        // side recurses into `ParseObjectOrCollectionInitializer`, which classifies *that* `{ 2, 3 }`
        // as `CollectionInitializerExpression` (no qualifying element). The *outer* wrapper still
        // sees `X = ...` as a qualifying assignment and stays `ObjectInitializerExpression`.
        UsingExpression("new Goo { X = { 2, 3 }, 4 }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.CollectionInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "4");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Recovery and separator shapes that still classify correctly

    [Fact]
    public void Mixed_ColonRecoveryOnMember_ClassifiesAsObjectInitializer()
    {
        // `X: 1` is the parser's colon-recovery shape for `X = 1` (the operator token is recovered
        // as `EqualsToken`). The resulting `SimpleAssignmentExpression` with `IdentifierName` left
        // still qualifies, so the mixed wrapper is `ObjectInitializerExpression`.
        UsingExpression(
            "new Goo { X: 1, 2 }",
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=").WithLocation(1, 12));

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    M(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Mixed_SemicolonSeparatorBetweenElements_ClassifiesAsObjectInitializer()
    {
        // `ParseObjectOrCollectionInitializer` calls `ParseCommaSeparatedSyntaxList` with
        // `allowSemicolonAsSeparator: true`. The separator token kind does not feed the classifier;
        // element shape does, so a mixed list using `;` separators still classifies as
        // `ObjectInitializerExpression`.
        UsingExpression(
            "new Goo { X = 1; 2 }",
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 16));

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region `with` expression — parser permissiveness (feature out of scope, regression pin)

    [Fact]
    public void With_MixedShape_ParserAcceptsAsWithInitializerExpression()
    {
        // The mixed-initializer proposal explicitly does *not* extend `with`. The parser is
        // permissive at the `WithInitializerExpression` level — it accepts any sequence of
        // expressions in the body. This test pins that the parser tree shape for `r with { X = 1, 2 }`
        // is unaffected by anything else in PR 1; binder rejection of the bare element initializer
        // continues to live elsewhere.
        UsingExpression("r with { X = 1, 2 }");

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
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion

    #region Language version: parser is permissive at every version

    [Theory, CombinatorialData]
    public void Mixed_NoParseDiagnosticsAnyLangVersion(LanguageVersion languageVersion)
    {
        UsingExpression(
            "new Goo { X = 1, 2, 3 }",
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
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion
}

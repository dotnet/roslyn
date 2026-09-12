// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed partial class ModifierParserRecoveryTests
{
    [Fact]
    public void PartialBeforeIntMethod()
    {
        UsingDeclaration("""partial int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PublicPartialBeforeIntMethod()
    {
        UsingDeclaration("""public partial int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PartialPublicBeforeIntMethod()
    {
        UsingDeclaration("""partial public int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void AsyncPartialBeforeIntMethod()
    {
        UsingDeclaration("""async partial int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PartialAsyncBeforeIntMethod()
    {
        UsingDeclaration("""partial async int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PublicAsyncPartialBeforeIntMethod()
    {
        UsingDeclaration("""public async partial int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PublicPartialAsyncBeforeIntMethod()
    {
        UsingDeclaration("""public partial async int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void AsyncPublicPartialBeforeIntMethod()
    {
        UsingDeclaration("""async public partial int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void AsyncPartialPublicBeforeIntMethod()
    {
        UsingDeclaration("""async partial public int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PartialPublicAsyncBeforeIntMethod()
    {
        UsingDeclaration("""partial public async int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PartialAsyncPublicBeforeIntMethod()
    {
        UsingDeclaration("""partial async public int M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
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
    public void PartialBeforeTupleReturningMethod()
    {
        UsingDeclaration("""partial (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PublicPartialBeforeTupleReturningMethod()
    {
        UsingDeclaration("""public partial (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PartialPublicBeforeTupleReturningMethod()
    {
        UsingDeclaration("""partial public (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void AsyncPartialBeforeTupleReturningMethod()
    {
        UsingDeclaration("""async partial (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PartialAsyncBeforeTupleReturningMethod()
    {
        UsingDeclaration("""partial async (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PublicAsyncPartialBeforeTupleReturningMethod()
    {
        UsingDeclaration("""public async partial (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PublicPartialAsyncBeforeTupleReturningMethod()
    {
        UsingDeclaration("""public partial async (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void AsyncPublicPartialBeforeTupleReturningMethod()
    {
        UsingDeclaration("""async public partial (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void AsyncPartialPublicBeforeTupleReturningMethod()
    {
        UsingDeclaration("""async partial public (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PartialPublicAsyncBeforeTupleReturningMethod()
    {
        UsingDeclaration("""partial public async (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PartialAsyncPublicBeforeTupleReturningMethod()
    {
        UsingDeclaration("""partial async public (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PartialBeforeNestedTupleReturningMethod()
    {
        UsingDeclaration("""partial ((int x, int y) left, int right) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
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
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "left");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "right");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PartialBeforeRefTupleReturningMethod()
    {
        UsingDeclaration("""partial ref (int x, int y) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.RefType);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
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
    public void PartialBeforeField()
    {
        UsingDeclaration("""partial int F;""");
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
        EOF();
    }

    [Fact]
    public void PartialBeforeProperty()
    {
        UsingDeclaration("""partial int P { get; }""");
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
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
    public void PartialBeforeIndexer()
    {
        UsingDeclaration("""partial int this[int i] { get; }""");
        N(SyntaxKind.IndexerDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
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
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PartialBeforeOperator()
    {
        UsingDeclaration("""partial int operator +(C value) => value;""");
        N(SyntaxKind.OperatorDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.OperatorKeyword);
            N(SyntaxKind.PlusToken);
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.IdentifierToken, "value");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.ArrowExpressionClause);
            {
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "value");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialBeforeConversionOperator()
    {
        UsingDeclaration("""partial implicit operator int(C value) => 0;""");
        N(SyntaxKind.ConversionOperatorDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.ImplicitKeyword);
            N(SyntaxKind.OperatorKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.IdentifierToken, "value");
                }
                N(SyntaxKind.CloseParenToken);
            }
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
        EOF();
    }

    [Fact]
    public void PartialBeforeDestructor()
    {
        UsingDeclaration("""partial ~C() { }""");
        N(SyntaxKind.DestructorDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
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
    public void PartialBeforeConstField()
    {
        UsingDeclaration("""partial const int F = 0;""");
        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.ConstKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialBeforeEventField()
    {
        UsingDeclaration("""partial event System.Action E;""");
        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
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
    public void PartialBeforeNestedClass()
    {
        UsingDeclaration("""partial class N { }""");
        N(SyntaxKind.ClassDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.ClassKeyword);
            N(SyntaxKind.IdentifierToken, "N");
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void PartialBeforeDelegate()
    {
        UsingDeclaration("""partial delegate void D();""");
        N(SyntaxKind.DelegateDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.DelegateKeyword);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.VoidKeyword);
            }
            N(SyntaxKind.IdentifierToken, "D");
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
    public void PartialModifierBeforeArrayTypeWithMissingElementType()
    {
        UsingDeclaration(
            """partial[] M();""",
            null,
            // (1,8): error CS1031: Type expected
            // partial[] M();
            Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(1, 8));
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.ArrayType);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
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
    public void PartialModifierBeforeJaggedArrayTypeWithMissingElementType()
    {
        UsingDeclaration(
            """partial[][] M();""",
            null,
            // (1,8): error CS1031: Type expected
            // partial[][] M();
            Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(1, 8));
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.ArrayType);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
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
    public void PartialModifierBeforeNullableTypeWithMissingElementType()
    {
        UsingDeclaration(
            """partial? M();""",
            null,
            // (1,8): error CS1031: Type expected
            // partial? M();
            Diagnostic(ErrorCode.ERR_TypeExpected, "?").WithLocation(1, 8));
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.NullableType);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.QuestionToken);
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
    public void PartialModifierBeforePointerTypeWithMissingElementType()
    {
        UsingDeclaration(
            """partial* M();""",
            null,
            // (1,8): error CS1031: Type expected
            // partial* M();
            Diagnostic(ErrorCode.ERR_TypeExpected, "*").WithLocation(1, 8));
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PointerType);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.AsteriskToken);
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
    public void PartialModifierBeforeDotProducesIncompleteMember()
    {
        UsingDeclaration(
            """partial.N M();""",
            null,
            // (1,1): error CS1073: Unexpected token '.'
            // partial.N M();
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "partial").WithArguments(".").WithLocation(1, 1),
            // (1,8): error CS1519: Invalid token '.' in a member declaration
            // partial.N M();
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ".").WithArguments(".").WithLocation(1, 8));
        N(SyntaxKind.IncompleteMember);
        {
            N(SyntaxKind.PartialKeyword);
        }
        EOF();
    }

    [Fact]
    public void PartialModifierBeforeAliasQualifiedTypeWithMissingAlias()
    {
        UsingDeclaration(
            """partial::N M();""",
            null,
            // (1,8): error CS1001: Identifier expected
            // partial::N M();
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "::").WithLocation(1, 8));
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AliasQualifiedName);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.ColonColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
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
    public void PartialModifierBeforeAsyncTypeProducesIncompleteMember()
    {
        UsingDeclaration(
            """partial async;""",
            null,
            // (1,1): error CS1073: Unexpected token ';'
            // partial async;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "partial async").WithArguments(";").WithLocation(1, 1),
            // (1,14): error CS1519: Invalid token ';' in a member declaration
            // partial async;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(1, 14));
        N(SyntaxKind.IncompleteMember);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "async");
            }
        }
        EOF();
    }

    [Fact]
    public void PartialModifierBeforeRequiredModifierWithMissingTypeAndName()
    {
        UsingDeclaration(
            """partial required => value;""",
            null,
            // (1,18): error CS1031: Type expected
            // partial required => value;
            Diagnostic(ErrorCode.ERR_TypeExpected, "=>").WithLocation(1, 18),
            // (1,18): error CS1001: Identifier expected
            // partial required => value;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(1, 18));
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.RequiredKeyword);
            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }
            M(SyntaxKind.IdentifierToken);
            N(SyntaxKind.ArrowExpressionClause);
            {
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "value");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialModifierBeforeFileModifierWithMissingTypeAndName()
    {
        UsingDeclaration(
            """partial file { get; }""",
            null,
            // (1,14): error CS1031: Type expected
            // partial file { get; }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(1, 14),
            // (1,14): error CS1001: Identifier expected
            // partial file { get; }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 14));
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.FileKeyword);
            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }
            M(SyntaxKind.IdentifierToken);
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
    public void PartialModifierBeforeGenericTypeProducesIncompleteMember()
    {
        UsingDeclaration(
            """partial safe<T>() { }""",
            null,
            // (1,1): error CS1073: Unexpected token '('
            // partial safe<T>() { }
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "partial safe<T>").WithArguments("(").WithLocation(1, 1),
            // (1,16): error CS1519: Invalid token '(' in a member declaration
            // partial safe<T>() { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(1, 16));
        N(SyntaxKind.IncompleteMember);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "safe");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void PartialConstructorWithoutParameters()
    {
        UsingDeclaration("""partial() { }""");
        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialConstructorWithParameters()
    {
        UsingDeclaration("""partial(int x, int y) { }""");
        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.IdentifierToken, "partial");
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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "y");
                }
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
    public void PartialGenericMethodWithoutReturnType()
    {
        UsingDeclaration(
            """partial<T>() { }""",
            null,
            // (1,1): error CS1520: Method must have a return type
            // partial<T>() { }
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "partial").WithLocation(1, 1));
        N(SyntaxKind.MethodDeclaration);
        {
            M(SyntaxKind.PredefinedType);
            {
                M(SyntaxKind.VoidKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
            N(SyntaxKind.TypeParameterList);
            {
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.TypeParameter);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                N(SyntaxKind.GreaterThanToken);
            }
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
    public void PartialGenericMethodNameFollowedByMalformedParameter()
    {
        UsingDeclaration(
            """partial<T> M();""",
            null,
            // (1,1): error CS1520: Method must have a return type
            // partial<T> M();
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "partial").WithLocation(1, 1),
            // (1,12): error CS1003: Syntax error, '(' expected
            // partial<T> M();
            Diagnostic(ErrorCode.ERR_SyntaxError, "M").WithArguments("(").WithLocation(1, 12),
            // (1,13): error CS1001: Identifier expected
            // partial<T> M();
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 13),
            // (1,13): error CS1003: Syntax error, ',' expected
            // partial<T> M();
            Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(1, 13),
            // (1,14): error CS8124: Tuple must contain at least two elements.
            // partial<T> M();
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, 14),
            // (1,15): error CS1001: Identifier expected
            // partial<T> M();
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 15),
            // (1,15): error CS1026: ) expected
            // partial<T> M();
            Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 15));
        N(SyntaxKind.MethodDeclaration);
        {
            M(SyntaxKind.PredefinedType);
            {
                M(SyntaxKind.VoidKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
            N(SyntaxKind.TypeParameterList);
            {
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.TypeParameter);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                N(SyntaxKind.GreaterThanToken);
            }
            N(SyntaxKind.ParameterList);
            {
                M(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "M");
                    }
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.Parameter);
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
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialFieldName()
    {
        UsingDeclaration(
            """partial;""",
            null,
            // (1,1): error CS1520: Method must have a return type
            // partial;
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "partial").WithLocation(1, 1));
        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                M(SyntaxKind.PredefinedType);
                {
                    M(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialInitializedFieldName()
    {
        UsingDeclaration(
            """partial = null;""",
            null,
            // (1,1): error CS1520: Method must have a return type
            // partial = null;
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "partial").WithLocation(1, 1));
        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                M(SyntaxKind.PredefinedType);
                {
                    M(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
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
        }
        EOF();
    }

    [Fact]
    public void PartialExpressionBodiedPropertyName()
    {
        UsingDeclaration(
            """partial => null;""",
            null,
            // (1,1): error CS1520: Method must have a return type
            // partial => null;
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "partial").WithLocation(1, 1));
        N(SyntaxKind.PropertyDeclaration);
        {
            M(SyntaxKind.PredefinedType);
            {
                M(SyntaxKind.VoidKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
            N(SyntaxKind.ArrowExpressionClause);
            {
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialPropertyName()
    {
        UsingDeclaration(
            """partial { get; }""",
            null,
            // (1,1): error CS1520: Method must have a return type
            // partial { get; }
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "partial").WithLocation(1, 1));
        N(SyntaxKind.PropertyDeclaration);
        {
            M(SyntaxKind.PredefinedType);
            {
                M(SyntaxKind.VoidKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialFirstVariableName()
    {
        UsingDeclaration(
            """partial, other;""",
            null,
            // (1,1): error CS1520: Method must have a return type
            // partial, other;
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "partial").WithLocation(1, 1));
        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                M(SyntaxKind.PredefinedType);
                {
                    M(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "other");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialMethodName()
    {
        UsingDeclaration("""int partial();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialGenericMethodName()
    {
        UsingDeclaration("""int partial<T>();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
            N(SyntaxKind.TypeParameterList);
            {
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.TypeParameter);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                N(SyntaxKind.GreaterThanToken);
            }
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
    public void PartialVariableName()
    {
        UsingDeclaration("""int partial;""");
        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialInitializedVariableName()
    {
        UsingDeclaration("""int partial = 0;""");
        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialExpressionBodiedPropertyAfterType()
    {
        UsingDeclaration("""int partial => 0;""");
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
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
        EOF();
    }

    [Fact]
    public void PartialPropertyAfterType()
    {
        UsingDeclaration("""int partial { get; }""");
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialAsGenericTypeArgument()
    {
        UsingDeclaration("""G<partial> M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "G");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
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
    public void PartialAsTupleElementType()
    {
        UsingDeclaration("""(partial value, int count) M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.IdentifierToken, "value");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "count");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void PartialAsFunctionPointerParameterType()
    {
        UsingDeclaration("""delegate*<partial, void> M();""");
        N(SyntaxKind.MethodDeclaration);
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
                            N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialAsRefReturnElementType()
    {
        UsingDeclaration("""ref partial M();""");
        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.RefType);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
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

}

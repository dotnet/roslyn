// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CrefParsingTests : ParsingTests
    {
        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            throw new NotSupportedException();
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options)
        {
            var commentText = string.Format(@"/// <see cref=""{0}""/>", text);
            var trivia = SyntaxFactory.ParseLeadingTrivia(commentText).Single();
            var structure = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
            var attr = structure.DescendantNodes().OfType<XmlCrefAttributeSyntax>().Single();
            return attr.Cref;
        }

        #region Name members

        #region Unqualified

        [Fact]
        public void UnqualifiedNameMember1()
        {
            UsingNode("A");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
        }

        [Fact]
        public void UnqualifiedNameMember2()
        {
            UsingNode("A{B}");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
            }
        }

        [Fact]
        public void UnqualifiedNameMember3()
        {
            UsingNode("A()");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Fact]
        public void UnqualifiedNameMember4()
        {
            UsingNode("A{B}()");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        #endregion Unqualified

        #region Qualified

        [Fact]
        public void QualifiedNameMember1()
        {
            UsingNode("T.A");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void QualifiedNameMember2()
        {
            UsingNode("T.A{B}");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void QualifiedNameMember3()
        {
            UsingNode("T.A()");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CrefParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
        }

        [Fact]
        public void QualifiedNameMember4()
        {
            UsingNode("T.A{B}()");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.CrefParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
        }

        #endregion Qualified

        #endregion Name Members

        #region Indexer members

        #region Unqualified

        [Fact]
        public void UnqualifiedIndexerMember1()
        {
            UsingNode("this");

            N(SyntaxKind.IndexerMemberCref);
            {
                N(SyntaxKind.ThisKeyword);
            }
        }

        [Fact]
        public void UnqualifiedIndexerMember2()
        {
            UsingNode("this[A]");

            N(SyntaxKind.IndexerMemberCref);
            {
                N(SyntaxKind.ThisKeyword);
                N(SyntaxKind.CrefBracketedParameterList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
        }

        #endregion Unqualified

        #region Qualified

        [Fact]
        public void QualifiedIndexerMember1()
        {
            UsingNode("T.this");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IndexerMemberCref);
                {
                    N(SyntaxKind.ThisKeyword);
                }
            }
        }

        [Fact]
        public void QualifiedIndexerMember2()
        {
            UsingNode("T.this[A]");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IndexerMemberCref);
                {
                    N(SyntaxKind.ThisKeyword);
                    N(SyntaxKind.CrefBracketedParameterList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.CrefParameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
        }

        #endregion Qualified

        #endregion Indexer Members

        #region Operator members

        #region Unqualified

        [Fact]
        public void UnqualifiedOperatorMember1()
        {
            UsingNode("operator +");

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.PlusToken);
            }
        }

        [Fact]
        public void UnqualifiedOperatorMember2()
        {
            UsingNode("operator +(A)");

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        #endregion Unqualified

        #region Qualified

        [Fact]
        public void QualifiedOperatorMember1()
        {
            UsingNode("T.operator +");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.OperatorMemberCref);
                {
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
                }
            }
        }

        [Fact]
        public void QualifiedOperatorMember2()
        {
            UsingNode("T.operator +(A)");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.OperatorMemberCref);
                {
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.CrefParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CrefParameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
        }

        #endregion Qualified

        #region Ambiguities

        [WorkItem(546992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546992")]
        [Fact]
        public void GreaterThanGreaterThan()
        {
            UsingNode("operator }}(A{A{T}})");

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.GreaterThanGreaterThanToken); // >>
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.GreaterThanToken); // just >
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken); // just >
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        #endregion Ambiguities

        #endregion Operator Members

        #region Conversion Operator members

        #region Unqualified

        [Fact]
        public void UnqualifiedConversionOperatorMember1()
        {
            UsingNode("implicit operator A");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
        }

        [Fact]
        public void UnqualifiedConversionOperatorMember2()
        {
            UsingNode("explicit operator A(B)");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        #endregion Unqualified

        #region Qualified

        [Fact]
        public void QualifiedConversionOperatorMember1()
        {
            UsingNode("T.implicit operator A");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.ConversionOperatorMemberCref);
                {
                    N(SyntaxKind.ImplicitKeyword);
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void QualifiedConversionOperatorMember2()
        {
            UsingNode("T.explicit operator A(B)");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.ConversionOperatorMemberCref);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CrefParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CrefParameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
        }

        #endregion Qualified

        #endregion Conversion Operator Members

        #region Parameters

        [Fact]
        public void ParameterCount()
        {
            UsingNode("A()");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }

            UsingNode("A(B)");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }

            UsingNode("A(B, C)");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Fact]
        public void ParameterRefKind()
        {
            UsingNode("A(ref B, out C)");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Fact]
        public void ParameterNullableType()
        {
            UsingNode("A(B?)");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Fact]
        public void ParameterPointerType()
        {
            UsingNode("A(B*, C**)");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.AsteriskToken);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.AsteriskToken);
                            }
                            N(SyntaxKind.AsteriskToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [WorkItem(531157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531157")]
        [Fact]
        public void ParameterVoidPointerType()
        {
            UsingNode("IntPtr.op_Explicit(void*)");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CrefParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CrefParameter);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ParameterArrayType()
        {
            UsingNode("A(B[], C[,][,,])");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CommaToken);
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
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Fact]
        public void ParameterComplex()
        {
            UsingNode("A(ref int?*[], out B::C{D}.E?[,][])");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.AsteriskToken);
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
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.AliasQualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.ColonColonToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CommaToken);
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
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [WorkItem(531154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531154")]
        [Fact]
        public void NestedArrayTypes()
        {
            UsingNode("F(A{int[], B?, C?*[,]})");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
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
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PointerType);
                                    {
                                        N(SyntaxKind.NullableType);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.QuestionToken);
                                        }
                                        N(SyntaxKind.AsteriskToken);
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.OmittedArraySizeExpression);
                                        {
                                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                                        }
                                        N(SyntaxKind.CommaToken);
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
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        #endregion Parameters

        #region Conversion operator return types

        [WorkItem(531154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531154")]
        [Fact]
        public void PrimitiveArrayReturnType()
        {
            UsingNode("explicit operator int[]");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
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
            EOF();
        }

        [WorkItem(531154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531154")]
        [Fact]
        public void NamedTypeArrayReturnType()
        {
            UsingNode("explicit operator C[]");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
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
            EOF();
        }

        #endregion Conversion operator return types

        #region Qualified

        [Fact]
        public void Qualified1()
        {
            // NOTE: since int.A won't fit into a TypeSyntax, it is represented as 
            // a qualified cref member instead.
            UsingNode("int.A");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void Qualified2()
        {
            UsingNode("A.B.C");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void Qualified3()
        {
            UsingNode("A{T}.B{U, V}.C");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void Qualified4()
        {
            UsingNode("Alias::B.C");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void Qualified5()
        {
            UsingNode("global::B.C");

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.GlobalKeyword);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.NameMemberCref);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        #endregion Qualified

        #region Aliased or Predefined

        [Fact]
        public void AliasedOrPredefined1()
        {
            UsingNode("string");

            N(SyntaxKind.TypeCref);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
            }
        }

        [Fact]
        public void AliasedOrPredefined2()
        {
            UsingNode("Alias::B");

            N(SyntaxKind.TypeCref);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void AliasedOrPredefined3()
        {
            UsingNode("global::B");

            N(SyntaxKind.TypeCref);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.GlobalKeyword);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void AliasedOrPredefined4()
        {
            UsingNode("global::global");

            N(SyntaxKind.TypeCref);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.GlobalKeyword);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        #endregion Aliased or Predefined

        #region Identifiers

        [Fact]
        public void EscapedKeyword()
        {
            UsingNode("@string");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
        }

        [Fact]
        public void EscapedUnicode()
        {
            UsingNode(@"\u0061"); // a

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
        }

        [Fact]
        public void UnescapedUnicode()
        {
            UsingNode("\u00CB"); // E with umlaut

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
        }

        [Fact]
        public void InvalidIdentifier()
        {
            UsingNode("2");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
        }

        #endregion Identifiers

        #region Non-simple-type constructors

        [Fact]
        public void PredefinedTypeConstructor()
        {
            UsingNode("string()");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Fact]
        public void AliasQualifiedTypeConstructor()
        {
            UsingNode("Alias::B()");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Fact]
        public void AliasQualifiedGenericTypeConstructor()
        {
            UsingNode("Alias::B{T}()");

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        #endregion Non-simple-type constructors
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CrefParsingTests : ParsingTests
    {
        public CrefParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            throw new NotSupportedException();
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options)
        {
            var commentText = string.Format(@"/// <see cref=""{0}""/>", text);
            var trivia = SyntaxFactory.ParseLeadingTrivia(commentText, options ?? CSharpParseOptions.Default).Single();
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
        public void UnqualifiedUnsignedRightShift_01()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingNode("operator >>>", options);

                N(SyntaxKind.OperatorMemberCref);
                {
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
                }
            }
        }

        [Fact]
        public void UnqualifiedUnsignedRightShift_02()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingNode("operator > >>", options);

                N(SyntaxKind.OperatorMemberCref);
                {
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.GreaterThanToken);
                }
                EOF();
            }
        }

        [Fact]
        public void UnqualifiedUnsignedRightShift_03()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingNode("operator >> >", options);

                N(SyntaxKind.OperatorMemberCref);
                {
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.GreaterThanGreaterThanToken);
                }
                EOF();
            }
        }

        [Fact]
        public void UnqualifiedUnsignedRightShift_04()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingNode("operator >>>=", options);

                N(SyntaxKind.OperatorMemberCref);
                {
                    N(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.PlusToken);
                }
                EOF();
            }
        }

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
        public void UnqualifiedOperatorMember1_Checked()
        {
            UsingNode("operator checked +");

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
                N(SyntaxKind.PlusToken);
            }
        }

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void UnqualifiedOperatorMember1_Unchecked()
        {
            UsingNode("operator unchecked +", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator unchecked +'
                // /// <see cref="operator unchecked +"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator unchecked +").WithArguments("operator unchecked +").WithLocation(1, 16),
                // (1,25): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="operator unchecked +"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 25));

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.PlusToken);
            }
            EOF();
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

        [Fact]
        public void UnqualifiedOperatorMember2_Checked()
        {
            UsingNode("operator checked +(A)");

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
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

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void UnqualifiedOperatorMember2_Unchecked()
        {
            UsingNode("operator unchecked +(A)", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator unchecked +(A)'
                // /// <see cref="operator unchecked +(A)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator unchecked +(A)").WithArguments("operator unchecked +(A)").WithLocation(1, 16),
                // (1,25): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="operator unchecked +(A)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 25));

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
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
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
        public void QualifiedOperatorMember1_Checked()
        {
            UsingNode("T.operator checked +");

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
                    N(SyntaxKind.CheckedKeyword);
                    N(SyntaxKind.PlusToken);
                }
            }
        }

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void QualifiedOperatorMember1_Unchecked()
        {
            UsingNode("T.operator unchecked +", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'T.operator unchecked +'
                // /// <see cref="T.operator unchecked +"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "T.operator unchecked +").WithArguments("T.operator unchecked +").WithLocation(1, 16),
                // (1,27): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="T.operator unchecked +"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 27)
                );

            N(SyntaxKind.QualifiedCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.OperatorMemberCref);
                {
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
                }
            }
            EOF();
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

        [Fact]
        public void QualifiedOperatorMember2_Checked()
        {
            UsingNode("T.operator checked +(A)");

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
                    N(SyntaxKind.CheckedKeyword);
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

        [Fact]
        public void GreaterThanGreaterThan_Checked()
        {
            UsingNode("operator checked }}(A{A{T}})");

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
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

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void GreaterThanGreaterThan_Unchecked()
        {
            UsingNode("operator unchecked }}(A{A{T}})", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator unchecked }}(A{A{T}})'
                // /// <see cref="operator unchecked }}(A{A{T}})"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator unchecked }}(A{A{T}})").WithArguments("operator unchecked }}(A{A{T}})").WithLocation(1, 16),
                // (1,25): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="operator unchecked }}(A{A{T}})"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 25)
                );

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.GreaterThanGreaterThanToken);
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
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
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [WorkItem(546992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546992")]
        [Fact]
        public void GreaterThanGreaterThanGreaterThan()
        {
            UsingNode("operator }}}(A{A{T}})").GetDiagnostics().Verify();

            N(SyntaxKind.OperatorMemberCref);
            {
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.GreaterThanGreaterThanGreaterThanToken); // >>>
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
        public void UnqualifiedConversionOperatorMember1_Checked()
        {
            UsingNode("implicit operator checked A");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
        }

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void UnqualifiedConversionOperatorMember1_Unchecked()
        {
            UsingNode("implicit operator unchecked A", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'implicit operator unchecked A'
                // /// <see cref="implicit operator unchecked A"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "implicit operator unchecked A").WithArguments("implicit operator unchecked A").WithLocation(1, 16),
                // (1,34): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="implicit operator unchecked A"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 34)
                );

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
            }
            EOF();
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

        [Fact]
        public void UnqualifiedConversionOperatorMember2_Checked()
        {
            UsingNode("explicit operator checked A(B)");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
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

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void UnqualifiedConversionOperatorMember2_Unchecked()
        {
            UsingNode("explicit operator unchecked A(B)", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'explicit operator unchecked A(B)'
                // /// <see cref="explicit operator unchecked A(B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "explicit operator unchecked A(B)").WithArguments("explicit operator unchecked A(B)").WithLocation(1, 16),
                // (1,34): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="explicit operator unchecked A(B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 34)
                );

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
        public void QualifiedConversionOperatorMember1_Checked()
        {
            UsingNode("T.implicit operator checked A");

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
                    N(SyntaxKind.CheckedKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void QualifiedConversionOperatorMember1_Unchecked()
        {
            UsingNode("T.implicit operator unchecked A", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'T.implicit operator unchecked A'
                // /// <see cref="T.implicit operator unchecked A"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "T.implicit operator unchecked A").WithArguments("T.implicit operator unchecked A").WithLocation(1, 16),
                // (1,36): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="T.implicit operator unchecked A"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 36)
                );

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

        [Fact]
        public void QualifiedConversionOperatorMember2_Checked()
        {
            UsingNode("T.explicit operator checked A(B)");

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
                    N(SyntaxKind.CheckedKeyword);
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

        [Fact, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        public void QualifiedConversionOperatorMember2_Unchecked()
        {
            UsingNode("T.explicit operator unchecked A(B)", TestOptions.RegularWithDocumentationComments,
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'T.explicit operator unchecked A(B)'
                // /// <see cref="T.explicit operator unchecked A(B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "T.explicit operator unchecked A(B)").WithArguments("T.explicit operator unchecked A(B)").WithLocation(1, 16),
                // (1,36): warning CS1658: Unexpected keyword 'unchecked'. See also error CS9027.
                // /// <see cref="T.explicit operator unchecked A(B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "unchecked").WithArguments("Unexpected keyword 'unchecked'", "9027").WithLocation(1, 36)
                );

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
        public void ParameterRefReadonly_01()
        {
            UsingNode("A(ref readonly B)");

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
                        N(SyntaxKind.ReadOnlyKeyword);
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
        public void ParameterRefReadonly_02()
        {
            UsingNode("A(ref readonly B, C)");

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
                        N(SyntaxKind.ReadOnlyKeyword);
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
        public void ParameterRefReadonly_03()
        {
            UsingNode("A(B, ref readonly C)");

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
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
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
        public void ParameterRefReadonly_04()
        {
            UsingNode("A(out B, ref readonly C)");

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
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }

        [Theory, CombinatorialData]
        public void ParameterRefReadonly_05(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingNode("A(readonly ref B)", TestOptions.Regular.WithLanguageVersion(languageVersion).WithDocumentationMode(DocumentationMode.Diagnose),
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'A(readonly ref B)'
                // /// <see cref="A(readonly ref B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A(").WithArguments("A(readonly ref B)").WithLocation(1, 16),
                // (1,18): warning CS1658: ) expected. See also error CS1026.
                // /// <see cref="A(readonly ref B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments(") expected", "1026").WithLocation(1, 18));

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Theory, CombinatorialData]
        public void ParameterRefReadonly_06(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingNode("A(readonly B)", TestOptions.Regular.WithLanguageVersion(languageVersion).WithDocumentationMode(DocumentationMode.Diagnose),
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'A(readonly B)'
                // /// <see cref="A(readonly B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A(").WithArguments("A(readonly B)").WithLocation(1, 16),
                // (1,18): warning CS1658: ) expected. See also error CS1026.
                // /// <see cref="A(readonly B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments(") expected", "1026").WithLocation(1, 18));

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Theory, CombinatorialData]
        public void ParameterRefReadonly_07(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingNode("A(in readonly B)", TestOptions.Regular.WithLanguageVersion(languageVersion).WithDocumentationMode(DocumentationMode.Diagnose),
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'A(in readonly B)'
                // /// <see cref="A(in readonly B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A(in readonly B)").WithArguments("A(in readonly B)").WithLocation(1, 16),
                // (1,21): warning CS1658: 'readonly' modifier must be specified after 'ref'.. See also error CS9190.
                // /// <see cref="A(in readonly B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments("'readonly' modifier must be specified after 'ref'.", "9190").WithLocation(1, 21));

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Theory, CombinatorialData]
        public void ParameterRefReadonly_08(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingNode("A(out readonly B)", TestOptions.Regular.WithLanguageVersion(languageVersion).WithDocumentationMode(DocumentationMode.Diagnose),
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'A(out readonly B)'
                // /// <see cref="A(out readonly B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A(out readonly B)").WithArguments("A(out readonly B)").WithLocation(1, 16),
                // (1,22): warning CS1658: 'readonly' modifier must be specified after 'ref'.. See also error CS9190.
                // /// <see cref="A(out readonly B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments("'readonly' modifier must be specified after 'ref'.", "9190").WithLocation(1, 22));

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Theory, CombinatorialData]
        public void ParameterRefReadonly_09(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingNode("A(ref readonly readonly B)", TestOptions.Regular.WithLanguageVersion(languageVersion).WithDocumentationMode(DocumentationMode.Diagnose),
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'A(ref readonly readonly B)'
                // /// <see cref="A(ref readonly readonly B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A(ref readonly").WithArguments("A(ref readonly readonly B)").WithLocation(1, 16),
                // (1,31): warning CS1658: Identifier expected; 'readonly' is a keyword. See also error CS1041.
                // /// <see cref="A(ref readonly readonly B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments("Identifier expected; 'readonly' is a keyword", "1041").WithLocation(1, 31),
                // (1,31): warning CS1658: ) expected. See also error CS1026.
                // /// <see cref="A(ref readonly readonly B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments(") expected", "1026").WithLocation(1, 31));

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CrefParameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Theory, CombinatorialData]
        public void ParameterRefReadonly_10(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingNode("A(readonly ref B)", TestOptions.Regular.WithLanguageVersion(languageVersion).WithDocumentationMode(DocumentationMode.Diagnose),
                // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'A(readonly ref B)'
                // /// <see cref="A(readonly ref B)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A(").WithArguments("A(readonly ref B)").WithLocation(1, 16),
                // (1,18): warning CS1658: ) expected. See also error CS1026.
                // /// <see cref="A(readonly ref B)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments(") expected", "1026").WithLocation(1, 18));

            N(SyntaxKind.NameMemberCref);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CrefParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
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

        [Fact]
        public void PrimitiveArrayReturnType_Checked()
        {
            UsingNode("explicit operator checked int[]");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
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

        [Fact]
        public void NamedTypeArrayReturnType_Checked()
        {
            UsingNode("explicit operator checked C[]");

            N(SyntaxKind.ConversionOperatorMemberCref);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
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

            M(SyntaxKind.NameMemberCref);
            {
                M(SyntaxKind.IdentifierName);
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

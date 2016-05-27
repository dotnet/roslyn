// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Adapted from AsyncParsingTests.
    [CompilerTrait(CompilerFeature.SourceGenerators)]
    public class MoreReplaceParsingTests : ParsingTests
    {
        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: (options ?? TestOptions.Regular).WithReplaceFeature());
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options)
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void MethodCalledReplace()
        {
            UsingTree(@"
class C
{
    void replace() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodReturningReplace()
        {
            UsingTree(@"
class C
{
    replace M() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodReplaceReplace()
        {
            UsingTree(@"
class C
{
    replace replace() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodReplaceReplaceReplace()
        {
            UsingTree(@"
class C
{
    replace replace replace() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.ReplaceKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void IncompleteReplace()
        {
            UsingTree(@"
class C
{
    replace
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void IncompleteReplaceReplace()
        {
            UsingTree(@"
class C
{
    replace replace
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void IncompleteReplaceReplaceReplace()
        {
            UsingTree(@"
class C
{
    replace replace replace
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.ReplaceKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteReplaceMember01()
        {
            // when parsing an incomplete member, treat 'replace' as a modifier if it makes sense
            UsingTree(@"
class C
{
    replace Task<
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.ReplaceKeyword);
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
                                M(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteReplaceMember02()
        {
            // when parsing an incomplete member, treat 'replace' as a modifier if it makes sense
            UsingTree(@"
class C
{
    replace Tasks.Task<
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.ReplaceKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
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
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteReplaceMember03()
        {
            // when parsing an incomplete member, treat 'replace' as a modifier if it makes sense
            UsingTree(@"
class C
{
    static replace Tasks.Task<
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.ReplaceKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
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
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteReplaceMember04()
        {
            // when parsing an incomplete member, treat 'replace' as a modifier if it makes sense
            // negative case
            UsingTree(@"
class C
{
    replace operator+
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken); // replace
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
                        M(SyntaxKind.ParameterList);
                        {
                            M(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteReplaceMember05()
        {
            // when parsing an incomplete member, treat 'replace' as a modifier if it makes sense
            // negative case
            UsingTree(@"
class C
{
    replace Task<T>
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.ReplaceKeyword);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteReplaceMember06()
        {
            // when parsing an incomplete member, treat 'replace' as a modifier if it makes sense
            // negative case
            UsingTree(@"
class C
{
    replace Task<T> f
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.ReplaceKeyword);
                        N(SyntaxKind.VariableDeclaration);
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
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void PropertyReplaceReplace()
        {
            UsingTree(@"
class C
{
    replace replace { get; set; }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void PropertyReplaceReplaceReplace()
        {
            UsingTree(@"
class C
{
    replace replace replace { get; set; }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.ReplaceKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void EventReplaceReplace()
        {
            UsingTree(@"
class C
{
    event replace replace;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void EventReplaceReplaceReplace1()
        {
            UsingTree(@"
class C
{
    event replace replace replace;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void EventReplaceReplaceReplace2()
        {
            UsingTree(@"
class C
{
    replace event replace replace;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.ReplaceKeyword);
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }
    }
}

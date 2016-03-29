// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    #region Local types for verification

    [Flags]
    public enum NodeStatus
    {
        None = 0,
        IsError = 1,
        IsWarning = 2,
        IsActive = 4,
        IsNotActive = 8, // used for #if etc.
        Unspecified = 8, // used for #def/und
        TrueValue = 16,
        Defined = 16, // used for #def/und
        FalseValue = 32,
        Undefined = 32, // used for #def/und
        BranchTaken = 64,
        NotBranchTaken = 128,
    }

    internal struct DirectiveInfo
    {
        public SyntaxKind Kind;
        public NodeStatus Status;
        public string Text;
        public int Number;
    }

    internal struct PragmaInfo
    {
        public SyntaxKind PragmaKind;
        public SyntaxKind WarningOrChecksumKind;
        public SyntaxKind DisableOrRestoreKind;
        public string[] WarningList;
        public string[] FileGuidByte;
    }

    internal struct MemberInfo
    {
        public SyntaxKind Kind;
        public NodeStatus Status;
        public NodeStatus Status2;
        public string Text;
    }

    #endregion

    public class PreprocessorTests
    {
        public PreprocessorTests()
        {
        }

        #region Helpers

        private CSharpParseOptions GetOptions(SourceCodeKind kind, string[] defines)
        {
            return new CSharpParseOptions(languageVersion: LanguageVersion.CSharp4, kind: kind, preprocessorSymbols: defines);
        }

        private CompilationUnitSyntax Parse(string text, params string[] defines)
        {
            return Parse(text, SourceCodeKind.Regular, defines);
        }

        private CompilationUnitSyntax Parse(string text, SourceCodeKind kind, params string[] defines)
        {
            var options = this.GetOptions(kind, defines);
            var itext = SourceText.From(text);
            return SyntaxFactory.ParseSyntaxTree(itext, options).GetCompilationUnitRoot();
        }

        private SyntaxTree ParseTree(string text, params string[] defines)
        {
            var options = this.GetOptions(SourceCodeKind.Regular, defines);
            var itext = SourceText.From(text);
            return SyntaxFactory.ParseSyntaxTree(itext, options);
        }

        private void TestRoundTripping(CompilationUnitSyntax node, string text, bool disallowErrors = true)
        {
            Assert.NotNull(node);
            var fullText = node.ToFullString();
            Assert.Equal(text, fullText);

            if (disallowErrors)
            {
                Assert.Empty(node.GetDiagnostics());
            }
            else
            {
                Assert.NotEmpty(node.GetDiagnostics());
            }
        }

        private void VerifyDirectives(CSharpSyntaxNode node, params SyntaxKind[] expected)
        {
            var directives = node.GetDirectives();
            Assert.Equal(expected.Length, directives.Count);
            if (expected.Length == 0)
            {
                return;
            }

            List<SyntaxKind> actual = new List<SyntaxKind>();
            foreach (var dt in directives)
            {
                actual.Add(dt.Kind());
            }

            int idx = 0;
            foreach (var ek in expected)
            {
                // Assert.True(actualKinds.Contains(kind)); // no order 
                Assert.Equal(ek, actual[idx++]); // exact order
            }
        }

        private void VerifyDirectivesSpecial(CSharpSyntaxNode node, params DirectiveInfo[] expected)
        {
            var directives = node.GetDirectives();
            Assert.Equal(expected.Length, directives.Count);

            List<SyntaxKind> actual = new List<SyntaxKind>();
            foreach (var dt in directives)
            {
                actual.Add(dt.Kind());
            }

            int idx = 0;
            foreach (var exp in expected)
            {
                Assert.Equal(exp.Kind, actual[idx]); // exact order

                // need to know what to expected here
                var dt = directives[idx++];

                if (NodeStatus.IsActive == (exp.Status & NodeStatus.IsActive))
                {
                    Assert.True(dt.IsActive);
                }
                else if (NodeStatus.IsNotActive == (exp.Status & NodeStatus.IsNotActive))
                {
                    Assert.False(dt.IsActive);
                }

                if (NodeStatus.BranchTaken == (exp.Status & NodeStatus.BranchTaken))
                {
                    Assert.True(((BranchingDirectiveTriviaSyntax)dt).BranchTaken);
                }
                else if (NodeStatus.NotBranchTaken == (exp.Status & NodeStatus.NotBranchTaken))
                {
                    Assert.False(((BranchingDirectiveTriviaSyntax)dt).BranchTaken);
                }

                if (NodeStatus.TrueValue == (exp.Status & NodeStatus.TrueValue))
                {
                    Assert.True(((ConditionalDirectiveTriviaSyntax)dt).ConditionValue);
                }
                else if (NodeStatus.FalseValue == (exp.Status & NodeStatus.FalseValue))
                {
                    Assert.False(((ConditionalDirectiveTriviaSyntax)dt).ConditionValue);
                }

                switch (exp.Kind)
                {
                    case SyntaxKind.DefineDirectiveTrivia:
                        if (null != exp.Text)
                        {
                            Assert.Equal(exp.Text, ((DefineDirectiveTriviaSyntax)dt).Name.ValueText); // Text
                        }

                        break;
                    case SyntaxKind.LineDirectiveTrivia:
                        var ld = dt as LineDirectiveTriviaSyntax;

                        // default number = 0 - no number
                        if (exp.Number == -1)
                        {
                            Assert.Equal(SyntaxKind.LineKeyword, ld.LineKeyword.Kind());
                            Assert.Equal(SyntaxKind.DefaultKeyword, ld.Line.Kind());
                        }
                        else if (exp.Number == -2)
                        {
                            Assert.Equal(SyntaxKind.LineKeyword, ld.LineKeyword.Kind());
                            Assert.Equal(SyntaxKind.HiddenKeyword, ld.Line.Kind());
                        }
                        else if (exp.Number == 0)
                        {
                            Assert.Equal(String.Empty, ld.Line.Text);
                        }
                        else if (exp.Number > 0)
                        {
                            Assert.Equal(exp.Number, ld.Line.Value); // Number
                            Assert.Equal(exp.Number, Int32.Parse(ld.Line.Text));
                        }

                        if (null == exp.Text)
                        {
                            Assert.Equal(SyntaxKind.None, ld.File.Kind());
                        }
                        else
                        {
                            Assert.NotEqual(SyntaxKind.None, ld.File.Kind());
                            Assert.Equal(exp.Text, ld.File.Value);
                        }

                        break;
                } // switch
            }
        }

        private void VerifyDirectivePragma(CSharpSyntaxNode node, PragmaInfo expected)
        {
            var directives = node.GetDirectives();
            Assert.Equal(1, directives.Count);
            var dt = directives[0];
            Assert.Equal(expected.PragmaKind, directives[0].Kind());

            if (dt is PragmaWarningDirectiveTriviaSyntax)
            {
                var pwd = (PragmaWarningDirectiveTriviaSyntax)dt;
                Assert.Equal(SyntaxKind.PragmaKeyword, pwd.PragmaKeyword.Kind());
                if (SyntaxKind.None == expected.WarningOrChecksumKind)
                {
                    Assert.True(pwd.WarningKeyword.IsMissing);
                }
                else
                {
                    Assert.Equal(SyntaxKind.WarningKeyword, pwd.WarningKeyword.Kind());
                }

                if (SyntaxKind.None == expected.DisableOrRestoreKind)
                {
                    Assert.True(pwd.DisableOrRestoreKeyword.IsMissing);
                }
                else
                {
                    Assert.Equal(expected.DisableOrRestoreKind, pwd.DisableOrRestoreKeyword.Kind());
                }

                if (expected.WarningList == null || expected.WarningList.Length == 0)
                {
                    Assert.Equal(0, pwd.ErrorCodes.Count);
                }
                else
                {
                    Assert.Equal(expected.WarningList.Length, pwd.ErrorCodes.Count);
                    int idx = 0;
                    foreach (var warningNumber in expected.WarningList)
                    {
                        var actualWarningNumber = pwd.ErrorCodes[idx++];
                        if (actualWarningNumber.Kind() == SyntaxKind.NumericLiteralExpression)
                        {
                            var token = (actualWarningNumber as LiteralExpressionSyntax).Token;
                            Assert.Equal(warningNumber, token.ValueText);
                        }
                        else if (actualWarningNumber.Kind() == SyntaxKind.IdentifierName)
                        {
                            var token = (actualWarningNumber as IdentifierNameSyntax).Identifier;
                            Assert.Equal(warningNumber, token.ValueText);
                        }
                        else
                        {
                            Assert.True(false, "Warning ID must be an identifier or numeric literal");
                        }
                    }
                }
            }
            else if (dt is PragmaChecksumDirectiveTriviaSyntax)
            {
                var pcd = (PragmaChecksumDirectiveTriviaSyntax)dt;
                Assert.Equal(SyntaxKind.PragmaKeyword, pcd.PragmaKeyword.Kind());
                Assert.Equal(SyntaxKind.ChecksumKeyword, pcd.ChecksumKeyword.Kind()); // no missing
                // always 3
                Assert.Equal(3, expected.FileGuidByte.Length);
                if (expected.FileGuidByte[0] == null)
                {
                    Assert.True(pcd.File.IsMissing);
                }

                Assert.Equal(expected.FileGuidByte[0], pcd.File.Value);
                if (expected.FileGuidByte[1] == null)
                {
                    Assert.True(pcd.Guid.IsMissing);
                }

                Assert.Equal(expected.FileGuidByte[1], pcd.Guid.Value);
                if (expected.FileGuidByte[2] == null)
                {
                    Assert.True(pcd.Bytes.IsMissing);
                }

                Assert.Equal(expected.FileGuidByte[2], pcd.Bytes.Value);
            }
            else
            {
                throw new NotImplementedException(dt.Kind().ToString());
            }
        }

        private void VerifyErrorCode(CSharpSyntaxNode node, params int[] expected)
        {
            var actual = node.ErrorsAndWarnings().Select(e => e.Code).ToList();

            // no error
            if ((expected.Length == 0) && (actual.Count == 0))
            {
                return;
            }

            // Parser might give more errors than expected & that's fine
            Assert.InRange(actual.Count, expected.Length, int.MaxValue);

            // necessary?
            if (actual.Count < expected.Length)
            {
                return;
            }

            foreach (int i in expected)
            {
                Assert.Contains(i, actual); // no order
            }
        }

        private void VerifyErrorSpecial(CSharpSyntaxNode node, DirectiveInfo expected)
        {
            var diags = node.ErrorsAndWarnings();
            Assert.Equal(1, diags.Length);
            var actual = diags[0];
            Assert.Equal(expected.Number, actual.Code);

            // warning or not
            if (NodeStatus.IsWarning == (expected.Status & NodeStatus.IsWarning))
            {
                Assert.Equal(DiagnosticSeverity.Warning, actual.Severity);
            }

            // error message
            if (expected.Text != null)
            {
                Assert.Equal(expected.Text, actual.GetMessage(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Not sure if this is good idea
        /// </summary>
        /// <param name="memberInfo"></param>
        private void VerifyMembers(CompilationUnitSyntax node, params MemberInfo[] memberInfo)
        {
            Assert.Equal(memberInfo.Length, node.Members.Count);
            var actual = node.Members;
            int idx = 0;
            foreach (var exp in memberInfo)
            {
                var mem = actual[idx++];
                Assert.Equal(exp.Kind, mem.Kind());

#if false
                var td = (TypeDeclarationSyntax)mem;
                // #define/undef
                if ((exp.Status & NodeStatus.Defined) == NodeStatus.Defined)
                {
                    Assert.Equal(DefineState.Defined, td.IsDefined(exp.Text));
                }
                else if ((exp.Status & NodeStatus.Undefined) == NodeStatus.Undefined)
                {
                    Assert.Equal(DefineState.Undefined, td.IsDefined(exp.Text));
                }
                else if ((exp.Status & NodeStatus.Unspecified) == NodeStatus.Unspecified)
                {
                    Assert.Equal(DefineState.Unspecified, td.IsDefined(exp.Text));
                }
                else
                {
                    Assert.Equal(exp.Text, td.Name.GetText());
                }
                // check cond-symbol in Member
                if ((exp.Status2 & NodeStatus.TrueValue) == NodeStatus.TrueValue)
                {
                    Assert.Equal(DefineState.Defined, td.Name.IsDefined(exp.Text));
                }
                else if ((exp.Status2 & NodeStatus.FalseValue) == NodeStatus.FalseValue)
                {
                    Assert.Equal(DefineState.Undefined, td.Name.IsDefined(exp.Text));
                }
                else if ((exp.Status2 & NodeStatus.IsNotActive) == NodeStatus.IsNotActive) // reuse same flag for different meaning:(
                {
                    Assert.Equal(DefineState.Unspecified, td.Name.IsDefined(exp.Text));
                }
#endif
            }
        }

        #endregion

        #region General

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegBadDirectiveName()
        {
            var text = @"#foo";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_PPDirectiveExpected);
            VerifyDirectives(node, SyntaxKind.BadDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegBadDirectiveNoName()
        {
            var text = @"#";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_PPDirectiveExpected);
            VerifyDirectives(node, SyntaxKind.BadDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDirectiveWithLeadingSpace()
        {
            var text = @"    #define XYZ";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDirectiveWithSpaceAfterHash()
        {
            var text = @"#   define XYZ";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDirectiveInsideMultilineComment()
        {
            var text = @"
/* 
#define XYZ
*/
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDirectiveAfterSingleLineComment()
        {
            var text = @"
// yada #define XYZ
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDirectiveInsideMultilineString()
        {
            var text = @"
class A
{
   string X = @""
#define XYZ
"";
}
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDirectiveInsideExcludedMultilineComment()
        {
            var text = @"
#if false
/*
#define XYZ
*/
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [WorkItem(906872, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressNegDirectiveInExcludedSingleLineComment()
        {
            var text = @"
#if false
// #define XYZ
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDirectiveInExcludedMultilineString()
        {
            var text = @"
#if false
class A
{
   string X = @""
#define XYZ
"";
}
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [WorkItem(906894, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressDirectiveInsideExcludedSingleLineString()
        {
            var text = @"
#if false
class A
{
   string X = ""#define XYZ"";
}
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        #endregion

        #region Conditional

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueEndif()
        {
            var text = @"
#if true
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseEndif()
        {
            var text = @"
#if false
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfNotTakenWithEndIfLeadingWhitespace()
        {
            var text =
@"#if DBG
  class A { }
       #endif";

            // whitespace preceding '#' is important for this test.
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfWithBadTokens()
        {
            var text =
@"#if true GARBAGE
#endif";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElseEndif()
        {
            var text = @"
#if true
  class A { }
#else
  class B { }
#endif
  class C { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "A" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElseEndif()
        {
            var text = @"
#if false
  class A { }
#else
  class B { }
#endif
  class C { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "B" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElifTrueEndif()
        {
            var text = @"
#if true
  class A { }
#elif true
  class B { }
#endif
  class C { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "A" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElifTrueEndif()
        {
            var text = @"
#if false
  class A { }
#elif true
  class B { }
#endif
  class C { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "B" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElifFalseEndif()
        {
            var text = @"
#if true
  class A { }
#elif false
  class B { }
#endif
  class C { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "A" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElifFalseEndif()
        {
            var text = @"
#if false
  class A { }
#elif false
  class B { }
#endif
  class C { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElifTrueElseEndif()
        {
            var text = @"
#if true
  class A { }
#elif true
  class B { }
#else 
  class C { }
#endif
  class D { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "A" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "D" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElifTrueElseEndif()
        {
            var text = @"
#if false
  class A { }
#elif true
  class B { }
#else 
  class C { }
#endif
  class D { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "B" }, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "D" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElifFalseElseEndif()
        {
            var text = @"
#if false
  class A { }
#elif false
  class B { }
#else 
  class C { }
#endif
  class D { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" }, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "D" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElifFalseElseEndif()
        {
            var text = @"
#if true
  class A { }
#elif false
  class B { }
#else 
  class C { }
#endif
  class D { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "A" }, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "D" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElifFalseElifTrueElseEndif()
        {
            var text = @"
#if true
  class A { }
#elif false
  class B { }
#elif true
  class C { }
#else
  class D { }
#endif
  class E { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "A" }, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "E" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElifFalseElifTrueElseEndif()
        {
            var text = @"
#if false
  class A { }
#elif false
  class B { }
#elif true
  class C { }
#else
  class D { }
#endif
  class E { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "C" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "E" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElifFalseElifFalseElseEndif()
        {
            var text = @"
#if false
  class A { }
#elif false
  class B { }
#elif false
  class C { }
#else
  class D { }
#endif
  class E { }
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "D" },
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "E" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueIfTrueEndIfEndIf()
        {
            var text = @"
#if true
#if true
#endif
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseIfTrueEndIfEndIf()
        {
            var text = @"
#if false
#if true
#endif
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueIfFalseEndIfEndIf()
        {
            var text = @"
#if true
#if false
#endif
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElseIfTrueEndIfEndIf()
        {
            var text = @"
#if true
#else
#if true
#endif
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueElifTrueIfTrueEndIfEndIf()
        {
            var text = @"
#if true
#elif true
#if true
#endif
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseElifTrueElseIfTrueEndIfEndIf()
        {
            var text = @"
#if false
#elif true
#else
#if true
#endif
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseIfFalseElseEndIfEndIf()
        {
            var text = @"
#if false
#if false
#endif
#if false
#else
#endif
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithNameDefined()
        {
            var text = @"
#if XYZ
#endif
";
            var node = Parse(text, "XYZ"); // define XYZ

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithNameUndefined()
        {
            var text = @"
#if XYZ
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithLogicalOr()
        {
            var text =
@"#if ABC || XYZ
#endif
";
            var node = Parse(text, "ABC", "XYZ");

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithLogicalOrWhenOnlyOneDefined()
        {
            var text =
@"#if ABC || XYZ
#endif
";
            var node = Parse(text, "ABC");

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithLogicalAnd()
        {
            var text =
@"#if ABC && XYZ
#endif
";
            var node = Parse(text, "ABC", "XYZ");

            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithLogicalAndWhenOnlyOneDefined()
        {
            var text =
@"#if ABC && XYZ
#endif
";
            var node = Parse(text, "ABC");
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithLogicalNotOnDefined()
        {
            var text =
@"#if !ABC
#endif
";
            var node = Parse(text, "ABC");
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithLogicalNotOnUndefined()
        {
            var text =
@"#if !ABC
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithParens()
        {
            var text =
@"#if (ABC)
#endif
";
            var node = Parse(text, "ABC");
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithEqualsAndBothDefined()
        {
            var text =
@"#if ABC == XYZ
#endif
";
            var node = Parse(text, "ABC", "XYZ");
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithEqualsAndBothNotDefined()
        {
            var text =
@"#if ABC == XYZ
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithEqualsAndOneDefined()
        {
            var text =
@"#if ABC == XYZ
#endif
";
            var node = Parse(text, "ABC");
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithNotEqualsAndBothDefined()
        {
            var text =
@"#if ABC != XYZ
#endif
";
            var node = Parse(text, "ABC", "XYZ");
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithNotEqualsAndBothUndefined()
        {
            var text =
@"#if ABC != XYZ
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfWithNotEqualsAndOneDefined()
        {
            var text =
@"#if ABC != XYZ
#endif
";
            var node = Parse(text, "ABC");
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [WorkItem(541898, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541898")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfNoEmptyTrivia()
        {
            var text = @"
#if YES
#pragma warning disable 0219
#pragma warning restore 0219
#elif NO
#pragma warning disable 0219
#pragma warning restore 0219
#else
#pragma warning disable 0219
#pragma warning restore 0219
#endif
";
            var node = Parse(text, "YES");
            TestRoundTripping(node, text);
            Assert.True(node.DescendantTrivia().All(trivia => trivia.Width > 0));
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfTrueWithNoEndif()
        {
            var text =
@"#if true
";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfFalseWithNoEndif()
        {
            var text =
@"#if false
";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfTrueWithEOF()
        {
            var text = @"#if true";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfFalseWithEOF()
        {
            var text = @"#if false";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfWithNoCondition()
        {
            var text =
@"#if 
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidPreprocExpr);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfTrueWithMissingParen()
        {
            var text =
@"#if (true
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_CloseParenExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfFalseWithMissingParen()
        {
            var text =
@"#if (false
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_CloseParenExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfTrueWithElifAfterElse()
        {
            var text =
@"#if true
#else
#elif true
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfWithElseAfterElse()
        {
            var text =
@"#if true
#else
#else
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfTrueEndRegionEndIf()
        {
            var text =
@"#if true
#endregion
#endif
";

            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsActive }, // ?
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegEndIfWithoutIf()
        {
            var text = @"#endif";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_UnexpectedDirective);
            VerifyDirectives(node, SyntaxKind.BadDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegElseWithoutIf()
        {
            var text = @"#else";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_UnexpectedDirective);
            VerifyDirectives(node, SyntaxKind.BadDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegElifWithoutIf()
        {
            var text = @"#elif";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_UnexpectedDirective);
            VerifyDirectives(node, SyntaxKind.BadDirectiveTrivia);
        }

        [WorkItem(542198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542198")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegElifWithoutIfWithSkipped()
        {
            var text = @"#elif ! defined X";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_UnexpectedDirective);
            VerifyDirectives(node, SyntaxKind.BadDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfTrueCodeElseCodeEndIf()
        {
            var text = @"
#if true
  class A { }
#else
  class B { }
#endif
";
            var node = Parse(text);

            TestRoundTripping(node, text);

            // TODO
            Assert.Equal(1, node.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, node.Members[0].Kind());
            var td = (TypeDeclarationSyntax)node.Members[0];
            Assert.Equal("A", td.Identifier.ToString());
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseCodeElseCodeEndif()
        {
            var text = @"
#if false
  class A { }
#else
  class B { }
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia);
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Text = "B" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfEndifDirectivesWithBadCode()
        {
            var text =
@"#if true
#else
#endif
aeu";

            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_NamespaceUnexpected);
            VerifyDirectives(node, SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfElifWithBoolExpression()
        {
            var text = @"#define True1
#define True2

#define False1
#undef False1
#undef False1
#undef False2

using System;

public class Test
{
    public static int Main(string [] args)
    {
		int i = 6;

 #if True1 == true
		i--;
 #endif

#if False1 == false
		i--;
#endif

# if false
	#error #elif True2 == True1
#elif True2 == True1
		i--;
#else
	#error #else #elif True2 == True1
 #endif

#if (True1 != false) && ((False1) == False2) && (true || false)
		i--;
# else
	#error #if (True != false) && ((False1) == False2) && (true || false)
# endif

#if ((true == True1) != (false && true))
		i--;
#else
#error ((true == True1) != (false && true))
#endif

#if !(!(!!(true))) != false
		i--;
#else
#error !(!(!!(true))) != false
#endif

		return(i > 0 ? 1 : 0);
    }
}";

            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.DefineDirectiveTrivia, SyntaxKind.DefineDirectiveTrivia, SyntaxKind.DefineDirectiveTrivia,
                SyntaxKind.UndefDirectiveTrivia, SyntaxKind.UndefDirectiveTrivia, SyntaxKind.UndefDirectiveTrivia,
                SyntaxKind.IfDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia, SyntaxKind.IfDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia,
                SyntaxKind.IfDirectiveTrivia, SyntaxKind.ErrorDirectiveTrivia, SyntaxKind.ElifDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ErrorDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia,
                SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ErrorDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia,
                SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ErrorDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia,
                SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ErrorDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia);
        }

        [WorkItem(921726, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfElifWithBadNumericalName()
        {
            var text = @"class A
{
#if 0
    int x = 0;
#elif 1
    int x = 1;
#else
    int x = -1;
#endif
}
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidPreprocExpr, (int)ErrorCode.ERR_InvalidPreprocExpr);
            VerifyDirectives(node, SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElifDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia);
        }

        [WorkItem(911446, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressIfNestedExcludedBody()
        {
            var text = @"using System;
public class Test
{
public static int Main()
{
int i = 0;
#if false
aaa
#if true
this shouldn't even be looked at.
#else
Except for counting up the #if/ #endif pairs
#endif
This should be skipped too!
#endif
return (i);
}
}
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.IfDirectiveTrivia, SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia);
        }

        [WorkItem(911464, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressIfFalseHashExcludedEndIf()
        {
            var text = @"
#if false
 // Bug#911464
#endif

#if false
 class X { void f() { R(""# s""); } }
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [WorkItem(913373, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressNestedIfWithUndefinedSymbols()
        {
            var text = @"class A
    {
#if AAA
        static void M()
        {
            int x = 0;
#if BBB
            x =1;
#endif
            x =2;
        }
#endif
}";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        #endregion

        #region #region/#endregion

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegionEndRegion()
        {
            var text =
@"#region
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegionRegionEndRegionEndRegion()
        {
            var text =
@"#region
#region
#endregion
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegionIfTrueEndIfEndRegion()
        {
            var text =
@"#region
#if true
#endif
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                    new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegRegionEndIfEndRegion()
        {
            var text =
@"#region
#endif
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndRegionDirectiveExpected);
            VerifyDirectivesSpecial(node,
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegRegionElifEndRegion()
        {
            var text =
@"#region
#elif
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndRegionDirectiveExpected);
            VerifyDirectives(node, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.BadDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegRegionElseEndRegion()
        {
            var text =
@"#region
#else
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndRegionDirectiveExpected);
            VerifyDirectives(node, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.BadDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfTrueRegionEndIf()
        {
            var text =
@"#if true
#region
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndRegionDirectiveExpected, (int)ErrorCode.ERR_EndRegionDirectiveExpected);
            VerifyDirectivesSpecial(node,
                    new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfTrueRegionEndIfEndRegion()
        {
            var text =
@"#if true
#region
#endif
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndRegionDirectiveExpected, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
                    new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsActive },
                    new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfFalseRegionEndIf()
        {
            var text =
@"#if false
#region
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndRegionDirectiveExpected, (int)ErrorCode.ERR_EndRegionDirectiveExpected);
            VerifyDirectivesSpecial(node,
                    new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                    new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsNotActive },
                    new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsNotActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfFalseRegionEndIfEndRegion()
        {
            var text =
@"#if false
#region
#endif
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndRegionDirectiveExpected, (int)ErrorCode.ERR_EndifDirectiveExpected);
            VerifyDirectivesSpecial(node,
        new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
        new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsNotActive },
        new DirectiveInfo { Kind = SyntaxKind.BadDirectiveTrivia, Status = NodeStatus.IsNotActive },
        new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsNotActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegionWithTrailingTokens()
        {
            var text =
@"#region -- Some Cool Region --
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [WorkItem(2958, "DevDiv_Projects/Roslyn")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegionWithSingleLineComment()
        {
            var text =
@"#region A//B
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });

            var regionDirective = (RegionDirectiveTriviaSyntax)node.GetFirstDirective();
            Assert.Equal($"#region A//B{Environment.NewLine}", regionDirective.ToFullString());
            var regionText = regionDirective.EndOfDirectiveToken.LeadingTrivia.Single();
            Assert.Equal(SyntaxKind.PreprocessingMessageTrivia, regionText.Kind());
            Assert.Equal("A//B", regionText.ToFullString());
        }

        [WorkItem(2958, "DevDiv_Projects/Roslyn")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegionWithInvalidSingleLineComment()
        {
            var text =
@"#region A/\B
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.RegionDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndRegionDirectiveTrivia, Status = NodeStatus.IsActive });

            var regionDirective = (RegionDirectiveTriviaSyntax)node.GetFirstDirective();
            Assert.Equal($"#region A/\\B{Environment.NewLine}", regionDirective.ToFullString());
            var regionText = regionDirective.EndOfDirectiveToken.LeadingTrivia.Single();
            Assert.Equal(SyntaxKind.PreprocessingMessageTrivia, regionText.Kind());
            Assert.Equal("A/\\B", regionText.ToFullString());
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegRegionWithMultilineComment()
        {
            var text = @"
class Test
{
#region /* This is a 
    multilines 
        comment */
#endregion 
}
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_SemicolonExpected); // CS1003 (Extra), CS1002
            VerifyDirectives(node, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia);
        }

        [WorkItem(906835, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressNegRegionWithInvalidEscapeString()
        {
            // Dev10 compiler gives errors CS1009
            var text = @"
#region \u01
#endregion
#region \U000A
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegionEndedWithEscapedNewline()
        {
            var text = @"
#region \u000D\u000A class A { }
#endregion
#region \U
#endregion
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia);
            VerifyMembers(node);
        }

        [WorkItem(527079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527079")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressRegionWithEscapeUnicodePrefixOnly()
        {
            // [Breaking Change] Dev10 compiler gives errors CS1009
            var text = @"#region \u
#endregion \U
";

            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia);
        }

        #endregion

        #region #define/#undefine

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDefineBeforeFirstToken()
        {
            var text = @"
#define XXX
class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            string defSym = "XXX";
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = defSym });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Status = NodeStatus.Unspecified, Status2 = NodeStatus.Defined, Text = defSym });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegDefineAfterFirstToken()
        {
            var text = @"
class A { }
#define XXX
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_PPDefFollowsToken); // CS1032
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "XXX" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegDefineAfterTokenOnSameLine()
        {
            var text = @"
class A { } #define XXX
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_BadDirectivePlacement); // CS1040
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "XXX" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDefineInIfBeforeFirstToken()
        {
            // defines after other directives are okay
            var text = @"
#if true
#define XXX
#endif
class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "XXX" },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDefineInIfExclusionAfterFirstToken()
        {
            // bad defines after first token in exlusion zone should not be errors
            var text = @"
class A { }
#if false
#define XXX
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "XXX" },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDefineInsideIfFalse()
        {
            var text = @"
#if false
#define ZZZ
#endif
  class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            string defSym = "ZZZ";
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = defSym },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Status = NodeStatus.Unspecified, Status2 = NodeStatus.Unspecified, Text = defSym });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDefineInsideIfTrue()
        {
            var text = @"
#if true
#define ZZZ
#endif
  class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            string defSym = "ZZZ";
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = defSym },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Status = NodeStatus.Unspecified, Text = defSym, Status2 = NodeStatus.Defined });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegDefineWithNoName()
        {
            var text = @"#define";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_IdentifierExpected); // CS1001
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = string.Empty });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegDefineWithBadTokenForName()
        {
            var text = @"# define  true
class A {}";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_IdentifierExpected); // CS1001
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = string.Empty });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegDefineWithBadTokensAfterName()
        {
            var text = @"#define FOO(";
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "FOO" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestUndefBeforeFirstToken()
        {
            var text = @"
#undef XXX
class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            string defSym = "XXX";
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = defSym });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Status = NodeStatus.Unspecified, Status2 = NodeStatus.Undefined, Text = defSym });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegUndefAfterFirstToken()
        {
            var text = @"
class A { }
#undef XXX
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_PPDefFollowsToken);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = "XXX" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegUndefAfterTokenOnSameLine()
        {
            var text = @"
class A { } #undef XXX
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_BadDirectivePlacement);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = "XXX" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestUndefInIfBeforeFirstToken()
        {
            // defines after other directives are okay
            var text = @"
#if true
#undef XXX
#endif
class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = "XXX" },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact, WorkItem(869243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869243")]
        [Trait("Feature", "Directives")]
        public void Bug869243_01()
        {
            var text = @"
#if false
    #define Z
#else
    #if Z
 ?1 // no errors here
    #endif
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "Z" },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact, WorkItem(869243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869243")]
        [Trait("Feature", "Directives")]
        public void Bug869243_02()
        {
            var text = @"
#if false
    #define A
#elif false
    #define B
#else
    #if A
 ?1 // no errors here
    #endif
    #if B
 ?2 // no errors here
    #endif
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "A" },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "B" },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact, WorkItem(869243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869243")]
        [Trait("Feature", "Directives")]
        public void Bug869243_03()
        {
            var text = @"
#if false
    #define Z
    #if true
        #define Z
    #endif
#else
    #if Z
 ?1 // no errors here
    #endif
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "Z" },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsNotActive | NodeStatus.NotBranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "Z" },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact, WorkItem(869243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869243")]
        [Trait("Feature", "Directives")]
        public void Bug869243_04()
        {
            var text = @"
#if false
    #define Z
#else
    #if false
        #define Z
    #elif true
        #if Z
     ?1 // no errors here
        #endif
    #endif
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "Z" },
                new DirectiveInfo { Kind = SyntaxKind.ElseDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "Z" },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestUndefInIfExclusionAfterFirstToken()
        {
            // bad defines after first token in exlusion zone should not be errors
            var text = @"
class A { }
#if false
#undef XXX
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "XXX" },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestUndefInsideIfFalse()
        {
            var text = @"
#if false
#undef ZZZ
#endif
  class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.NotBranchTaken | NodeStatus.FalseValue },
                new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsNotActive, Text = "ZZZ" },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node, new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Status = NodeStatus.Unspecified, Text = "ZZZ" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestUndefInsideIfTrue()
        {
            var text = @"
#if true
#undef ZZZ
#endif
  class A { }
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            string defSym = "ZZZ";
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive | NodeStatus.BranchTaken | NodeStatus.TrueValue },
                new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = defSym },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
            VerifyMembers(node,
                new MemberInfo { Kind = SyntaxKind.ClassDeclaration, Status = NodeStatus.Unspecified, Status2 = NodeStatus.Undefined, Text = defSym });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegUndefWithNoName()
        {
            var text = @"#undef";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_IdentifierExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = string.Empty });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegUndefWithBadTokenForName()
        {
            var text = @"#undef (";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_IdentifierExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = string.Empty });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegUndefWithBadTokensAfterName()
        {
            var text = @"#undef FOO(";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = "FOO(" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegDefWithBadCommentsTokensAfterName()
        {
            var text = @"#define A_1 /**
*
*/";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "A_1" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegUndefWithBadNumericalName()
        {
            var text = @"#undef 1234 /// <!@#$%^&*()_+-=>";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_IdentifierExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = "1234" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDefKeyword()
        {
            var text = @"#define error
#if error
#warning W1
#endif

#undef error
#if error
#warning W2
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_WarningDirective, Text = "#warning: 'W1'" });
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "error" },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive },

                new DirectiveInfo { Kind = SyntaxKind.UndefDirectiveTrivia, Status = NodeStatus.IsActive, Text = "error" },
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDefKeywordExhaustive()
        {
            var text = @"#define if
#define else
#define elif
#define endif
#define region
#define endregion
#define define
#define undef
#define warning
#define error
#define line
#define pragma
#define reference
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "if" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "else" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "elif" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "endif" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "region" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "endregion" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "define" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "undef" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "warning" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "error" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "line" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "pragma" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "reference" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegDefKeywordExhaustive()
        {
            var text = @"#define true
#define false
#define default
#define hidden
#define checksum
#define disable
#define restore
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node,
                (int)ErrorCode.ERR_IdentifierExpected,
                (int)ErrorCode.ERR_IdentifierExpected,
                (int)ErrorCode.ERR_IdentifierExpected,
                (int)ErrorCode.ERR_IdentifierExpected,
                (int)ErrorCode.ERR_IdentifierExpected,
                (int)ErrorCode.ERR_IdentifierExpected,
                (int)ErrorCode.ERR_IdentifierExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "" },
                new DirectiveInfo { Kind = SyntaxKind.DefineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "" });
        }

        #endregion

        #region #error/#warning

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestError()
        {
            var text = @"#error Your monkey is alive";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = "#error: 'Your monkey is alive'" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestWarning()
        {
            var text = @"#warning Your monkey is alive";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_WarningDirective, Text = "#warning: 'Your monkey is alive'" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseErrorEndIf()
        {
            var text = @"
#if false
#error Your monkey is alive
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfFalseWarningEndIf()
        {
            var text = @"
#if false
#warning Your monkey is alive
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfKeyword()
        {
            var text = @"
#if if
#warning W1
#elif elif
#warning W2
#elif endif
#warning W3
#elif !error
#warning W4
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text, disallowErrors: false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_WarningDirective, Text = "#warning: 'W4'" });
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsNotActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestIfKeywordExhaustive()
        {
            var text = @"
#if true
#elif false
#elif if
#elif else
#elif elif
#elif endif
#elif region
#elif endregion
#elif define
#elif undef
#elif warning
#elif error
#elif line
#elif pragma
#elif reference
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegIfKeywordExhaustive()
        {
            var text = @"
#if default
#elif hidden
#elif checksum
#elif disable
#elif restore
#endif
";
            var node = Parse(text);
            TestRoundTripping(node, text, disallowErrors: false);
            VerifyErrorCode(node,
                (int)ErrorCode.ERR_EndOfPPLineExpected,
                (int)ErrorCode.ERR_EndOfPPLineExpected,
                (int)ErrorCode.ERR_EndOfPPLineExpected,
                (int)ErrorCode.ERR_EndOfPPLineExpected,
                (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node,
                new DirectiveInfo { Kind = SyntaxKind.IfDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.ElifDirectiveTrivia, Status = NodeStatus.IsActive },
                new DirectiveInfo { Kind = SyntaxKind.EndIfDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestErrorWithNoMessage()
        {
            var text = @"#error";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = "#error: ''" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestWarningWithNoMessage()
        {
            var text = @"#warning";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_WarningDirective, Text = "#warning: ''" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.WarningDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestErrorWithLeadingWhitespace()
        {
            var text = @"#error  abc def";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = "#error: 'abc def'" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestErrorWithTrailingWhitespace()
        {
            var text = @"#error abc def ";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = "#error: 'abc def '" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestErrorWithSingleLineComment()
        {
            var text = @"#error  /*abc def*/ ";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = "#error: '/*abc def*/ '" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestErrorWithMultiLineComment()
        {
            var text = @"#error  /*abc def*/ ";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = "#error: '/*abc def*/ '" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestErrorWithUnterminatedMultiLineComment()
        {
            var text = @"#error  /*abc def ";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = "#error: '/*abc def '" });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [WorkItem(541954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541954")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestErrorWithNonLatinChars()
        {
            var errorText = "\u0444\u0430\u0439\u043B";
            var text = @"#error " + errorText;
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.ERR_ErrorDirective, Text = string.Format("#error: '{0}'", errorText) });
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ErrorDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [WorkItem(541953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541953")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestDiagnosticStringFileName()
        {
            CheckDiagnosticStringFileName("a.cs", "", "a.cs");
            CheckDiagnosticStringFileName("b\\a.cs", "", "b\\a.cs"); //path not trimmed
            CheckDiagnosticStringFileName("c:\\b\\a.cs", "", "c:\\b\\a.cs"); //path not trimmed
            CheckDiagnosticStringFileName("<file>", "", "<file>"); //invalid path characters - verbatim

            CheckDiagnosticStringFileName("z.cs", "#line 1 \"a.cs\"\r\n", "a.cs");
            CheckDiagnosticStringFileName("z.cs", "#line 1 \"b\\a.cs\"\r\n", "b\\a.cs"); //path not trimmed
            CheckDiagnosticStringFileName("z.cs", "#line 1 \"c:\\b\\a.cs\"\r\n", "c:\\b\\a.cs"); //path not trimmed
            CheckDiagnosticStringFileName("z.cs", "#line 1 \"<file>\"\r\n", "<file>"); //invalid path characters - verbatim
        }

        private void CheckDiagnosticStringFileName(string compilationFileName, string lineDirective, string expectedErrorStringFileName)
        {
            var text = lineDirective + "#error ERROR\r\n";
            var tree = SyntaxFactory.ParseSyntaxTree(text, path: compilationFileName);
            TestRoundTripping(tree.GetCompilationUnitRoot(), text, false);
            var error = tree.GetDiagnostics().Single();
            Assert.Equal((int)ErrorCode.ERR_ErrorDirective, error.Code);
            string errorString = error.ToString();
            string actualErrorStringFileName = errorString.Substring(0, errorString.IndexOf('('));
            Assert.Equal(expectedErrorStringFileName, actualErrorStringFileName);
        }

        #endregion

        #region #line

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLine()
        {
            var text = @"#line 1000";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 1000 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLineAndFile()
        {
            var text = @"#line 1000 ""bogus.cs""";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 1000, Text = "bogus.cs" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLineDefault()
        {
            var text = @"#line default";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = -1 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineDefaultWithFile()
        {
            var text = @"#line default ""bogus.cs""";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = -1 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLineDefaultWithComment()
        {
            var text = @"#line default // FOO";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = -1 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLineHidden()
        {
            var text = @"#line hidden";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = -2 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineHiddenWithFile()
        {
            var text = @"#line hidden ""bogus.cs""";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = -2 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLineHiddenWithComment()
        {
            var text = @"#line hidden // FOO";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = -2 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithBadNumber()
        {
            var text = @"#line Foo";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidLineNumber);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithNoNumberOrFile()
        {
            var text = @"#line";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidLineNumber);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithOnlyFile()
        {
            var text = @"#line ""bogus.cs""";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidLineNumber);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Text = "bogus.cs" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegMoreLineNumThenPDBCanHandle()
        {
            var text = @"#line 16707566 ""XYZ""";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_TooManyLinesForDebugger);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 16707566, Text = "XYZ" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineNumberInvalidZero()
        {
            var text = @"#line 0 ""XYZ""";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidLineNumber);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineNumberInvalidNegative()
        {
            var text = @"#line -1 ""XYZ""";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidLineNumber);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineNumberInvalidBiggerThanMaxInt()
        {
            var text = @"#line 2147483649 ""XYZ""";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidLineNumber);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithExtraTokens()
        {
            var text = @"#line 1000 XYZ";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_MissingPPFile);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 1000 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLineWithComment()
        {
            var text = @"#line 1000 // XYZ";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 1000 });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithFileAndExtraTokens()
        {
            var text = @"#line 1000 ""bogus.cs"" XYZ";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 1000, Text = "bogus.cs" });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestLineWithFileAndComment()
        {
            var text = @"#line 12345 ""bogus.cs"" // XYZ";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 12345, Text = "bogus.cs" });
        }

        [WorkItem(536699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536699")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressNegLineWithErrorMessage()
        {
            var text = @"class A
{
#line 100 ""test.cs""
x = 1;
}";
            var tree = ParseTree(text);
            TestRoundTripping(tree.GetCompilationUnitRoot(), text, false);

            // err TODO: check line number in error
            VerifyErrorCode(tree.GetCompilationUnitRoot(), (int)ErrorCode.ERR_InvalidMemberDecl); // CS1519 - parser gives Two
            VerifyDirectivesSpecial(tree.GetCompilationUnitRoot(), new DirectiveInfo { Kind = SyntaxKind.LineDirectiveTrivia, Status = NodeStatus.IsActive, Number = 100, Text = "test.cs" });

            var diagnostics = tree.GetDiagnostics();
            Assert.Contains("100", diagnostics.First().ToString(), StringComparison.Ordinal); // one-based line number

            var lineSpan = diagnostics.First().Location.GetMappedLineSpan();
            Assert.Equal(99, lineSpan.StartLinePosition.Line); // zero-based line number
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithTooLargeNumber()
        {
            var text = @"
public class Test
{
    # line 999999999999999999999
}
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_IntOverflow); // CS1021, CS1576 (not in parser) ErrorCode.ERR_InvalidLineNumber
            VerifyDirectives(node, SyntaxKind.LineDirectiveTrivia);
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithZero()
        {
            var text = @"
public class Test
{
    # line 0
}
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_InvalidLineNumber); // CS1021, CS1576 (not in parser) ErrorCode.ERR_InvalidLineNumber
            VerifyDirectives(node, SyntaxKind.LineDirectiveTrivia);
        }

        [WorkItem(537981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537981")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithoutSpaceBeforeFile()
        {
            var text = @"
public class Test
{
    # line 1""File""
}
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_MissingPPFile);
            VerifyDirectives(node, SyntaxKind.LineDirectiveTrivia);
        }

        [WorkItem(537981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537981")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegLineWithZeroWidthSpaceBeforeFile()
        {
            var text = @"
public class Test
{
    # line 1\u200B""File""
}
";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node); //zero width space is still a space
            VerifyDirectives(node, SyntaxKind.LineDirectiveTrivia);
        }

        [WorkItem(907010, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressLineWithVariousFileNameFormats()
        {
            var text = @"public class LineTests
{
	public static void Main() 
	{
		#line 10 ""ftp://test.cs""
		#line 20 ""ftps://test.cs""
		#line 30 ""http://test.cs""
		#line 40 ""https://test.cs""
		#line 50 ""gopher://test.cs""
		#line 60 ""telnet://test.cs""
		#line 70 ""dict://test.cs""
		#line 80 ""file://foo.aspx""
		#line 90 ""ldap://test.cs""
		#line 100 ""news://test.cs""
		#line 110 ""\\ddrelqa\logs\whidbey\2003-07-01\BVT64002\fx.Xml.XSLT\TESTPROCESSED20030701082505866.xml"" // parser error
		#line 120 ""C:\Documents and Settings\someuser\Local Settings\Temp\{f0a37341-d692-11d4-a984-009027ec0a9c}\test.cs"" // parser error
		#line 130 ""mailto://someuser@microsoft.com""
	}
}
";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia,
                SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia,
                SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia, SyntaxKind.LineDirectiveTrivia);
        }

        #endregion

        #region #pragma

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaWarningDisable()
        {
            var text = @"#pragma warning disable 114";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.DisableKeyword,
                WarningList = new[] { "114" }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaWarningDisableWithMultipleCodes()
        {
            var text = @"#pragma warning disable 114, CS0162, 168";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.DisableKeyword,
                WarningList = new[] { "114", "CS0162", "168" }
            });
        }

        [Fact, WorkItem(536701, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536701"), WorkItem(530051, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530051")]
        [Trait("Feature", "Directives")]
        public void TestRegressPragmaWarningDisableWithWarningCode()
        {
            var text = @"#pragma warning disable 440
using global = A; // CS0440
class A
{
static void Main() { }
}
";
            // verify that error still appears in GetDiagnostics
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var diagnostic = tree.GetDiagnostics().Single();
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal(440, diagnostic.Code);

            // verify pragma information
            var node = tree.GetCompilationUnitRoot();
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.DisableKeyword,
                WarningList = new[] { "440" }
            });

            // verify that GetParseDiagnostics filters disabled warning
            var comp = CSharpCompilation.Create("Test", syntaxTrees: new[] { tree });
            Assert.Empty(comp.GetParseDiagnostics());
        }

        [WorkItem(908125, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNoWarningForUnrecognizedCode()
        {
            var text = @"#pragma warning disable 99999";
            var node = Parse(text);

            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.DisableKeyword,
                WarningList = new[] { "99999" }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaWarningRestore()
        {
            var text = @"#pragma warning restore CS0114";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.RestoreKeyword,
                WarningList = new[] { "CS0114" }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaWarningRestoreWithMultipleCodes()
        {
            var text = @"#pragma warning restore CS0114, 162, Something // Multiple codes";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.RestoreKeyword,
                WarningList = new[] { "CS0114", "162", "Something" }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestStringLiteralsAreDisallowed()
        {
            var text = @"#pragma warning restore ""CS0114"", 162, ""CS0168"" // Mixed string & numeric codes";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_IdentifierOrNumericLiteralExpected, Status = NodeStatus.IsWarning }); // CS1072
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.RestoreKeyword,
                WarningList = new[] { string.Empty }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaWarningWithBadStyle()
        {
            var text = @"#pragma warning FOO";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_IllegalPPWarning, Status = NodeStatus.IsWarning }); // CS1634
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.None,
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaWarningWithBadStyleAndCodes()
        {
            var text = @"#pragma warning FOO 114";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_IllegalPPWarning, Status = NodeStatus.IsWarning }); // CS1634
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.None
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaWarningWithNoStyle()
        {
            var text = @"#pragma warning 114";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorSpecial(node, new DirectiveInfo { Number = (int)ErrorCode.WRN_IllegalPPWarning, Status = NodeStatus.IsWarning }); // CS1634
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.None
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaWarningDisableWithNoCodes()
        {
            var text = @"#pragma warning disable";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.DisableKeyword
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaWarningRestoreWithNoCodes()
        {
            var text = @"#pragma warning restore";
            var node = Parse(text);
            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.WarningKeyword,
                DisableOrRestoreKind = SyntaxKind.RestoreKeyword
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaWithoutWarningOrChecksum()
        {
            var text = @"#pragma";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPragma); // CS1633
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.None,
                DisableOrRestoreKind = SyntaxKind.None
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaWithBadToken()
        {
            var text = @"#pragma FOO";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPragma);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaWarningDirectiveTrivia,
                WarningOrChecksumKind = SyntaxKind.None,
                DisableOrRestoreKind = SyntaxKind.None
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaChecksum()
        {
            var file = "bogus.cs";
            var guid = "{" + Guid.Empty + "}";
            var bytes = "ab007f1d23d9";
            var text = string.Format(@"#pragma checksum ""{0}"" ""{1}"" ""{2}""", file, guid, bytes);
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, guid, bytes }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaChecksumWithNoBytes()
        {
            var file = "bogus.cs";
            var guid = "{" + Guid.Empty + "}";
            var text = string.Format(@"#pragma checksum ""{0}"" ""{1}""", file, guid);
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPPChecksum); // CS1695
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, guid, null }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaChecksumWithNoGuidAndNoBytes()
        {
            var file = "bogus.cs";
            var text = string.Format(@"#pragma checksum ""{0}""", file);
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPPChecksum);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, null, null }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaChecksumWithNothingElse()
        {
            var text = @"#pragma checksum";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPPChecksum);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { null, null, null }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestPragmaChecksumWithNoBracesOnGuid()
        {
            var file = "bogus.cs";
            var guid = Guid.Empty.ToString();
            var bytes = "ab007f1d23d9";
            var text = string.Format(@"#pragma checksum ""{0}"" ""{1}"" ""{2}""", file, guid, bytes);
            var node = Parse(text);

            TestRoundTripping(node, text);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, guid, bytes }
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegPragmaChecksumWithBadGuid()
        {
            var file = "bogus.cs";
            var guid = "{abc-123}";
            var bytes = "ab007f1d23d9";
            var text = string.Format(@"#pragma checksum ""{0}"" ""{1}"" ""{2}""", file, guid, bytes);
            var node = Parse(text);

            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPPChecksum);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, guid, bytes }
            });
        }

        [WorkItem(909445, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressNegPragmaChecksumWithMLineComment()
        {
            var file = "test.cs";
            var guid = "{406EA660-64CF-4C82-B6F0-42D48172A799}";
            var bytes = string.Empty;
            var text = string.Format(@"#pragma checksum ""{0}"" ""{1}"" ""{2}"" /* Test Comment */", file, guid, bytes);

            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_EndOfPPLineExpected); // CS1696
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, guid, bytes }
            });
        }

        [WorkItem(909445, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressNegPragmaChecksumWithComma()
        {
            var file = "test.cs";
            var guid = "{406EA660-64CF-4C82-B6F0-42D48172A799}";
            var bytes = string.Empty;
            var text = string.Format(@"#pragma checksum ""{0}"", ""{1}"" ""{2}"" ", file, guid, bytes);

            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPPChecksum);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, null/*guid*/, null/*bytes*/ }
            });
        }

        [WorkItem(922889, "DevDiv/Personal")]
        [Fact]
        [Trait("Feature", "Directives")]
        public void TestRegressNegPragmaChecksumWithBadBytesInMethod()
        {
            var file = "test.cs";
            var guid = "{406EA660-64CF-4C82-B6F0-42D48172A799}";
            var bytes = "A";
            var text = string.Format(@"class Test {{
    static int Main() 
    {{
#pragma checksum ""{0}"" ""{1}"" ""{2}"" 
 return 0;
    }} }}", file, guid, bytes);

            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.WRN_IllegalPPChecksum);
            VerifyDirectivePragma(node, new PragmaInfo
            {
                PragmaKind = SyntaxKind.PragmaChecksumDirectiveTrivia,
                FileGuidByte = new string[] { file, guid, bytes }
            });
        }

        #endregion

        #region #r

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestReference()
        {
            var text = @"#r ""bogus""";
            var node = Parse(text, SourceCodeKind.Script);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo
            {
                Kind = SyntaxKind.ReferenceDirectiveTrivia,
                Status = NodeStatus.IsActive,
                Text = "bogus"
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestReferenceWithComment()
        {
            var text = @"#r ""bogus"" // FOO";
            var node = Parse(text, SourceCodeKind.Script);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo
            {
                Kind = SyntaxKind.ReferenceDirectiveTrivia,
                Status = NodeStatus.IsActive,
                Text = "bogus"
            });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestReferenceNumber()
        {
            var text = @"#r 123";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_ExpectedPPFile);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ReferenceDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestReferenceWithoutQuotes()
        {
            var text = @"#r Foo";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_ExpectedPPFile);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ReferenceDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestReferenceWithoutFileWithComment()
        {
            var text = @"#r //comment";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_ExpectedPPFile);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ReferenceDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestNegReferenceWithoutFile()
        {
            var text = @"#r";
            var node = Parse(text);
            TestRoundTripping(node, text, false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_ExpectedPPFile);
            VerifyDirectivesSpecial(node, new DirectiveInfo { Kind = SyntaxKind.ReferenceDirectiveTrivia, Status = NodeStatus.IsActive });
        }

        [Fact]
        [Trait("Feature", "Directives")]
        public void TestReferenceWithVariousFileNameFormats()
        {
            var text = @"
#r ""ftp://test.cs""
#r ""ftps://test.cs""
#r ""http://test.cs""
#r ""https://test.cs""
#r ""gopher://test.cs""
#r ""telnet://test.cs""
#r ""dict://test.cs""
#r ""file://foo.aspx""
#r ""ldap://test.cs""
#r ""news://test.cs""
#r ""\\ddrelqa\logs\whidbey\2003-07-01\BVT64002\fx.Xml.XSLT\TESTPROCESSED20030701082505866.xml"" // comment
#r ""C:\Documents and Settings\someuser\Local Settings\Temp\{f0a37341-d692-11d4-a984-009027ec0a9c}\test.cs"" // comment
#r ""mailto://someuser@microsoft.com""
";
            var node = Parse(text, SourceCodeKind.Script);
            TestRoundTripping(node, text);
            VerifyDirectives(node, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia,
                SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia,
                SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia);
        }

        #endregion

        #region #load

        [Fact]
        public void TestLoad()
        {
            var text = "#load \"bogus\"";
            var node = Parse(text, SourceCodeKind.Script);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo
            {
                Kind = SyntaxKind.LoadDirectiveTrivia,
                Status = NodeStatus.IsActive,
                Text = "bogus"
            });
        }

        [Fact]
        public void TestLoadWithoutFile()
        {
            var text = "#load";
            var node = Parse(text, SourceCodeKind.Script);
            TestRoundTripping(node, text, disallowErrors: false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_ExpectedPPFile);
            VerifyDirectivesSpecial(node, new DirectiveInfo
            {
                Kind = SyntaxKind.LoadDirectiveTrivia,
                Status = NodeStatus.IsActive,
            });
            Assert.True(node.GetLoadDirectives().Single().File.IsMissing);
        }

        [Fact]
        public void TestLoadWithSemicolon()
        {
            var text = "#load \"\";";
            var node = Parse(text, SourceCodeKind.Script);
            TestRoundTripping(node, text, disallowErrors: false);
            VerifyErrorCode(node, (int)ErrorCode.ERR_EndOfPPLineExpected);
            VerifyDirectivesSpecial(node, new DirectiveInfo
            {
                Kind = SyntaxKind.LoadDirectiveTrivia,
                Status = NodeStatus.IsActive,
                Text = ""
            });
        }

        [Fact]
        public void TestLoadWithComment()
        {
            var text = "#load \"bogus\" // comment";
            var node = Parse(text, SourceCodeKind.Script);
            TestRoundTripping(node, text);
            VerifyDirectivesSpecial(node, new DirectiveInfo
            {
                Kind = SyntaxKind.LoadDirectiveTrivia,
                Status = NodeStatus.IsActive,
                Text = "bogus"
            });
        }

        #endregion
    }
}

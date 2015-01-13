// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class NodeRules
    {
        [NonTerminalRuleAttribute(Name = "ParentSpanForNonTerminal", Group = "Span")]
        public bool ParentSpanForNonTerminal(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (nonTerminal.Parent != null)
            {
                if (nonTerminal.SpanStart < nonTerminal.Parent.SpanStart || nonTerminal.Span.End > nonTerminal.Parent.Span.End)
                {
                    retVal = false;
                    errorText = "Span of this non-terminal is not within Span of its parent";
                }
                else if (nonTerminal.FullSpan.Start < nonTerminal.Parent.FullSpan.Start || nonTerminal.FullSpan.End > nonTerminal.Parent.FullSpan.End)
                {
                    retVal = false;
                    errorText = "FullSpan of this non-terminal is not within FullSpan of its parent";
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "ParentSpanForToken", Group = "Span")]
        public bool ParentSpanForToken(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.SpanStart < token.Parent.SpanStart || token.Span.End > token.Parent.Span.End)
            {
                retVal = false;
                errorText = "Span of this token is not within Span of its parent";
            }
            else if (token.FullSpan.Start < token.Parent.FullSpan.Start || token.FullSpan.End > token.Parent.FullSpan.End)
            {
                retVal = false;
                errorText = "FullSpan of this token is not within FullSpan of its parent";
            }

            return retVal;
        }

        [NonTerminalRuleAttribute(Name = "FirstChildSpan", Group = "Span")]
        public bool FirstChildSpan(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var firstChild = nonTerminal.ChildNodesAndTokens().First();
            if (nonTerminal.SpanStart != firstChild.SpanStart)
            {
                retVal = false;
                errorText = "Start Span of first child of this non-terminal does not coincide with start Span of this non-terminal";
            }
            else if (nonTerminal.FullSpan.Start != firstChild.FullSpan.Start)
            {
                retVal = false;
                errorText = "Start FullSpan of first child of this non-terminal does not coincide with start FullSpan of this non-terminal";
            }

            return retVal;
        }

        [NonTerminalRuleAttribute(Name = "LastChildSpan", Group = "Span")]
        public bool LastChildSpan(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var lastChild = nonTerminal.ChildNodesAndTokens().Last();
            if (nonTerminal.Span.End != lastChild.Span.End)
            {
                retVal = false;
                errorText = "End Span of last child of this non-terminal does not coincide with end Span of this non-terminal";
            }
            else if (nonTerminal.FullSpan.End != lastChild.FullSpan.End)
            {
                retVal = false;
                errorText = "End FullSpan of last child of this non-terminal does not coincide with end FullSpan of this non-terminal";
            }

            return retVal;
        }

        [NonTerminalRuleAttribute(Name = "ChildSpanWidth", Group = "Span")]
        public bool ChildSpanWidth(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var total = 0;
            foreach (var child in nonTerminal.ChildNodesAndTokens())
            {
                total = child.FullSpan.Length;
            }

            if (nonTerminal.FullSpan.Length != total)
            {
                retVal = false;
                errorText = "FullSpan width of this non-terminal (" + nonTerminal.FullSpan.Length + ") does not match sum of FullSpan widths of its children (" + total + ")";
            }

            return retVal;
        }

        [NonTerminalRuleAttribute(Name = "ChildSiblingSpan", Group = "Span")]
        public bool ChildSiblingSpan(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var curStart = -1;
            var prvEnd = -1;
            foreach (var child in nonTerminal.ChildNodesAndTokens())
            {
                curStart = child.SpanStart;
                if (prvEnd == -1)
                {
                    prvEnd = curStart;
                }

                if (curStart < prvEnd)
                {
                    retVal = false;
                    errorText = "Start Span of a child of this non-terminal is less than the end Span of this child's preceding sibling";
                    continue;
                }

                prvEnd = child.Span.End;
            }

            if (retVal)
            {
                curStart = -1;
                prvEnd = -1;
                foreach (var child in nonTerminal.ChildNodesAndTokens())
                {
                    curStart = child.FullSpan.Start;
                    if (prvEnd == -1)
                    {
                        prvEnd = curStart;
                    }

                    if (curStart != prvEnd)
                    {
                        retVal = false;
                        errorText = "FullSpan of a child of this non-terminal is not adjacent to the FullSpan of this child's preceding sibling";
                        continue;
                    }

                    prvEnd = child.FullSpan.End;
                }
            }

            return retVal;
        }

        [TriviaRuleAttribute(Name = "TriviaSpan", Group = "Span")]
        public bool TriviaSpan(SyntaxTrivia trivia, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (!((trivia.SpanStart <= trivia.Token.SpanStart || trivia.SpanStart >= trivia.Token.Span.End) && (trivia.Span.End >= trivia.Token.Span.End || trivia.Span.End <= trivia.Token.SpanStart)))
            {
                retVal = false;
                errorText = "Span of this trivia is within Span of its parent token";
            }
            else if (!((trivia.FullSpan.Start <= trivia.Token.SpanStart || trivia.FullSpan.Start >= trivia.Token.Span.End) && (trivia.FullSpan.End >= trivia.Token.Span.End || trivia.FullSpan.End <= trivia.Token.SpanStart)))
            {
                retVal = false;
                errorText = "FullSpan of this trivia is within Span of its parent token";
            }
            else if (trivia.SpanStart < trivia.Token.FullSpan.Start || trivia.Span.End > trivia.Token.FullSpan.End)
            {
                retVal = false;
                errorText = "Span of this trivia is not within FullSpan of its parent token";
            }
            else if (trivia.FullSpan.Start < trivia.Token.FullSpan.Start || trivia.FullSpan.End > trivia.Token.FullSpan.End)
            {
                retVal = false;
                errorText = "FullSpan of this trivia is not within FullSpan of its parent token";
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "FirstLeadingTriviaSpan", Group = "Span")]
        public bool FirstLeadingTriviaSpan(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.LeadingTrivia.Any())
            {
                var firstLeadingTrivia = token.LeadingTrivia.First();
                if (token.FullSpan.Start != firstLeadingTrivia.FullSpan.Start)
                {
                    retVal = false;
                    errorText = "Start FullSpan of first leading trivia of this token does not coincide with start FullSpan of this token";
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "LastTrailingTriviaSpan", Group = "Span")]
        public bool LastTrailingTriviaSpan(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.TrailingTrivia.Any())
            {
                var lastTrailingTrivia = token.TrailingTrivia.Last();
                if (token.FullSpan.End != lastTrailingTrivia.FullSpan.End)
                {
                    retVal = false;
                    errorText = "End FullSpan of last trailing trivia of this token does not coincide with end FullSpan of this token";
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "LastLeadingTriviaSpan", Group = "Span")]
        public bool LastLeadingTriviaSpan(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.LeadingTrivia.Any())
            {
                var lastLeadingTrivia = token.LeadingTrivia.Last();
                if (!lastLeadingTrivia.HasStructure)
                {
                    //Example where structured trivia can violate this rule -
                    //Leading #end region trivia on the } token has last child with trailing EndOfLineTrivia.
                    //namespace EVTInterop002_Server
                    //{
                    //#region region
                    //#endregion
                    //}
                    if (token.SpanStart != lastLeadingTrivia.Span.End)
                    {
                        retVal = false;
                        errorText = "End Span of last leading trivia of this token does not coincide with start Span of this token";
                    }
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "FirstTrailingTriviaSpan", Group = "Span")]
        public bool FirstTrailingTriviaSpan(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.TrailingTrivia.Any())
            {
                var firstTrailingTrivia = token.TrailingTrivia.First();
                if (!firstTrailingTrivia.HasStructure)
                {
                    //Example where structured trivia can violate this rule -
                    //Trailing /** (multiline xml doc comment) trivia on the } token has children with their own leading DocumentationCommentExteriorTrivia.
                    //class c1
                    //{
                    //}/** 
                    //*
                    //**/
                    if (token.Span.End != firstTrailingTrivia.SpanStart)
                    {
                        retVal = false;
                        errorText = "Start Span of first trailing trivia of this token does not coincide with end Span of this token";
                    }
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "TriviaSpanWidth", Group = "Span")]
        public bool TriviaSpanWidth(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.LeadingTrivia.Any() || token.TrailingTrivia.Any())
            {
                var total = 0;
                foreach (var leadingTrivia in token.LeadingTrivia)
                {
                    total = leadingTrivia.FullSpan.Length;
                }

                foreach (var trailingTrivia in token.TrailingTrivia)
                {
                    total = trailingTrivia.FullSpan.Length;
                }

                if (token.FullSpan.Length != (total + token.Span.Length))
                {
                    retVal = false;
                    errorText = "FullSpan width of this token (" + token.FullSpan.Length + ") does not match Span width of this token (" + token.Span.Length + ") + sum of FullSpan widths of all leading and trailing trivia of this token (" + total + ")";
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "TriviaSiblingSpan", Group = "Span")]
        public bool TriviaSiblingSpan(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var curStart = -1;
            var prvEnd = -1;
            foreach (var leadingTrivia in token.LeadingTrivia)
            {
                curStart = leadingTrivia.FullSpan.Start;
                if (prvEnd == -1)
                {
                    prvEnd = curStart;
                }

                if (curStart != prvEnd)
                {
                    retVal = false;
                    errorText = "FullSpan of a leading trivia of this token is not adjacent to the FullSpan of this leading trivia's preceding sibling";
                    continue;
                }

                prvEnd = leadingTrivia.FullSpan.End;
            }

            if (retVal)
            {
                curStart = -1;
                prvEnd = -1;
                foreach (var trailingTrivia in token.TrailingTrivia)
                {
                    curStart = trailingTrivia.FullSpan.Start;
                    if (prvEnd == -1)
                    {
                        prvEnd = curStart;
                    }

                    if (curStart != prvEnd)
                    {
                        retVal = false;
                        errorText = "FullSpan of a trailing trivia of this token is not adjacent to the FullSpan of this trailing trivia's preceding sibling";
                        continue;
                    }

                    prvEnd = trailingTrivia.FullSpan.End;
                }
            }

            return retVal;
        }

        [NonTerminalRuleAttribute(Name = "SpanAndFullSpanForNonTerminal", Group = "Span")]
        public bool SpanAndFullSpanForNonTerminal(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (nonTerminal.ChildNodesAndTokens().Any())
            {
                if (nonTerminal.FullSpan.Start > nonTerminal.SpanStart || nonTerminal.Span.End > nonTerminal.FullSpan.End)
                {
                    retVal = false;
                    errorText = "FullSpan of this non-terminal does not enclose its Span";
                }
            }
            else
            {
                retVal = false;
                errorText = "A non-terminal must have at least one child";
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "SpanAndFullSpanForToken", Group = "Span")]
        public bool SpanAndFullSpanForToken(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.LeadingTrivia.Any() || token.TrailingTrivia.Any())
            {
                if (token.FullSpan.Start > token.SpanStart || token.Span.End > token.FullSpan.End)
                {
                    retVal = false;
                    errorText = "FullSpan of this token does not enclose Span of this token";
                }
            }
            else
            {
                if (token.Span != token.FullSpan)
                {
                    retVal = false;
                    errorText = "Span and FullSpan for this token do not match - but they should because this token has no leading or trailing trivia";
                }
            }

            return retVal;
        }

        [TriviaRuleAttribute(Name = "SpanAndFullSpanForTrivia", Group = "Span")]
        public bool SpanAndFullSpanForTrivia(SyntaxTrivia trivia, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (trivia.HasStructure)
            {
                if (trivia.FullSpan.Start > trivia.SpanStart || trivia.Span.End > trivia.FullSpan.End)
                {
                    retVal = false;
                    errorText = "FullSpan of this structured trivia does not enclose Span of this trivia";
                }
            }
            else
            {
                if (trivia.Span != trivia.FullSpan)
                {
                    retVal = false;
                    errorText = "Span and FullSpan for this non-structured trivia do not match";
                }
            }

            return retVal;
        }

        /* #End Region
     */
        /* #Region "Structure"
     */
        [NonTerminalRuleAttribute(Name = "ParentForNonTerminal", Group = "Structure")]
        public bool ParentForNonTerminal(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (nonTerminal.Parent == null)
            {
                if (!nonTerminal.IsStructuredTrivia && nonTerminal.GetKind() != "CompilationUnit")
                {
                    retVal = false;
                    errorText = "This non-terminal has a null parent";
                }
            }
            else
            {
                if (nonTerminal.IsStructuredTrivia)
                {
                    retVal = false;
                    errorText = "This structured trivia has a non-null parent";
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "ParentForToken", Group = "Structure")]
        public bool ParentForToken(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.Parent == null)
            {
                retVal = false;
                errorText = "This token has a null parent";
            }

            return retVal;
        }

        [TriviaRuleAttribute(Name = "TriviaStructure", Group = "Structure")]
        public bool TriviaStructure(SyntaxTrivia trivia, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (trivia.HasStructure)
            {
                if (trivia.GetStructure() == null)
                {
                    retVal = false;
                    errorText = "This structured trivia has null structure";
                }
                else if (!trivia.GetStructure().IsStructuredTrivia)
                {
                    retVal = false;
                    errorText = "This structured trivia's structure has IsStructuredTrivia set to 'False'";
                }
            }
            else if (!trivia.HasStructure & trivia.GetStructure() != null)
            {
                retVal = false;
                errorText = "This non-structured trivia has non-null structure";
            }

            return retVal;
        }

        [NonTerminalRuleAttribute(Name = "ParentAndChildrenForNonTerminal", Group = "Structure")]
        public bool ParentAndChildrenForNonTerminal(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            foreach (var child in nonTerminal.ChildNodesAndTokens())
            {
                if (child.Parent != nonTerminal)
                {
                    retVal = false;
                    errorText = "A child of this non-terminal does not have this non-terminal set as its parent";
                    continue;
                }
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "ParentAndChildrenForToken", Group = "Structure")]
        public bool ParentAndChildrenForToken(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            foreach (var leadingTrivia in token.LeadingTrivia)
            {
                if (leadingTrivia.Token != token)
                {
                    retVal = false;
                    errorText = "A leading trivia of this token does not have this token set as its parent";
                    continue;
                }
            }

            if (retVal)
            {
                foreach (var trailingTrivia in token.TrailingTrivia)
                {
                    if (trailingTrivia.Token != token)
                    {
                        retVal = false;
                        errorText = "A trailing trivia of this token does not have this token set as its parent";
                        continue;
                    }
                }
            }

            return retVal;
        }

        /* #End Region
     */
        /* #Region "Misc"
     */
        //SEE BUG 867280
        [NonTerminalRuleAttribute(Name = "ZeroWidthOrMissingNonTerminals", Group = "Misc")]
        public bool ZeroWidthOrMissingNonTerminals(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (nonTerminal.ContainsDiagnostics || tree.GetDiagnostics().Any())
            {
                if (nonTerminal.IsMissing)
                {
                    if (nonTerminal.Span.Length == 0)
                    {
                        foreach (var child in nonTerminal.ChildNodesAndTokens())
                        {
                            if (child.IsNode && !child.AsNode().IsMissing)
                            {
                                retVal = false;
                                errorText = "This missing non-terminal has a non-missing child non-terminal";
                                continue;
                            }
                            else if (child.IsToken && !child.AsToken().IsMissing)
                            {
                                retVal = false;
                                errorText = "This missing non-terminal has a non-missing child token";
                                continue;
                            }
                        }
                    }
                    else
                    {
                        retVal = false;
                        errorText = "Missing non-terminals should have 0 Span width";
                    }
                }
                else if (nonTerminal.Span.Length == 0)
                {
                    var kind = nonTerminal.GetKind();
                    if (!(kind == "OmittedArgument" || kind.Contains("Bad") ||
                        (kind == "CompilationUnit" && 
                        nonTerminal.ChildNodesAndTokens().Count == 1 &&
                        nonTerminal.ChildNodesAndTokens().First().GetKind() == "EndOfFileToken")))
                    {
                        //Ignore BadStatement and BadDirective (these can only be present if tree has errors).
                        //Ignore case where code file is empty or file only includes trivia - in this case
                        //root node will have a single child ('EndOfFileToken') which has 0 width.
                        retVal = false;
                        errorText = "Non-terminals with 0 Span width should have IsMissing set to 'True'";
                    }
                }
            }
            else
            {
                if (nonTerminal.IsMissing)
                {
                    retVal = false;
                    errorText = "A tree with 0 errors should not contain missing non-terminals";
                }
                else if (nonTerminal.Span.Length == 0)
                {
                    var kind = nonTerminal.GetKind();
                    if (!(kind == "OmittedArgument" || (kind == "CompilationUnit" && nonTerminal.ChildNodesAndTokens().Count == 1 && nonTerminal.ChildNodesAndTokens().First().GetKind() == "EndOfFileToken")))
                    {
                        //Ignore case where code file is empty or file only includes trivia - in this case
                        //root node will have a single child ('EndOfFileToken') which has 0 width.
                        retVal = false;
                        errorText = "A tree with 0 errors should not contain non-terminals with 0 width";
                    }
                }
            }

            return retVal;
        }

        //SEE BUG 867280
        [TokenRuleAttribute(Name = "ZeroWidthOrMissingTokens", Group = "Misc")]
        public bool ZeroWidthOrMissingTokens(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (token.ContainsDiagnostics || tree.GetDiagnostics().Any())
            {
                if (token.IsMissing)
                {
                    if (token.Span.Length == 0)
                    {
                        if (token.LeadingTrivia.Any() || token.TrailingTrivia.Any())
                        {
                            var atleastOneSkippedTrivia = (
                                from leadingTrivia in token.LeadingTrivia
                                let kind = leadingTrivia.GetKind()
                                where kind == "SkippedTextTrivia" || kind == "SkippedTokens"
                                select new
                                {
                                    leadingTrivia,
                                    kind
                                }

                            ).Any();
                            if (!atleastOneSkippedTrivia)
                            {
                                atleastOneSkippedTrivia = (
                                    from trailingTrivia in token.TrailingTrivia
                                    let kind = trailingTrivia.GetKind()
                                    where kind == "SkippedTextTrivia" || kind == "SkippedTokens"
                                    select new
                                    {
                                        trailingTrivia,
                                        kind
                                    }

                                ).Any();
                                if (!atleastOneSkippedTrivia)
                                {
                                    retVal = false;
                                    errorText = "Missing tokens should have at least one trivia with kind SkippedTextTrivia OR SkippedTokens";
                                }
                            }
                        }
                    }
                    else
                    {
                        retVal = false;
                        errorText = "Missing tokens should have 0 Span width";
                    }
                }
                else if (token.Span.Length == 0)
                {
                    var kind = token.GetKind();
                    if (!(kind == "EndOfFileToken" || kind == "EmptyToken" || kind == "EndOfDocumentationCommentToken" || kind == "EndOfDirectiveToken" || kind.Contains("Bad")))
                    {
                        //BadToken only appears in error trees for C#.
                        retVal = false;
                        errorText = "Tokens with 0 Span width should have IsMissing set to 'True'";
                    }
                }
            }
            else
            {
                if (token.IsMissing)
                {
                    retVal = false;
                    errorText = "A tree with 0 errors should not contain missing tokens";
                }
                else if (token.Span.Length == 0)
                {
                    var kind = token.GetKind();
                    if (!(kind == "EndOfFileToken" || kind == "EmptyToken" || kind == "EndOfDocumentationCommentToken" || kind == "EndOfDirectiveToken"))
                    {
                        //EmptyToken can be present even in non-error cases in VB (it is used for OmittedArgument).
                        retVal = false;
                        errorText = "A tree with 0 errors should not contain tokens with 0 width";
                    }
                }
            }

            return retVal;
        }

        [NonTerminalRuleAttribute(Name = "BadNonTerminals", Group = "Misc")]
        public bool BadNonTerminals(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var kind = nonTerminal.GetKind();
            if (tree.GetDiagnostics().Any())
            {
                if (kind.Contains("Bad"))
                {
                    if (tree.GetDiagnostics(nonTerminal).Any())
                    {
                    }
                    else
                    {
                        retVal = false;
                        errorText = "Bad non-terminals should have at least one error on them";
                    }
                }
            }
            else if (kind.Contains("Bad"))
            {
                retVal = false;
                errorText = "A tree with 0 errors should not contain Bad non-terminals";
            }

            return retVal;
        }

        [TokenRuleAttribute(Name = "BadTokens", Group = "Misc")]
        public bool BadTokens(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var kind = token.GetKind();
            if (tree.GetDiagnostics().Any())
            {
                if (kind.Contains("Bad"))
                {
                    if (tree.GetDiagnostics(token).Any())
                    {
                    }
                    else
                    {
                        retVal = false;
                        errorText = "Bad tokens should have at least one error on them";
                    }
                }
            }
            else if (kind.Contains("Bad"))
            {
                retVal = false;
                errorText = "A tree with 0 errors should not contain Bad tokens";
            }

            return retVal;
        }

        //SEE BUG 867280
        [TriviaRuleAttribute(Name = "ZeroWidthTrivia", Group = "Misc")]
        public bool ZeroWidthTrivia(SyntaxTrivia trivia, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            if (trivia.Span.Length == 0 || trivia.FullSpan.Length == 0)
            {
                retVal = false;
                errorText = "Trivia should never have 0 Span or FullSpan width";
            }

            return retVal;
        }

        //SEE BUG 865832
        [TokenRuleAttribute(Name = "WhiteSpaceInTokens", Group = "Misc")]
        public bool WhiteSpaceInTokens(SyntaxToken token, SyntaxTree tree, ref string errorText)
        {
            var retVal = true;
            var kind = token.GetKind();
            if (kind != "StatementTerminatorToken" && kind != "DateLiteralToken" && kind != "StringLiteralToken" && kind != "CharacterLiteralToken" && kind != "XmlTextLiteralToken" && kind != "XmlAttributeDataToken" && kind != "XmlCommentDataToken" && kind != "XmlCDataToken" && kind != "XmlProcessingInstructionDataToken" && kind != "XmlTextLiteralNewLineToken" && kind != "LessThanEqualsToken" && kind != "GreaterThanEqualsToken" && kind != "LessThanGreaterThanToken" && kind != "ColonEqualsToken" && kind != "CaretEqualsToken" && kind != "AsteriskEqualsToken" && kind != "PlusEqualsToken" && kind != "MinusEqualsToken" && kind != "SlashEqualsToken" && kind != "BackslashEqualsToken" && kind != "LessThanLessThanToken" && kind != "GreaterThanGreaterThanToken" && kind != "LessThanLessThanEqualsToken" && kind != "GreaterThanGreaterThanEqualsToken" && kind != "AmpersandEqualsToken")
            {
                var text = token.ToString();
                if ((text.Contains(" ") || text.Contains('\t') || text.Contains('\r') || text.Contains('\n') || text.Contains("\r\n")))
                {
                    retVal = false;
                    errorText = "The text of this token should not contain any whitespace";
                }
            }

            return retVal;
        }
    }
}
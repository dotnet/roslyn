// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class SyntaxHelpers
    {
        internal static readonly CSharpParseOptions PreviewParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview); // Used to be LanguageVersionFacts.CurrentVersion

        /// <summary>
        /// Parse expression. Returns null if there are any errors.
        /// </summary>
        internal static ExpressionSyntax? ParseExpression(
            this string expr,
            DiagnosticBag diagnostics,
            bool allowFormatSpecifiers,
            out ReadOnlyCollection<string>? formatSpecifiers)
        {
            // Remove trailing semi-colon if any. This is to support copy/paste
            // of (simple cases of) RHS of assignment in Watch window, not to
            // allow arbitrary syntax after the semi-colon, not even comments.
            if (RemoveSemicolonIfAny(ref expr))
            {
                // Format specifiers are not expected before a semi-colon.
                allowFormatSpecifiers = false;
            }

            var syntax = ParseDebuggerExpression(expr, consumeFullText: !allowFormatSpecifiers);
            diagnostics.AddRange(syntax.GetDiagnostics());
            formatSpecifiers = null;

            if (allowFormatSpecifiers)
            {
                var builder = ArrayBuilder<string>.GetInstance();
                if (ParseFormatSpecifiers(builder, expr, syntax.FullWidth, diagnostics) &&
                    builder.Count > 0)
                {
                    formatSpecifiers = new ReadOnlyCollection<string>(builder.ToArray());
                }

                builder.Free();
            }
            return diagnostics.HasAnyErrors() ? null : syntax;
        }

        internal static ExpressionSyntax? ParseAssignment(
            this string target,
            string expr,
            DiagnosticBag diagnostics)
        {
            var text = SourceText.From(expr, encoding: null, SourceHashAlgorithms.Default);
            var expression = ParseDebuggerExpressionInternal(text, consumeFullText: true);
            // We're creating a SyntaxTree for just the RHS so that the Diagnostic spans for parse errors
            // will be correct (with respect to the original input text).  If we ever expose a SemanticModel
            // for debugger expressions, we should use this SyntaxTree.
            var syntaxTree = expression.CreateSyntaxTree(text);
            diagnostics.AddRange(syntaxTree.GetDiagnostics());
            if (diagnostics.HasAnyErrors())
            {
                return null;
            }

            // Any Diagnostic spans produced in binding will be offset by the length of the "target" expression text.
            // If we want to support live squiggles in debugger windows, SemanticModel, etc, we'll want to address this.
            var targetSyntax = ParseDebuggerExpressionInternal(SourceText.From(target, encoding: null, SourceHashAlgorithms.Default), consumeFullText: true);
            Debug.Assert(!targetSyntax.GetDiagnostics().Any(), "The target of an assignment should never contain Diagnostics if we're being allowed to assign to it in the debugger.");

            var assignment = InternalSyntax.SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                targetSyntax,
                InternalSyntax.SyntaxFactory.Token(SyntaxKind.EqualsToken),
                expression);
            return assignment.MakeDebuggerExpression(SourceText.From(assignment.ToString(), encoding: null, SourceHashAlgorithms.Default));
        }

        /// <summary>
        /// Parse statement. Returns null if there are any errors.
        /// </summary>
        internal static StatementSyntax? ParseStatement(
            this string expr,
            DiagnosticBag diagnostics)
        {
            var syntax = ParseDebuggerStatement(expr);
            diagnostics.AddRange(syntax.GetDiagnostics());
            return diagnostics.HasAnyErrors() ? null : syntax;
        }

        /// <summary>
        /// Return set of identifier tokens, with leading and
        /// trailing spaces and comma separators removed.
        /// </summary>
        private static bool ParseFormatSpecifiers(
            ArrayBuilder<string> builder,
            string expr,
            int offset,
            DiagnosticBag diagnostics)
        {
            bool expectingComma = true;
            int start = -1;
            int n = expr.Length;

            for (; offset < n; offset++)
            {
                var c = expr[offset];
                if (SyntaxFacts.IsWhitespace(c) || (c == ','))
                {
                    if (start >= 0)
                    {
                        var token = expr.Substring(start, offset - start);
                        if (expectingComma)
                        {
                            ReportInvalidFormatSpecifier(token, diagnostics);
                            return false;
                        }
                        builder.Add(token);
                        start = -1;
                        expectingComma = (c != ',');
                    }
                    else if (c == ',')
                    {
                        if (!expectingComma)
                        {
                            ReportInvalidFormatSpecifier(",", diagnostics);
                            return false;
                        }
                        expectingComma = false;
                    }
                }
                else if (start < 0)
                {
                    start = offset;
                }
            }

            if (start >= 0)
            {
                var token = expr.Substring(start);
                if (expectingComma)
                {
                    ReportInvalidFormatSpecifier(token, diagnostics);
                    return false;
                }
                builder.Add(token);
            }
            else if (!expectingComma)
            {
                ReportInvalidFormatSpecifier(",", diagnostics);
                return false;
            }

            // Verify format specifiers are valid identifiers.
            foreach (var token in builder)
            {
                if (!token.All(SyntaxFacts.IsIdentifierPartCharacter))
                {
                    ReportInvalidFormatSpecifier(token, diagnostics);
                    return false;
                }
            }

            return true;
        }

        private static void ReportInvalidFormatSpecifier(string token, DiagnosticBag diagnostics)
        {
            diagnostics.Add(ErrorCode.ERR_InvalidSpecifier, Location.None, token);
        }

        private static bool RemoveSemicolonIfAny(ref string str)
        {
            for (int i = str.Length - 1; i >= 0; i--)
            {
                var c = str[i];
                if (c == ';')
                {
                    str = str.Substring(0, i);
                    return true;
                }
                if (!SyntaxFacts.IsWhitespace(c))
                {
                    break;
                }
            }
            return false;
        }

        private static ExpressionSyntax ParseDebuggerExpression(string text, bool consumeFullText)
        {
            var source = SourceText.From(text, encoding: null, SourceHashAlgorithms.Default);
            var expression = ParseDebuggerExpressionInternal(source, consumeFullText);
            return expression.MakeDebuggerExpression(source);
        }

        private static InternalSyntax.ExpressionSyntax ParseDebuggerExpressionInternal(SourceText source, bool consumeFullText)
        {
            using var lexer = new InternalSyntax.Lexer(source, PreviewParseOptions, allowPreprocessorDirectives: false);
            using var parser = new InternalSyntax.LanguageParser(lexer, oldTree: null, changes: null, lexerMode: InternalSyntax.LexerMode.DebuggerSyntax);

            var node = parser.ParseExpression();
            if (consumeFullText)
                node = parser.ConsumeUnexpectedTokens(node);
            return node;
        }

        private static StatementSyntax ParseDebuggerStatement(string text)
        {
            var source = SourceText.From(text, encoding: null, SourceHashAlgorithms.Default);
            using var lexer = new InternalSyntax.Lexer(source, PreviewParseOptions);
            using var parser = new InternalSyntax.LanguageParser(lexer, oldTree: null, changes: null, lexerMode: InternalSyntax.LexerMode.DebuggerSyntax);

            var statement = parser.ParseStatement();
            var syntaxTree = statement.CreateSyntaxTree(source);
            return (StatementSyntax)syntaxTree.GetRoot();
        }

        private static SyntaxTree CreateSyntaxTree(this InternalSyntax.CSharpSyntaxNode root, SourceText text)
        {
            return CSharpSyntaxTree.CreateForDebugger((CSharpSyntaxNode)root.CreateRed(), text, PreviewParseOptions);
        }

        private static ExpressionSyntax MakeDebuggerExpression(this InternalSyntax.ExpressionSyntax expression, SourceText text)
        {
            var syntaxTree = InternalSyntax.SyntaxFactory.ExpressionStatement(attributeLists: default, expression, InternalSyntax.SyntaxFactory.Token(SyntaxKind.SemicolonToken)).CreateSyntaxTree(text);
            return ((ExpressionStatementSyntax)syntaxTree.GetRoot()).Expression;
        }

        internal static string EscapeKeywordIdentifiers(string identifier)
        {
            return SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(identifier)) ? "@" + identifier : identifier;
        }

        /// <remarks>
        /// We don't want to use the real lexer because we want to treat keywords as identifiers.
        /// Since the inputs are so simple, we'll just do the lexing ourselves.
        /// </remarks>
        internal static bool TryParseDottedName(string input, [NotNullWhen(true)] out NameSyntax? output)
        {
            var pooled = PooledStringBuilder.GetInstance();
            try
            {
                var builder = pooled.Builder;

                output = null;
                foreach (var ch in input)
                {
                    if (builder.Length == 0)
                    {
                        if (!SyntaxFacts.IsIdentifierStartCharacter(ch))
                        {
                            output = null;
                            return false;
                        }

                        builder.Append(ch);
                    }
                    else if (ch == '.')
                    {
                        var identifierName = SyntaxFactory.IdentifierName(builder.ToString());

                        builder.Clear();

                        output = output == null
                            ? (NameSyntax)identifierName
                            : SyntaxFactory.QualifiedName(output, identifierName);
                    }
                    else if (SyntaxFacts.IsIdentifierPartCharacter(ch))
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        output = null;
                        return false;
                    }
                }

                // There must be at least one character in the last identifier.
                if (builder.Length == 0)
                {
                    output = null;
                    return false;
                }

                var finalIdentifierName = SyntaxFactory.IdentifierName(builder.ToString());
                output = output == null
                    ? (NameSyntax)finalIdentifierName
                    : SyntaxFactory.QualifiedName(output, finalIdentifierName);

                return true;
            }
            finally
            {
                pooled.Free();
            }
        }

        internal static NameSyntax PrependExternAlias(IdentifierNameSyntax externAliasSyntax, NameSyntax nameSyntax)
        {
            if (nameSyntax is QualifiedNameSyntax qualifiedNameSyntax)
            {
                return SyntaxFactory.QualifiedName(
                    PrependExternAlias(externAliasSyntax, qualifiedNameSyntax.Left),
                    qualifiedNameSyntax.Right);
            }
            else
            {
                return SyntaxFactory.AliasQualifiedName(externAliasSyntax, (SimpleNameSyntax)nameSyntax);
            }
        }
    }
}

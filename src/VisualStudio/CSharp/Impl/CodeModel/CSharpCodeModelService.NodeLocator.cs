// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal partial class CSharpCodeModelService
    {
        protected override AbstractNodeLocator CreateNodeLocator()
        {
            return new NodeLocator();
        }

        private class NodeLocator : AbstractNodeLocator
        {
            protected override string LanguageName => LanguageNames.CSharp;

            protected override EnvDTE.vsCMPart DefaultPart => EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

            protected override VirtualTreePoint? GetStartPoint(SourceText text, OptionSet options, SyntaxNode node, EnvDTE.vsCMPart part)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ArrowExpressionClause:
                        return GetStartPoint(text, (ArrowExpressionClauseSyntax)node, part);
                    case SyntaxKind.Attribute:
                        return GetStartPoint(text, (AttributeSyntax)node, part);
                    case SyntaxKind.AttributeArgument:
                        return GetStartPoint(text, (AttributeArgumentSyntax)node, part);
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.EnumDeclaration:
                        return GetStartPoint(text, (BaseTypeDeclarationSyntax)node, part);
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return GetStartPoint(text, options, (BaseMethodDeclarationSyntax)node, part);
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.EventDeclaration:
                        return GetStartPoint(text, options, (BasePropertyDeclarationSyntax)node, part);
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        return GetStartPoint(text, options, (AccessorDeclarationSyntax)node, part);
                    case SyntaxKind.DelegateDeclaration:
                        return GetStartPoint(text, (DelegateDeclarationSyntax)node, part);
                    case SyntaxKind.NamespaceDeclaration:
                        return GetStartPoint(text, (NamespaceDeclarationSyntax)node, part);
                    case SyntaxKind.UsingDirective:
                        return GetStartPoint(text, (UsingDirectiveSyntax)node, part);
                    case SyntaxKind.EnumMemberDeclaration:
                        return GetStartPoint(text, (EnumMemberDeclarationSyntax)node, part);
                    case SyntaxKind.VariableDeclarator:
                        return GetStartPoint(text, (VariableDeclaratorSyntax)node, part);
                    case SyntaxKind.Parameter:
                        return GetStartPoint(text, (ParameterSyntax)node, part);
                    default:
                        Debug.Fail("Unsupported node kind: " + node.Kind());
                        throw new NotSupportedException();
                }
            }

            protected override VirtualTreePoint? GetEndPoint(SourceText text, OptionSet options, SyntaxNode node, EnvDTE.vsCMPart part)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ArrowExpressionClause:
                        return GetEndPoint(text, (ArrowExpressionClauseSyntax)node, part);
                    case SyntaxKind.Attribute:
                        return GetEndPoint(text, (AttributeSyntax)node, part);
                    case SyntaxKind.AttributeArgument:
                        return GetEndPoint(text, (AttributeArgumentSyntax)node, part);
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.EnumDeclaration:
                        return GetEndPoint(text, (BaseTypeDeclarationSyntax)node, part);
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return GetEndPoint(text, (BaseMethodDeclarationSyntax)node, part);
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.EventDeclaration:
                        return GetEndPoint(text, (BasePropertyDeclarationSyntax)node, part);
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        return GetEndPoint(text, (AccessorDeclarationSyntax)node, part);
                    case SyntaxKind.DelegateDeclaration:
                        return GetEndPoint(text, (DelegateDeclarationSyntax)node, part);
                    case SyntaxKind.NamespaceDeclaration:
                        return GetEndPoint(text, (NamespaceDeclarationSyntax)node, part);
                    case SyntaxKind.UsingDirective:
                        return GetEndPoint(text, (UsingDirectiveSyntax)node, part);
                    case SyntaxKind.EnumMemberDeclaration:
                        return GetEndPoint(text, (EnumMemberDeclarationSyntax)node, part);
                    case SyntaxKind.VariableDeclarator:
                        return GetEndPoint(text, (VariableDeclaratorSyntax)node, part);
                    case SyntaxKind.Parameter:
                        return GetEndPoint(text, (ParameterSyntax)node, part);
                    default:
                        Debug.Fail("Unsupported node kind: " + node.Kind());
                        throw new NotSupportedException();
                }
            }

            private VirtualTreePoint GetBodyStartPoint(SourceText text, SyntaxToken openBrace)
            {
                Debug.Assert(!openBrace.IsMissing);

                var openBraceLine = text.Lines.GetLineFromPosition(openBrace.Span.End);
                var textAfterBrace = text.ToString(TextSpan.FromBounds(openBrace.Span.End, openBraceLine.End));

                return string.IsNullOrWhiteSpace(textAfterBrace)
                    ? new VirtualTreePoint(openBrace.SyntaxTree, text, text.Lines[openBraceLine.LineNumber + 1].Start)
                    : new VirtualTreePoint(openBrace.SyntaxTree, text, openBrace.Span.End);
            }

            private VirtualTreePoint GetBodyStartPoint(SourceText text, OptionSet options, SyntaxToken openBrace, SyntaxToken closeBrace, int memberStartColumn)
            {
                Debug.Assert(!openBrace.IsMissing);
                Debug.Assert(!closeBrace.IsMissing);
                Debug.Assert(memberStartColumn >= 0);

                var openBraceLine = text.Lines.GetLineFromPosition(openBrace.SpanStart);
                var closeBraceLine = text.Lines.GetLineFromPosition(closeBrace.SpanStart);

                var tokenAfterOpenBrace = openBrace.GetNextToken();
                var nextPosition = tokenAfterOpenBrace.SpanStart;

                // We need to check if there is any significant trivia trailing this token or leading
                // to the next token. This accounts for the fact that comments were included in the token
                // stream in Dev10.
                var significantTrivia = openBrace.GetAllTrailingTrivia()
                                                 .Where(t => !t.MatchesKind(SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia))
                                                 .FirstOrDefault();

                if (significantTrivia.Kind() != SyntaxKind.None)
                {
                    nextPosition = significantTrivia.SpanStart;
                }

                // If the opening and closing curlies are at least two lines apart then place the cursor
                // on the next line provided that there isn't any token on the line with the open curly.
                if (openBraceLine.LineNumber + 1 < closeBraceLine.LineNumber &&
                    openBraceLine.LineNumber < text.Lines.IndexOf(tokenAfterOpenBrace.SpanStart))
                {
                    var lineAfterOpenBrace = text.Lines[openBraceLine.LineNumber + 1];
                    var firstNonWhitespaceOffset = lineAfterOpenBrace.GetFirstNonWhitespaceOffset() ?? -1;

                    // If the line contains any text, we return the start of the first non-whitespace character.
                    if (firstNonWhitespaceOffset >= 0)
                    {
                        return new VirtualTreePoint(openBrace.SyntaxTree, text, lineAfterOpenBrace.Start + firstNonWhitespaceOffset);
                    }

                    // If the line is all whitespace then place the caret at the first indent after the start
                    // of the member.
                    var indentSize = GetTabSize(options);
                    var lineText = lineAfterOpenBrace.ToString();

                    var lineEndColumn = lineText.GetColumnFromLineOffset(lineText.Length, indentSize);
                    var indentColumn = memberStartColumn + indentSize;
                    var virtualSpaces = indentColumn - lineEndColumn;

                    return new VirtualTreePoint(openBrace.SyntaxTree, text, lineAfterOpenBrace.End, virtualSpaces);
                }
                else
                {
                    // If the body is empty then place it after the open brace; otherwise, place
                    // at the start of the first token after the open curly.
                    if (closeBrace.SpanStart == nextPosition)
                    {
                        return new VirtualTreePoint(openBrace.SyntaxTree, text, openBrace.Span.End);
                    }
                    else
                    {
                        return new VirtualTreePoint(openBrace.SyntaxTree, text, nextPosition);
                    }
                }
            }

            private VirtualTreePoint GetBodyEndPoint(SourceText text, SyntaxToken closeBrace)
            {
                var closeBraceLine = text.Lines.GetLineFromPosition(closeBrace.SpanStart);
                var textBeforeBrace = text.ToString(TextSpan.FromBounds(closeBraceLine.Start, closeBrace.SpanStart));

                return string.IsNullOrWhiteSpace(textBeforeBrace)
                    ? new VirtualTreePoint(closeBrace.SyntaxTree, text, closeBraceLine.Start)
                    : new VirtualTreePoint(closeBrace.SyntaxTree, text, closeBrace.SpanStart);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, ArrowExpressionClauseSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                        startPosition = node.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        startPosition = node.Expression.SpanStart;
                        break;

                    default:
                        throw Exceptions.ThrowENotImpl();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, AttributeSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.GetFirstToken().SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Name.SpanStart;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, AttributeArgumentSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartBody:
                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.SpanStart;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, BaseTypeDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartHeader:
                        startPosition = node.GetFirstTokenAfterAttributes().SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        goto case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.GetFirstToken().SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Identifier.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.OpenBraceToken.IsMissing || node.CloseBraceToken.IsMissing)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        return GetBodyStartPoint(text, node.OpenBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, OptionSet options, BaseMethodDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartHeader:
                        startPosition = node.GetFirstTokenAfterAttributes().SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        goto case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.GetFirstToken().SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        if (node is { Body: { OpenBraceToken: { IsMissing: false } } })
                        {
                            var line = text.Lines.GetLineFromPosition(node.SpanStart);
                            var indentation = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(GetTabSize(options));

                            return GetBodyStartPoint(text, options, node.Body.OpenBraceToken, node.Body.CloseBraceToken, indentation);
                        }
                        else
                        {
                            switch (node.Kind())
                            {
                                case SyntaxKind.MethodDeclaration:
                                    startPosition = ((MethodDeclarationSyntax)node).Identifier.SpanStart;
                                    break;
                                case SyntaxKind.ConstructorDeclaration:
                                    startPosition = ((ConstructorDeclarationSyntax)node).Identifier.SpanStart;
                                    break;
                                case SyntaxKind.DestructorDeclaration:
                                    startPosition = ((DestructorDeclarationSyntax)node).Identifier.SpanStart;
                                    break;
                                case SyntaxKind.ConversionOperatorDeclaration:
                                    startPosition = ((ConversionOperatorDeclarationSyntax)node).ImplicitOrExplicitKeyword.SpanStart;
                                    break;
                                case SyntaxKind.OperatorDeclaration:
                                    startPosition = ((OperatorDeclarationSyntax)node).OperatorToken.SpanStart;
                                    break;
                                default:
                                    startPosition = node.GetFirstTokenAfterAttributes().SpanStart;
                                    break;
                            }
                        }

                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.Body == null || node.Body.OpenBraceToken.IsMissing || node.Body.CloseBraceToken.IsMissing)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        return GetBodyStartPoint(text, node.Body.OpenBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private AccessorDeclarationSyntax FindFirstAccessorNode(BasePropertyDeclarationSyntax node)
            {
                if (node.AccessorList == null)
                {
                    return null;
                }

                return node.AccessorList.Accessors.FirstOrDefault();
            }

            private VirtualTreePoint GetStartPoint(SourceText text, OptionSet options, BasePropertyDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        goto case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        var firstAccessorNode = FindFirstAccessorNode(node);
                        if (firstAccessorNode != null)
                        {
                            var line = text.Lines.GetLineFromPosition(firstAccessorNode.SpanStart);
                            var indentation = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(GetTabSize(options));

                            if (firstAccessorNode.Body != null)
                            {
                                return GetBodyStartPoint(text, options, firstAccessorNode.Body.OpenBraceToken, firstAccessorNode.Body.CloseBraceToken, indentation);
                            }
                            else if (!firstAccessorNode.SemicolonToken.IsMissing)
                            {
                                // This is total weirdness from the old C# code model with auto props.
                                // If there isn't a body, the semi-colon is used
                                return GetBodyStartPoint(text, options, firstAccessorNode.SemicolonToken, firstAccessorNode.SemicolonToken, indentation);
                            }
                        }

                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.AccessorList != null && !node.AccessorList.OpenBraceToken.IsMissing)
                        {
                            var line = text.Lines.GetLineFromPosition(node.SpanStart);
                            var indentation = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(GetTabSize(options));

                            return GetBodyStartPoint(text, options, node.AccessorList.OpenBraceToken, node.AccessorList.CloseBraceToken, indentation);
                        }

                        throw Exceptions.ThrowEFail();

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, OptionSet options, AccessorDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        if (node.Body != null && !node.Body.OpenBraceToken.IsMissing)
                        {
                            var line = text.Lines.GetLineFromPosition(node.SpanStart);
                            var indentation = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(GetTabSize(options));

                            return GetBodyStartPoint(text, options, node.Body.OpenBraceToken, node.Body.CloseBraceToken, indentation);
                        }

                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.Body == null ||
                            node.Body.OpenBraceToken.IsMissing ||
                            node.Body.CloseBraceToken.IsMissing)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        return GetBodyStartPoint(text, node.Body.OpenBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, NamespaceDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.GetFirstToken().SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Name.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.OpenBraceToken.IsMissing || node.CloseBraceToken.IsMissing)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        return GetBodyStartPoint(text, node.OpenBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, DelegateDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        goto case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.GetFirstToken().SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Identifier.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, UsingDirectiveSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Name.SpanStart;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, VariableDeclaratorSyntax node, EnvDTE.vsCMPart part)
            {
                var field = node.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>();
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (field.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        goto case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = field.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Identifier.SpanStart;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, EnumMemberDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        goto case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Identifier.SpanStart;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetStartPoint(SourceText text, ParameterSyntax node, EnvDTE.vsCMPart part)
            {
                int startPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        goto case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        startPosition = node.SpanStart;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        startPosition = node.Identifier.SpanStart;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, startPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, ArrowExpressionClauseSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBody:
                        endPosition = node.Span.End;
                        break;

                    default:
                        throw Exceptions.ThrowENotImpl();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, AttributeSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Name.Span.End;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, AttributeArgumentSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartBody:
                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, BaseTypeDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        endPosition = node.AttributeLists.Last().GetLastToken().Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Identifier.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        return GetBodyEndPoint(text, node.CloseBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, BaseMethodDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        endPosition = node.AttributeLists.Last().GetLastToken().Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        if (node.Body != null && !node.Body.CloseBraceToken.IsMissing)
                        {
                            return GetBodyEndPoint(text, node.Body.CloseBraceToken);
                        }
                        else
                        {
                            switch (node.Kind())
                            {
                                case SyntaxKind.MethodDeclaration:
                                    endPosition = ((MethodDeclarationSyntax)node).Identifier.Span.End;
                                    break;
                                case SyntaxKind.ConstructorDeclaration:
                                    endPosition = ((ConstructorDeclarationSyntax)node).Identifier.Span.End;
                                    break;
                                case SyntaxKind.DestructorDeclaration:
                                    endPosition = ((DestructorDeclarationSyntax)node).Identifier.Span.End;
                                    break;
                                case SyntaxKind.ConversionOperatorDeclaration:
                                    endPosition = ((ConversionOperatorDeclarationSyntax)node).ImplicitOrExplicitKeyword.Span.End;
                                    break;
                                case SyntaxKind.OperatorDeclaration:
                                    endPosition = ((OperatorDeclarationSyntax)node).OperatorToken.Span.End;
                                    break;
                                default:
                                    endPosition = node.GetFirstTokenAfterAttributes().Span.End;
                                    break;
                            }
                        }

                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.Body == null || node.Body.OpenBraceToken.IsMissing || node.Body.CloseBraceToken.IsMissing)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        return GetBodyEndPoint(text, node.Body.CloseBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, BasePropertyDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        endPosition = node.AttributeLists.Last().Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        var firstAccessorNode = FindFirstAccessorNode(node);
                        if (firstAccessorNode != null)
                        {
                            if (firstAccessorNode.Body != null)
                            {
                                return GetBodyEndPoint(text, firstAccessorNode.Body.CloseBraceToken);
                            }
                            else
                            {
                                // This is total weirdness from the old C# code model with auto props.
                                // If there isn't a body, the semi-colon is used
                                return GetBodyEndPoint(text, firstAccessorNode.SemicolonToken);
                            }
                        }

                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.AccessorList != null && !node.AccessorList.CloseBraceToken.IsMissing)
                        {
                            return GetBodyEndPoint(text, node.AccessorList.CloseBraceToken);
                        }

                        throw Exceptions.ThrowEFail();

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, AccessorDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        if (node.Body == null ||
                            node.Body.OpenBraceToken.IsMissing ||
                            node.Body.CloseBraceToken.IsMissing)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        return GetBodyEndPoint(text, node.Body.CloseBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, DelegateDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        endPosition = node.AttributeLists.Last().Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Identifier.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, NamespaceDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Name.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        if (node.OpenBraceToken.IsMissing || node.CloseBraceToken.IsMissing)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        return GetBodyEndPoint(text, node.CloseBraceToken);

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, UsingDirectiveSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Name.Span.End;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, EnumMemberDeclarationSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        endPosition = node.AttributeLists.Last().GetLastToken().Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Identifier.Span.End;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, VariableDeclaratorSyntax node, EnvDTE.vsCMPart part)
            {
                var field = node.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>();
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (field.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        endPosition = field.AttributeLists.Last().GetLastToken().Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = field.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Identifier.Span.End;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }

            private VirtualTreePoint GetEndPoint(SourceText text, ParameterSyntax node, EnvDTE.vsCMPart part)
            {
                int endPosition;

                switch (part)
                {
                    case EnvDTE.vsCMPart.vsCMPartName:
                    case EnvDTE.vsCMPart.vsCMPartAttributes:
                    case EnvDTE.vsCMPart.vsCMPartHeader:
                    case EnvDTE.vsCMPart.vsCMPartWhole:
                    case EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter:
                    case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes:
                        throw Exceptions.ThrowENotImpl();

                    case EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter:
                        if (node.AttributeLists.Count == 0)
                        {
                            throw Exceptions.ThrowEFail();
                        }

                        endPosition = node.AttributeLists.Last().GetLastToken().Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes:
                        endPosition = node.Span.End;
                        break;

                    case EnvDTE.vsCMPart.vsCMPartBody:
                        throw Exceptions.ThrowEFail();

                    case EnvDTE.vsCMPart.vsCMPartNavigate:
                        endPosition = node.Identifier.Span.End;
                        break;

                    default:
                        throw Exceptions.ThrowEInvalidArg();
                }

                return new VirtualTreePoint(node.SyntaxTree, text, endPosition);
            }
        }
    }
}

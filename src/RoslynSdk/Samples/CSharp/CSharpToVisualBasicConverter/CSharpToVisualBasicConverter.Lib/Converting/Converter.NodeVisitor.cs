// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using CSharpToVisualBasicConverter.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace CSharpToVisualBasicConverter
{
    public partial class Converter
    {
        private class NodeVisitor : CS.CSharpSyntaxVisitor<SyntaxNode>
        {
            private readonly SourceText text;
            private readonly IDictionary<string, string> identifierMap;
            private readonly bool convertStrings;
            private readonly StatementVisitor statementVisitor;

            public NodeVisitor(SourceText text, IDictionary<string, string> identifierMap, bool convertStrings)
            {
                this.text = text;
                this.identifierMap = identifierMap;
                this.convertStrings = convertStrings;
                statementVisitor = new StatementVisitor(this, text);
            }

            internal SyntaxToken VisitToken(SyntaxToken token)
            {
                SyntaxToken result = VisitTokenWorker(token);
                return CopyTriviaTo(token, result);
            }

            private SyntaxToken CopyTriviaTo(SyntaxToken from, SyntaxToken to)
            {
                if (from.HasLeadingTrivia)
                {
                    to = to.WithLeadingTrivia(ConvertTrivia(from.LeadingTrivia));
                }

                if (from.HasTrailingTrivia)
                {
                    to = to.WithTrailingTrivia(ConvertTrivia(from.TrailingTrivia));
                }

                return to;
            }

            private SyntaxToken VisitTokenWorker(SyntaxToken token)
            {
                SyntaxKind kind = token.Kind();
                if (kind == CS.SyntaxKind.IdentifierToken)
                {
                    return VB.SyntaxFactory.Identifier(token.ValueText);
                }

                switch (kind)
                {
                    case CS.SyntaxKind.AbstractKeyword:
                        return token.Parent is CS.Syntax.TypeDeclarationSyntax
                            ? VB.SyntaxFactory.Token(VB.SyntaxKind.MustInheritKeyword)
                            : VB.SyntaxFactory.Token(VB.SyntaxKind.MustOverrideKeyword);

                    case CS.SyntaxKind.AssemblyKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.AssemblyKeyword);
                    case CS.SyntaxKind.AsyncKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.AsyncKeyword);
                    case CS.SyntaxKind.BoolKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.BooleanKeyword);
                    case CS.SyntaxKind.ByteKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ByteKeyword);
                    case CS.SyntaxKind.ConstKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ConstKeyword);
                    case CS.SyntaxKind.IfKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.IfKeyword);
                    case CS.SyntaxKind.IntKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.IntegerKeyword);
                    case CS.SyntaxKind.InternalKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.FriendKeyword);
                    case CS.SyntaxKind.ModuleKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ModuleKeyword);
                    case CS.SyntaxKind.NewKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.OverloadsKeyword);
                    case CS.SyntaxKind.OutKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ByRefKeyword);
                    case CS.SyntaxKind.OverrideKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.OverridesKeyword);
                    case CS.SyntaxKind.ParamsKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ParamArrayKeyword);
                    case CS.SyntaxKind.PartialKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.PartialKeyword);
                    case CS.SyntaxKind.PrivateKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.PrivateKeyword);
                    case CS.SyntaxKind.ProtectedKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ProtectedKeyword);
                    case CS.SyntaxKind.PublicKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.PublicKeyword);
                    case CS.SyntaxKind.ReadOnlyKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ReadOnlyKeyword);
                    case CS.SyntaxKind.RefKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ByRefKeyword);
                    case CS.SyntaxKind.SealedKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.NotOverridableKeyword);
                    case CS.SyntaxKind.ShortKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ShortKeyword);
                    case CS.SyntaxKind.StaticKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.SharedKeyword);
                    case CS.SyntaxKind.ThisKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.MeKeyword);
                    case CS.SyntaxKind.UIntKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.UIntegerKeyword);
                    case CS.SyntaxKind.UsingKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.ImportsKeyword);
                    case CS.SyntaxKind.VirtualKeyword:
                        return VB.SyntaxFactory.Token(VB.SyntaxKind.OverridableKeyword);
                    case CS.SyntaxKind.NumericLiteralToken:
                        return VB.SyntaxFactory.IntegerLiteralToken(token.ValueText, VB.Syntax.LiteralBase.Decimal, VB.Syntax.TypeCharacter.None, 0);
                    case CS.SyntaxKind.CharacterLiteralToken:
                        {
                            string text = Microsoft.CodeAnalysis.VisualBasic.SymbolDisplay.FormatPrimitive(token.ValueText[0], quoteStrings: true, useHexadecimalNumbers: true);
                            return VB.SyntaxFactory.CharacterLiteralToken(text, token.ValueText[0]);
                        }

                    case CS.SyntaxKind.StringLiteralToken:
                        {
                            string text = Microsoft.CodeAnalysis.VisualBasic.SymbolDisplay.FormatPrimitive(token.ValueText, quoteStrings: true, useHexadecimalNumbers: true);
                            return VB.SyntaxFactory.StringLiteralToken(text, token.ValueText);
                        }
                }

                if (CS.SyntaxFacts.IsKeywordKind(kind) ||
                    kind == CS.SyntaxKind.None)
                {
                    return VB.SyntaxFactory.Identifier(token.ValueText);
                }
                else if (CS.SyntaxFacts.IsPunctuation(kind))
                {
                    return VB.SyntaxFactory.Token(VB.SyntaxKind.EmptyToken);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            internal TSyntaxNode Visit<TSyntaxNode>(SyntaxNode node) where TSyntaxNode : SyntaxNode
            {
                return (TSyntaxNode)Visit(node);
            }

            private VB.Syntax.TypeSyntax VisitType(CS.Syntax.TypeSyntax type)
            {
                return Visit<VB.Syntax.TypeSyntax>(type);
            }

            internal VB.Syntax.NameSyntax VisitName(CS.Syntax.NameSyntax name)
            {
                return Visit<VB.Syntax.NameSyntax>(name);
            }

            internal VB.Syntax.ExpressionSyntax VisitExpression(CS.Syntax.ExpressionSyntax expression)
            {
                return ConvertToExpression(Visit<SyntaxNode>(expression));
            }

            internal VB.Syntax.StatementSyntax VisitStatement(CS.Syntax.ExpressionSyntax expression)
            {
                return ConvertToStatement(Visit<SyntaxNode>(expression));
            }

            private static VB.Syntax.ExpressionSyntax ConvertToExpression(SyntaxNode node)
            {
                if (node == null)
                {
                    return null;
                }
                else if (node is VB.Syntax.ExpressionSyntax)
                {
                    return (VB.Syntax.ExpressionSyntax)node;
                }

                string error = CreateCouldNotBeConvertedString(((SyntaxNode)node).ToFullString(), typeof(VB.Syntax.ExpressionSyntax));
                return VB.SyntaxFactory.StringLiteralExpression(VB.SyntaxFactory.StringLiteralToken(error, error));
            }

            private VB.Syntax.StatementSyntax ConvertToStatement(SyntaxNode node)
            {
                if (node == null)
                {
                    return null;
                }
                else if (node is VB.Syntax.StatementSyntax)
                {
                    return (VB.Syntax.StatementSyntax)node;
                }
                else if (node is VB.Syntax.InvocationExpressionSyntax)
                {
                    return VB.SyntaxFactory.ExpressionStatement((VB.Syntax.InvocationExpressionSyntax)node);
                }
                else if (node is VB.Syntax.AwaitExpressionSyntax)
                {
                    return VB.SyntaxFactory.ExpressionStatement((VB.Syntax.AwaitExpressionSyntax)node);
                }
                else
                {
                    // can happen in error scenarios
                    return CreateBadStatement(((SyntaxNode)node).ToFullString(), typeof(VB.Syntax.StatementSyntax));
                }
            }

            private SyntaxTriviaList ConvertTrivia(SyntaxTriviaList list)
            {
                return VB.SyntaxFactory.TriviaList(list.Where(t => !CS.SyntaxFacts.IsDocumentationCommentTrivia(t.Kind()))
                                                .SelectMany(VisitTrivia).Aggregate(new List<SyntaxTrivia>(),
                                                    (builder, trivia) => { builder.Add(trivia); return builder; }));
            }

            public override SyntaxNode VisitCompilationUnit(CS.Syntax.CompilationUnitSyntax node)
            {
                SyntaxList<VB.Syntax.AttributeListSyntax> blocks = List(node.AttributeLists.Select(Visit<VB.Syntax.AttributeListSyntax>));

                SyntaxList<VB.Syntax.AttributesStatementSyntax> attributes = blocks.Count > 0
                    ? List(VB.SyntaxFactory.AttributesStatement(blocks))
                    : default(SyntaxList<VB.Syntax.AttributesStatementSyntax>);

                IEnumerable<VB.Syntax.ImportsStatementSyntax> vbImports = node.Externs.Select(Visit<VB.Syntax.ImportsStatementSyntax>)
                                    .Concat(node.Usings.Select(Visit<VB.Syntax.ImportsStatementSyntax>));

                return VB.SyntaxFactory.CompilationUnit(
                    List<VB.Syntax.OptionStatementSyntax>(),
                    List(vbImports),
                    attributes,
                    List(node.Members.Select(Visit<VB.Syntax.StatementSyntax>)),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.EndOfFileToken));
            }

            private SyntaxList<VB.Syntax.AttributeListSyntax> ConvertAttributes(
                IEnumerable<CS.Syntax.AttributeListSyntax> list)
            {
                return List(list.Select(Visit<VB.Syntax.AttributeListSyntax>));
            }

            public override SyntaxNode VisitUsingDirective(CS.Syntax.UsingDirectiveSyntax directive)
            {
                if (directive.Alias == null)
                {
                    return VB.SyntaxFactory.ImportsStatement(
                        CopyTriviaTo(directive.UsingKeyword, VB.SyntaxFactory.Token(VB.SyntaxKind.ImportsKeyword)),
                        SeparatedList<VB.Syntax.ImportsClauseSyntax>(VB.SyntaxFactory.SimpleImportsClause(VisitName(directive.Name))));
                }
                else
                {
                    return VB.SyntaxFactory.ImportsStatement(
                        CopyTriviaTo(directive.UsingKeyword, VB.SyntaxFactory.Token(VB.SyntaxKind.ImportsKeyword)),
                        SeparatedList<VB.Syntax.ImportsClauseSyntax>(
                            VB.SyntaxFactory.SimpleImportsClause(VB.SyntaxFactory.ImportAliasClause(ConvertIdentifier(directive.Alias.Name)), VisitName(directive.Name))));
                }
            }

            public override SyntaxNode VisitIdentifierName(CS.Syntax.IdentifierNameSyntax node)
            {
                return VB.SyntaxFactory.IdentifierName(ConvertIdentifier(node));
            }

            internal SyntaxToken ConvertIdentifier(CS.Syntax.IdentifierNameSyntax name)
            {
                return ConvertIdentifier(name.Identifier);
            }

            internal SyntaxToken ConvertIdentifier(SyntaxToken name)
            {
                string text = name.ValueText;
                if (identifierMap != null && identifierMap.TryGetValue(text, out string replace))
                {
                    text = replace;
                }

                SyntaxNode node = name.Parent;
                bool afterDot1 = node.IsParentKind(CS.SyntaxKind.QualifiedName) && ((CS.Syntax.QualifiedNameSyntax)node.Parent).Right == node;
                bool afterDot2 = node.IsParentKind(CS.SyntaxKind.SimpleMemberAccessExpression) && ((CS.Syntax.MemberAccessExpressionSyntax)node.Parent).Name == node;
                bool afterDot = afterDot1 || afterDot2;

                if (!afterDot && VB.SyntaxFacts.GetKeywordKind(text) != VB.SyntaxKind.None)
                {
                    return VB.SyntaxFactory.Identifier(
                        "[" + text + "]",
                        isBracketed: true,
                        identifierText: text,
                        typeCharacter: VB.Syntax.TypeCharacter.None);
                }

                return VB.SyntaxFactory.Identifier(text);
            }

            internal IEnumerable<SyntaxTrivia> VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.HasStructure)
                {
                    VB.Syntax.StructuredTriviaSyntax structure = Visit<VB.Syntax.StructuredTriviaSyntax>(trivia.GetStructure());

                    if (structure is VB.Syntax.BadDirectiveTriviaSyntax)
                    {
                        yield return VB.SyntaxFactory.CommentTrivia(structure.ToFullString());
                    }
                    else
                    {
                        yield return VB.SyntaxFactory.Trivia(structure);
                    }
                }
                else
                {
                    switch (trivia.Kind())
                    {
                        case CS.SyntaxKind.MultiLineCommentTrivia:
                        case CS.SyntaxKind.SingleLineCommentTrivia:
                            yield return VB.SyntaxFactory.CommentTrivia("'" + trivia.ToString().Substring(2));
                            break;
                        case CS.SyntaxKind.EndOfLineTrivia:
                        case CS.SyntaxKind.WhitespaceTrivia:
                            yield break;
                        case CS.SyntaxKind.DisabledTextTrivia:
                            yield return VB.SyntaxFactory.DisabledTextTrivia(trivia.ToString());
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            public override SyntaxNode VisitAliasQualifiedName(CS.Syntax.AliasQualifiedNameSyntax node)
            {
                // TODO: don't throw away the alias
                return Visit<SyntaxNode>(node.Name);
            }

            public override SyntaxNode VisitQualifiedName(CS.Syntax.QualifiedNameSyntax node)
            {
                if (node.Right.IsKind(CS.SyntaxKind.GenericName))
                {
                    GenericNameSyntax genericName = (CS.Syntax.GenericNameSyntax)node.Right;
                    return VB.SyntaxFactory.QualifiedName(
                        VisitName(node.Left),
                        VB.SyntaxFactory.GenericName(
                            ConvertIdentifier(genericName.Identifier),
                            ConvertTypeArguments(genericName.TypeArgumentList)));
                }
                else if (node.Right.IsKind(CS.SyntaxKind.IdentifierName))
                {
                    return VB.SyntaxFactory.QualifiedName(
                        VisitName(node.Left),
                        Visit<VB.Syntax.IdentifierNameSyntax>(node.Right));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            private VB.Syntax.TypeArgumentListSyntax ConvertTypeArguments(
                CS.Syntax.TypeArgumentListSyntax typeArgumentList)
            {
                VB.Syntax.TypeSyntax[] types = typeArgumentList.Arguments.Select(VisitType).ToArray();
                return VB.SyntaxFactory.TypeArgumentList(types);
            }

            public override SyntaxNode VisitTypeParameterList(CS.Syntax.TypeParameterListSyntax node)
            {
                VB.Syntax.TypeParameterSyntax[] parameters = node.Parameters.Select(Visit<VB.Syntax.TypeParameterSyntax>).ToArray();
                return VB.SyntaxFactory.TypeParameterList(parameters);
            }

            private VB.Syntax.TypeParameterListSyntax ConvertTypeParameters(SeparatedSyntaxList<CS.Syntax.TypeParameterSyntax> list)
            {
                VB.Syntax.TypeParameterSyntax[] parameters = list.Select(t =>
                {
                    SyntaxToken variance = t.VarianceKeyword.IsKind(CS.SyntaxKind.None)
                        ? new SyntaxToken()
                        : t.VarianceKeyword.IsKind(CS.SyntaxKind.InKeyword)
                            ? VB.SyntaxFactory.Token(VB.SyntaxKind.InKeyword)
                            : VB.SyntaxFactory.Token(VB.SyntaxKind.OutKeyword);

                    // TODO: get the constraints.
                    return VB.SyntaxFactory.TypeParameter(ConvertIdentifier(t.Identifier)).WithVarianceKeyword(variance);
                }).ToArray();

                return VB.SyntaxFactory.TypeParameterList(parameters);
            }

            public override SyntaxNode VisitNamespaceDeclaration(CS.Syntax.NamespaceDeclarationSyntax node)
            {
                return VB.SyntaxFactory.NamespaceBlock(
                    VB.SyntaxFactory.NamespaceStatement(VisitName(node.Name)),
                    List(node.Members.Select(Visit<VB.Syntax.StatementSyntax>)));
            }

            public override SyntaxNode VisitEnumDeclaration(CS.Syntax.EnumDeclarationSyntax node)
            {
                VB.Syntax.EnumStatementSyntax declaration = VB.SyntaxFactory.EnumStatement(
                    ConvertAttributes(node.AttributeLists),
                    ConvertModifiers(node.Modifiers),
                    ConvertIdentifier(node.Identifier),
                    underlyingType: null);

                return VB.SyntaxFactory.EnumBlock(
                    declaration,
                    List<VB.Syntax.StatementSyntax>(node.Members.Select(Visit<VB.Syntax.EnumMemberDeclarationSyntax>)));
            }

            public override SyntaxNode VisitClassDeclaration(CS.Syntax.ClassDeclarationSyntax node)
            {
                VB.SyntaxKind blockKind;
                VB.SyntaxKind declarationKind;
                VB.SyntaxKind endKind;
                SyntaxToken keyword;
                SyntaxList<VB.Syntax.InheritsStatementSyntax> inherits = List<VB.Syntax.InheritsStatementSyntax>();
                SyntaxList<VB.Syntax.ImplementsStatementSyntax> implements = List<VB.Syntax.ImplementsStatementSyntax>();

                if (node.Modifiers.Any(CS.SyntaxKind.StaticKeyword))
                {
                    blockKind = VB.SyntaxKind.ModuleBlock;
                    declarationKind = VB.SyntaxKind.ModuleStatement;
                    endKind = VB.SyntaxKind.EndModuleStatement;
                    keyword = VB.SyntaxFactory.Token(VB.SyntaxKind.ModuleKeyword);
                }
                else
                {
                    blockKind = VB.SyntaxKind.ClassBlock;
                    declarationKind = VB.SyntaxKind.ClassStatement;
                    endKind = VB.SyntaxKind.EndClassStatement;
                    keyword = VB.SyntaxFactory.Token(VB.SyntaxKind.ClassKeyword);
                }

                if (node.BaseList != null && node.BaseList.Types.Count >= 1)
                {
                    // hack. in C# it's just a list of types.  We can't tell if the first one is a
                    // class or not.  So we just check if it starts with a capital I or not and use
                    // that as a weak enough heuristic.
                    TypeSyntax firstType = node.BaseList.Types[0].Type;
                    SyntaxToken rightName = GetRightmostNamePart(firstType);
                    if (rightName.ValueText.Length >= 2 &&
                        rightName.ValueText[0] == 'I' &&
                        char.IsUpper(rightName.ValueText[1]))
                    {
                        implements = ConvertImplementsList(node.BaseList.Types.Select(t => t.Type));
                    }
                    else
                    {
                        // first type looks like a class
                        inherits = List(
                            VB.SyntaxFactory.InheritsStatement(VB.SyntaxFactory.Token(VB.SyntaxKind.InheritsKeyword), SeparatedList(VisitType(firstType))));

                        implements = ConvertImplementsList(node.BaseList.Types.Skip(1).Select(t => t.Type));
                    }
                }

                return VisitTypeDeclaration(node, blockKind, declarationKind, endKind, keyword, inherits, implements);
            }

            public override SyntaxNode VisitStructDeclaration(CS.Syntax.StructDeclarationSyntax node)
            {
                VB.SyntaxKind blockKind = VB.SyntaxKind.StructureBlock;
                VB.SyntaxKind declarationKind = VB.SyntaxKind.StructureStatement;
                VB.SyntaxKind endKind = VB.SyntaxKind.EndStructureStatement;
                SyntaxToken keyword = VB.SyntaxFactory.Token(VB.SyntaxKind.StructureKeyword);
                SyntaxList<VB.Syntax.ImplementsStatementSyntax> implements = List<VB.Syntax.ImplementsStatementSyntax>();
                if (node.BaseList != null)
                {
                    implements = ConvertImplementsList(node.BaseList.Types.Select(t => t.Type));
                }

                return VisitTypeDeclaration(node, blockKind, declarationKind, endKind, keyword, inherits: List<VB.Syntax.InheritsStatementSyntax>(), implements: implements);
            }

            public override SyntaxNode VisitInterfaceDeclaration(CS.Syntax.InterfaceDeclarationSyntax node)
            {
                VB.SyntaxKind blockKind = VB.SyntaxKind.InterfaceBlock;
                VB.SyntaxKind declarationKind = VB.SyntaxKind.InterfaceStatement;
                VB.SyntaxKind endKind = VB.SyntaxKind.EndInterfaceStatement;
                SyntaxToken keyword = VB.SyntaxFactory.Token(VB.SyntaxKind.InterfaceKeyword);
                SyntaxList<VB.Syntax.InheritsStatementSyntax> inherits = List<VB.Syntax.InheritsStatementSyntax>();
                if (node.BaseList != null)
                {
                    inherits = ConvertInheritsList(node.BaseList.Types.Select(t => t.Type));
                }

                return VisitTypeDeclaration(node, blockKind, declarationKind, endKind, keyword, inherits: inherits, implements: List<VB.Syntax.ImplementsStatementSyntax>());
            }

            private SyntaxNode VisitTypeDeclaration(
                CS.Syntax.TypeDeclarationSyntax node,
                VB.SyntaxKind blockKind, VB.SyntaxKind declarationKind, VB.SyntaxKind endKind,
                SyntaxToken keyword, SyntaxList<VB.Syntax.InheritsStatementSyntax> inherits, SyntaxList<VB.Syntax.ImplementsStatementSyntax> implements)
            {
                SyntaxToken identifier = ConvertIdentifier(node.Identifier);
                VB.Syntax.TypeParameterListSyntax typeParameters = Visit<VB.Syntax.TypeParameterListSyntax>(node.TypeParameterList);

                VB.Syntax.TypeStatementSyntax declaration = VB.SyntaxFactory.TypeStatement(
                    declarationKind,
                    ConvertAttributes(node.AttributeLists),
                    ConvertModifiers(node.Modifiers.Where(t => !t.IsKind(CS.SyntaxKind.StaticKeyword))),
                    keyword,
                    identifier,
                    typeParameters);

                VB.Syntax.TypeBlockSyntax typeBlock = VB.SyntaxFactory.TypeBlock(
                    blockKind,
                    declaration,
                    inherits,
                    implements,
                    List(node.Members.Select(Visit<VB.Syntax.StatementSyntax>)),
                    VB.SyntaxFactory.EndBlockStatement(endKind, VB.SyntaxFactory.Token(VB.SyntaxKind.EndKeyword), keyword));

                SyntaxTrivia docComment = node.GetLeadingTrivia().FirstOrDefault(t => CS.SyntaxFacts.IsDocumentationCommentTrivia(t.Kind()));
                if (!docComment.IsKind(CS.SyntaxKind.None))
                {
                    IEnumerable<SyntaxTrivia> vbDocComment = VisitTrivia(docComment);
                    return typeBlock.WithLeadingTrivia(typeBlock.GetLeadingTrivia().Concat(vbDocComment));
                }
                else
                {
                    return typeBlock;
                }
            }

            private SyntaxToken GetRightmostNamePart(CS.Syntax.TypeSyntax type)
            {
                while (true)
                {
                    if (type.IsKind(CS.SyntaxKind.IdentifierName))
                    {
                        return ((CS.Syntax.IdentifierNameSyntax)type).Identifier;
                    }
                    else if (type.IsKind(CS.SyntaxKind.QualifiedName))
                    {
                        type = ((CS.Syntax.QualifiedNameSyntax)type).Right;
                    }
                    else if (type.IsKind(CS.SyntaxKind.GenericName))
                    {
                        return ((CS.Syntax.GenericNameSyntax)type).Identifier;
                    }
                    else if (type.IsKind(CS.SyntaxKind.AliasQualifiedName))
                    {
                        type = ((CS.Syntax.AliasQualifiedNameSyntax)type).Name;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Unexpected type syntax kind.");
                        return default(SyntaxToken);
                    }
                }
            }

            private SyntaxList<VB.Syntax.ImplementsStatementSyntax> ConvertImplementsList(IEnumerable<CS.Syntax.TypeSyntax> types)
                => List(types.Select(t => VB.SyntaxFactory.ImplementsStatement(VisitType(t))));

            private SyntaxList<VB.Syntax.InheritsStatementSyntax> ConvertInheritsList(IEnumerable<CS.Syntax.TypeSyntax> types)
                => List(types.Select(t => VB.SyntaxFactory.InheritsStatement(VisitType(t))));

            private SyntaxTokenList TokenList(IEnumerable<SyntaxToken> tokens) => VB.SyntaxFactory.TokenList(tokens);

            internal SyntaxTokenList ConvertModifiers(IEnumerable<SyntaxToken> list)
                => TokenList(list.Where(t => !t.IsKind(CS.SyntaxKind.ThisKeyword)).Select(VisitToken));

            public override SyntaxNode VisitMethodDeclaration(CS.Syntax.MethodDeclarationSyntax node)
            {
                bool isVoid = node.ReturnType.IsKind(CS.SyntaxKind.PredefinedType) &&
                    ((CS.Syntax.PredefinedTypeSyntax)node.ReturnType).Keyword.IsKind(CS.SyntaxKind.VoidKeyword);

                VB.Syntax.ImplementsClauseSyntax implementsClause = null;
                if (node.ExplicitInterfaceSpecifier != null)
                {
                    implementsClause = VB.SyntaxFactory.ImplementsClause(
                        VB.SyntaxFactory.QualifiedName(
                            (VB.Syntax.NameSyntax)VisitType(node.ExplicitInterfaceSpecifier.Name),
                            VB.SyntaxFactory.IdentifierName(VisitToken(node.Identifier))));
                }

                VB.Syntax.MethodStatementSyntax begin;

                SyntaxToken identifier = ConvertIdentifier(node.Identifier);
                VB.Syntax.TypeParameterListSyntax typeParameters = Visit<VB.Syntax.TypeParameterListSyntax>(node.TypeParameterList);

                bool isExtension =
                    node.ParameterList.Parameters.Count > 0 &&
                    node.ParameterList.Parameters[0].Modifiers.Any(CS.SyntaxKind.ThisKeyword);

                List<SyntaxToken> modifiers = isExtension
                     ? node.Modifiers.Where(t => !t.IsKind(CS.SyntaxKind.StaticKeyword)).ToList()
                     : node.Modifiers.ToList();

                List<AttributeListSyntax> attributes = isExtension
                    ? node.AttributeLists.Concat(CreateExtensionAttribute()).ToList()
                    : node.AttributeLists.ToList();

                if (isVoid)
                {
                    begin = VB.SyntaxFactory.SubStatement(
                        ConvertAttributes(attributes),
                        ConvertModifiers(modifiers),
                        identifier,
                        typeParameters,
                        Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                        asClause: null,
                        handlesClause: null,
                        implementsClause: implementsClause);
                }
                else
                {
                    SplitAttributes(attributes, out SyntaxList<VB.Syntax.AttributeListSyntax> returnAttributes, out SyntaxList<VB.Syntax.AttributeListSyntax> remainAttributes);

                    begin = VB.SyntaxFactory.FunctionStatement(
                        remainAttributes,
                        ConvertModifiers(modifiers),
                        identifier,
                        typeParameters,
                        Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                        VB.SyntaxFactory.SimpleAsClause(returnAttributes, VisitType(node.ReturnType)),
                        handlesClause: null,
                        implementsClause: implementsClause);
                }

                SyntaxTrivia docComment = node.GetLeadingTrivia().FirstOrDefault(t => CS.SyntaxFacts.IsDocumentationCommentTrivia(t.Kind()));
                if (!docComment.IsKind(CS.SyntaxKind.None))
                {
                    IEnumerable<SyntaxTrivia> vbDocComment = VisitTrivia(docComment);
                    begin = begin.WithLeadingTrivia(begin.GetLeadingTrivia().Concat(vbDocComment));
                }

                if (node.Body == null)
                {
                    return begin;
                }

                if (isVoid)
                {
                    return VB.SyntaxFactory.SubBlock(
                        begin,
                        statementVisitor.VisitStatement(node.Body),
                        VB.SyntaxFactory.EndSubStatement());
                }
                else
                {
                    return VB.SyntaxFactory.FunctionBlock(
                        begin,
                        statementVisitor.VisitStatement(node.Body),
                        VB.SyntaxFactory.EndFunctionStatement());
                }
            }

            private CS.Syntax.AttributeListSyntax CreateExtensionAttribute()
                => CS.SyntaxFactory.AttributeList(
                    attributes: CS.SyntaxFactory.SingletonSeparatedList(
                        CS.SyntaxFactory.Attribute(CS.SyntaxFactory.ParseName("System.Runtime.CompilerServices.Extension"))));

            private void SplitAttributes(
                IList<CS.Syntax.AttributeListSyntax> attributes,
                out SyntaxList<VB.Syntax.AttributeListSyntax> returnAttributes,
                out SyntaxList<VB.Syntax.AttributeListSyntax> remainingAttributes)
            {
                AttributeListSyntax returnAttribute =
                    attributes.FirstOrDefault(a => a.Target != null && a.Target.Identifier.IsKind(CS.SyntaxKind.ReturnKeyword));

                IEnumerable<AttributeListSyntax> rest =
                    attributes.Where(a => a != returnAttribute);

                returnAttributes = List(Visit<VB.Syntax.AttributeListSyntax>(returnAttribute));
                remainingAttributes = ConvertAttributes(rest);
            }

            public override SyntaxNode VisitParameterList(CS.Syntax.ParameterListSyntax node)
                => VB.SyntaxFactory.ParameterList(
                    SeparatedCommaList(node.Parameters.Select(Visit<VB.Syntax.ParameterSyntax>)));

            public override SyntaxNode VisitBracketedParameterList(CS.Syntax.BracketedParameterListSyntax node)
                => VB.SyntaxFactory.ParameterList(
                    SeparatedCommaList(node.Parameters.Select(Visit<VB.Syntax.ParameterSyntax>)));

            public override SyntaxNode VisitParameter(CS.Syntax.ParameterSyntax node)
            {
                VB.Syntax.SimpleAsClauseSyntax asClause = node.Type == null
                    ? null
                    : VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type));
                SyntaxTokenList modifiers = ConvertModifiers(node.Modifiers);
                if (node.Default != null)
                {
                    modifiers = TokenList(modifiers.Concat(VB.SyntaxFactory.Token(VB.SyntaxKind.OptionalKeyword)));
                }

                return VB.SyntaxFactory.Parameter(
                    ConvertAttributes(node.AttributeLists),
                    modifiers,
                    VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(node.Identifier)),
                    asClause,
                    node.Default == null ? null : VB.SyntaxFactory.EqualsValue(VisitExpression(node.Default.Value)));
            }

            public override SyntaxNode VisitGenericName(CS.Syntax.GenericNameSyntax node)
                => VB.SyntaxFactory.GenericName(
                    ConvertIdentifier(node.Identifier),
                    ConvertTypeArguments(node.TypeArgumentList));

            public override SyntaxNode VisitTypeParameter(CS.Syntax.TypeParameterSyntax node)
            {
                SyntaxToken variance = node.VarianceKeyword.IsKind(CS.SyntaxKind.None)
                    ? default(SyntaxToken)
                    : node.VarianceKeyword.IsKind(CS.SyntaxKind.InKeyword)
                        ? VB.SyntaxFactory.Token(VB.SyntaxKind.InKeyword)
                        : VB.SyntaxFactory.Token(VB.SyntaxKind.OutKeyword);

                // TODO: get the constraints.
                return VB.SyntaxFactory.TypeParameter(ConvertIdentifier(node.Identifier)).WithVarianceKeyword(variance);
            }

            public override SyntaxNode VisitPredefinedType(CS.Syntax.PredefinedTypeSyntax node)
            {
                switch (node.Keyword.Kind())
                {
                    case CS.SyntaxKind.BoolKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.BooleanKeyword));
                    case CS.SyntaxKind.ByteKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.ByteKeyword));
                    case CS.SyntaxKind.CharKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.CharKeyword));
                    case CS.SyntaxKind.DecimalKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.DecimalKeyword));
                    case CS.SyntaxKind.DoubleKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.DoubleKeyword));
                    case CS.SyntaxKind.FloatKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.SingleKeyword));
                    case CS.SyntaxKind.IntKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.IntegerKeyword));
                    case CS.SyntaxKind.LongKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.LongKeyword));
                    case CS.SyntaxKind.ObjectKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.ObjectKeyword));
                    case CS.SyntaxKind.SByteKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.SByteKeyword));
                    case CS.SyntaxKind.ShortKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.ShortKeyword));
                    case CS.SyntaxKind.StringKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.StringKeyword));
                    case CS.SyntaxKind.UIntKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.UIntegerKeyword));
                    case CS.SyntaxKind.ULongKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.ULongKeyword));
                    case CS.SyntaxKind.UShortKeyword:
                        return VB.SyntaxFactory.PredefinedType(VB.SyntaxFactory.Token(VB.SyntaxKind.UShortKeyword));
                    case CS.SyntaxKind.VoidKeyword:
                        return VB.SyntaxFactory.IdentifierName(VB.SyntaxFactory.Identifier("Void"));
                    default:
                        throw new NotImplementedException();
                }
            }

            public override SyntaxNode VisitBaseExpression(CS.Syntax.BaseExpressionSyntax node)
                => VB.SyntaxFactory.MyBaseExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.MyBaseKeyword));

            public override SyntaxNode VisitThisExpression(CS.Syntax.ThisExpressionSyntax node)
                => VB.SyntaxFactory.MeExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.MeKeyword));

            public override SyntaxNode VisitLiteralExpression(CS.Syntax.LiteralExpressionSyntax node)
            {
                switch (node.Kind())
                {
                    case CS.SyntaxKind.ArgListExpression:
                        string error = CreateCouldNotBeConvertedString(node.ToFullString(), typeof(SyntaxNode));
                        return VB.SyntaxFactory.StringLiteralExpression(
                            VB.SyntaxFactory.StringLiteralToken(error, error));
                    case CS.SyntaxKind.BaseExpression:
                        return VB.SyntaxFactory.MyBaseExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.MyBaseKeyword));
                    case CS.SyntaxKind.NumericLiteralExpression:
                    case CS.SyntaxKind.StringLiteralExpression:
                    case CS.SyntaxKind.CharacterLiteralExpression:
                    case CS.SyntaxKind.TrueLiteralExpression:
                    case CS.SyntaxKind.FalseLiteralExpression:
                    case CS.SyntaxKind.NullLiteralExpression:
                        return ConvertLiteralExpression(node);
                }

                throw new NotImplementedException();
            }

            private SyntaxNode ConvertLiteralExpression(CS.Syntax.LiteralExpressionSyntax node)
            {
                switch (node.Token.Kind())
                {
                    case CS.SyntaxKind.CharacterLiteralToken:
                        return VB.SyntaxFactory.CharacterLiteralExpression(
                            VB.SyntaxFactory.CharacterLiteralToken("\"" + node.Token.ToString().Substring(1, Math.Max(node.Token.ToString().Length - 2, 0)) + "\"c", (char)node.Token.Value));
                    case CS.SyntaxKind.FalseKeyword:
                        return VB.SyntaxFactory.FalseLiteralExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.FalseKeyword));
                    case CS.SyntaxKind.NullKeyword:
                        return VB.SyntaxFactory.NothingLiteralExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.NothingKeyword));
                    case CS.SyntaxKind.NumericLiteralToken:
                        return ConvertNumericLiteralToken(node);
                    case CS.SyntaxKind.StringLiteralToken:
                        return ConvertStringLiteralExpression(node);
                    case CS.SyntaxKind.TrueKeyword:
                        return VB.SyntaxFactory.TrueLiteralExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.TrueKeyword));
                }

                throw new NotImplementedException();
            }

            private SyntaxNode ConvertNumericLiteralToken(CS.Syntax.LiteralExpressionSyntax node)
            {
                if (node.Token.ToString().StartsWith("0x") ||
                    node.Token.ToString().StartsWith("0X"))
                {
                    return VB.SyntaxFactory.NumericLiteralExpression(
                        VB.SyntaxFactory.IntegerLiteralToken(
                            "&H" + node.Token.ToString().Substring(2).ToUpperInvariant(),
                            VB.Syntax.LiteralBase.Hexadecimal,
                            VB.Syntax.TypeCharacter.None,
                            0));
                }

                // TODO: handle the other numeric types.
                return VB.SyntaxFactory.NumericLiteralExpression(
                    VB.SyntaxFactory.IntegerLiteralToken(node.Token.ToString(), VB.Syntax.LiteralBase.Decimal, VB.Syntax.TypeCharacter.None, 0));
            }

            private SyntaxNode ConvertStringLiteralExpression(CS.Syntax.LiteralExpressionSyntax node)
            {
                int start = this.text.Lines.IndexOf(node.Token.Span.Start);
                int end = this.text.Lines.IndexOf(node.Token.Span.End);

                if (node.Token.IsVerbatimStringLiteral() &&
                    start != end)
                {
                    string text = node.Token.ToString();
                    text = text.Substring(2, text.Length - 3);
                    text = System.Security.SecurityElement.Escape(text);

                    return VB.SyntaxFactory.SimpleMemberAccessExpression(
                        VB.SyntaxFactory.XmlElement(
                            VB.SyntaxFactory.XmlElementStartTag(
                                VB.SyntaxFactory.Token(VB.SyntaxKind.LessThanToken),
                                VB.SyntaxFactory.XmlName(null, VB.SyntaxFactory.XmlNameToken("text", VB.SyntaxKind.XmlNameToken)),
                                VB.SyntaxFactory.List<VB.Syntax.XmlNodeSyntax>(),
                                VB.SyntaxFactory.Token(VB.SyntaxKind.GreaterThanToken)),
                            List<VB.Syntax.XmlNodeSyntax>(VB.SyntaxFactory.XmlText(VB.SyntaxFactory.TokenList(VB.SyntaxFactory.XmlTextLiteralToken(text, text)))),
                            VB.SyntaxFactory.XmlElementEndTag(
                                VB.SyntaxFactory.Token(VB.SyntaxKind.LessThanSlashToken),
                                VB.SyntaxFactory.XmlName(null, VB.SyntaxFactory.XmlNameToken("text", VB.SyntaxKind.XmlNameToken)),
                                VB.SyntaxFactory.Token(VB.SyntaxKind.GreaterThanToken))),
                        VB.SyntaxFactory.Token(VB.SyntaxKind.DotToken),
                        VB.SyntaxFactory.IdentifierName(VB.SyntaxFactory.Identifier("Value")));
                }
                else
                {
                    return VB.SyntaxFactory.StringLiteralExpression(VisitToken(node.Token));
                }
            }

            public override SyntaxNode VisitVariableDeclarator(CS.Syntax.VariableDeclaratorSyntax node)
            {
                TypeSyntax type = node.GetVariableType();
                bool isVar = type is CS.Syntax.IdentifierNameSyntax && ((CS.Syntax.IdentifierNameSyntax)type).Identifier.ValueText == "var";

                VB.Syntax.EqualsValueSyntax initializer = null;
                if (node.Initializer != null)
                {
                    initializer = VB.SyntaxFactory.EqualsValue(VisitExpression(node.Initializer.Value));
                }

                return VB.SyntaxFactory.VariableDeclarator(
                    SeparatedList(VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(node.Identifier))),
                    isVar ? null : VB.SyntaxFactory.SimpleAsClause(VisitType(type)),
                    initializer);
            }

            public override SyntaxNode VisitObjectCreationExpression(CS.Syntax.ObjectCreationExpressionSyntax node)
                => VB.SyntaxFactory.ObjectCreationExpression(
                    default(SyntaxList<VB.Syntax.AttributeListSyntax>),
                    VisitType(node.Type),
                    Visit<VB.Syntax.ArgumentListSyntax>(node.ArgumentList),
                    Visit<VB.Syntax.ObjectCreationInitializerSyntax>(node.Initializer));

            public override SyntaxNode VisitArgumentList(CS.Syntax.ArgumentListSyntax node)
                => VB.SyntaxFactory.ArgumentList(
                    SeparatedCommaList(node.Arguments.Select(Visit<VB.Syntax.ArgumentSyntax>)));

            public override SyntaxNode VisitBracketedArgumentList(CS.Syntax.BracketedArgumentListSyntax node)
                => VB.SyntaxFactory.ArgumentList(
                    SeparatedCommaList(node.Arguments.Select(Visit<VB.Syntax.ArgumentSyntax>)));

            public override SyntaxNode VisitArgument(CS.Syntax.ArgumentSyntax node)
            {
                if (node.NameColon == null)
                {
                    return VB.SyntaxFactory.SimpleArgument(VisitExpression(node.Expression));
                }
                else
                {
                    return VB.SyntaxFactory.SimpleArgument(
                               VB.SyntaxFactory.NameColonEquals(
                                   VB.SyntaxFactory.IdentifierName(ConvertIdentifier(node.NameColon.Name))),
                               VisitExpression(node.Expression));
                }
            }

            public override SyntaxNode VisitInvocationExpression(CS.Syntax.InvocationExpressionSyntax node)
                => VB.SyntaxFactory.InvocationExpression(
                    VisitExpression(node.Expression),
                    Visit<VB.Syntax.ArgumentListSyntax>(node.ArgumentList));

            public override SyntaxNode VisitFieldDeclaration(CS.Syntax.FieldDeclarationSyntax node)
            {
                SyntaxTokenList modifiers = ConvertModifiers(node.Modifiers);
                if (modifiers.Count == 0)
                {
                    modifiers = VB.SyntaxFactory.TokenList(VB.SyntaxFactory.Token(VB.SyntaxKind.DimKeyword));
                }

                return VB.SyntaxFactory.FieldDeclaration(
                    ConvertAttributes(node.AttributeLists),
                    modifiers,
                    SeparatedCommaList(node.Declaration.Variables.Select(Visit<VB.Syntax.VariableDeclaratorSyntax>)));
            }

            public override SyntaxNode VisitConstructorDeclaration(CS.Syntax.ConstructorDeclarationSyntax node)
            {
                VB.Syntax.SubNewStatementSyntax declaration = VB.SyntaxFactory.SubNewStatement(
                    ConvertAttributes(node.AttributeLists),
                    ConvertModifiers(node.Modifiers),
                    Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList));

                if (node.Body == null)
                {
                    return declaration;
                }

                return VB.SyntaxFactory.ConstructorBlock(
                    declaration,
                    statementVisitor.VisitStatement(node.Body),
                    VB.SyntaxFactory.EndSubStatement());
            }

            public override SyntaxNode VisitMemberAccessExpression(CS.Syntax.MemberAccessExpressionSyntax node)
            {
                if (node.Name.IsKind(CS.SyntaxKind.IdentifierName))
                {
                    return VB.SyntaxFactory.SimpleMemberAccessExpression(
                        VisitExpression(node.Expression),
                        VB.SyntaxFactory.Token(VB.SyntaxKind.DotToken),
                        VB.SyntaxFactory.IdentifierName(ConvertIdentifier((CS.Syntax.IdentifierNameSyntax)node.Name)));
                }
                else if (node.Name.IsKind(CS.SyntaxKind.GenericName))
                {
                    GenericNameSyntax genericName = (CS.Syntax.GenericNameSyntax)node.Name;
                    return VB.SyntaxFactory.SimpleMemberAccessExpression(
                        VisitExpression(node.Expression),
                        VB.SyntaxFactory.Token(VB.SyntaxKind.DotToken),
                        VB.SyntaxFactory.GenericName(ConvertIdentifier(genericName.Identifier),
                        ConvertTypeArguments(genericName.TypeArgumentList)));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            public override SyntaxNode VisitBinaryExpression(CS.Syntax.BinaryExpressionSyntax node)
            {
                VB.Syntax.ExpressionSyntax left = VisitExpression(node.Left);
                VB.Syntax.ExpressionSyntax right = VisitExpression(node.Right);

                switch (node.OperatorToken.Kind())
                {
                    case CS.SyntaxKind.AmpersandAmpersandToken:
                        return VB.SyntaxFactory.AndAlsoExpression(left, right);
                    case CS.SyntaxKind.AmpersandToken:
                        return VB.SyntaxFactory.AndExpression(left, right);

                    case CS.SyntaxKind.AsKeyword:
                        return VB.SyntaxFactory.TryCastExpression(left, (VB.Syntax.TypeSyntax)right);
                    case CS.SyntaxKind.AsteriskToken:
                        return VB.SyntaxFactory.MultiplyExpression(left, right);
                    case CS.SyntaxKind.AsteriskEqualsToken:
                        return VB.SyntaxFactory.MultiplyAssignmentStatement(left, VB.SyntaxFactory.Token(VB.SyntaxKind.AsteriskEqualsToken), right);
                    case CS.SyntaxKind.BarToken:
                        return VB.SyntaxFactory.OrExpression(left, right);
                    case CS.SyntaxKind.BarBarToken:
                        return VB.SyntaxFactory.OrElseExpression(left, right);

                    case CS.SyntaxKind.CaretToken:
                        return VB.SyntaxFactory.ExclusiveOrExpression(left, right);

                    case CS.SyntaxKind.MinusToken:
                        return VB.SyntaxFactory.SubtractExpression(left, right);

                    case CS.SyntaxKind.EqualsEqualsToken:
                        if (node.Right.IsKind(CS.SyntaxKind.NullLiteralExpression))
                        {
                            return VB.SyntaxFactory.IsExpression(left, right);
                        }
                        else
                        {
                            return VB.SyntaxFactory.EqualsExpression(left, right);
                        }

                    case CS.SyntaxKind.ExclamationEqualsToken:
                        if (node.Right.IsKind(CS.SyntaxKind.NullLiteralExpression))
                        {
                            return VB.SyntaxFactory.IsNotExpression(left, right);
                        }
                        else
                        {
                            return VB.SyntaxFactory.NotEqualsExpression(left, right);
                        }

                    case CS.SyntaxKind.GreaterThanToken:
                        return VB.SyntaxFactory.GreaterThanExpression(left, right);
                    case CS.SyntaxKind.GreaterThanEqualsToken:
                        return VB.SyntaxFactory.GreaterThanOrEqualExpression(left, right);
                    case CS.SyntaxKind.GreaterThanGreaterThanToken:
                        return VB.SyntaxFactory.RightShiftExpression(left, right);

                    case CS.SyntaxKind.IsKeyword:
                        return VB.SyntaxFactory.TypeOfIsExpression(left, (VB.Syntax.TypeSyntax)right);

                    case CS.SyntaxKind.LessThanToken:
                        return VB.SyntaxFactory.LessThanExpression(left, right);
                    case CS.SyntaxKind.LessThanEqualsToken:
                        return VB.SyntaxFactory.LessThanOrEqualExpression(left, right);
                    case CS.SyntaxKind.LessThanLessThanToken:
                        return VB.SyntaxFactory.LeftShiftExpression(left, right);
                    case CS.SyntaxKind.PercentToken:
                        return VB.SyntaxFactory.ModuloExpression(left, right);

                    case CS.SyntaxKind.PlusToken:
                        return VB.SyntaxFactory.AddExpression(left, right);
                    case CS.SyntaxKind.QuestionToken:
                    case CS.SyntaxKind.QuestionQuestionToken:
                        {
                            VB.Syntax.ArgumentSyntax[] args = new VB.Syntax.ArgumentSyntax[] { VB.SyntaxFactory.SimpleArgument(left), VB.SyntaxFactory.SimpleArgument(right) };
                            SeparatedSyntaxList<VB.Syntax.ArgumentSyntax> arguments = SeparatedCommaList(args);
                            return VB.SyntaxFactory.InvocationExpression(
                                VB.SyntaxFactory.IdentifierName(VB.SyntaxFactory.Identifier("If")),
                                VB.SyntaxFactory.ArgumentList(arguments));
                        }

                    case CS.SyntaxKind.SlashToken:
                        return VB.SyntaxFactory.DivideExpression(left, right);
                }

                throw new NotImplementedException();
            }

            public override SyntaxNode VisitAssignmentExpression(CS.Syntax.AssignmentExpressionSyntax node)
            {
                VB.Syntax.ExpressionSyntax left = VisitExpression(node.Left);
                VB.Syntax.ExpressionSyntax right = VisitExpression(node.Right);

                switch (node.OperatorToken.Kind())
                {
                    case CS.SyntaxKind.AmpersandEqualsToken:
                        {
                            VB.Syntax.ExpressionSyntax left2 = VisitExpression(node.Left);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                left,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.AndAlsoExpression(left2, right));
                        }

                    case CS.SyntaxKind.AsteriskEqualsToken:
                        return VB.SyntaxFactory.MultiplyAssignmentStatement(left, VB.SyntaxFactory.Token(VB.SyntaxKind.AsteriskEqualsToken), right);

                    case CS.SyntaxKind.BarEqualsToken:
                        {
                            VB.Syntax.ExpressionSyntax left2 = VisitExpression(node.Left);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                left,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.OrElseExpression(left2, right));
                        }

                    case CS.SyntaxKind.CaretEqualsToken:
                        {
                            VB.Syntax.ExpressionSyntax left2 = VisitExpression(node.Left);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                left,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.ExclusiveOrExpression(left2, right));
                        }

                    case CS.SyntaxKind.MinusEqualsToken:
                        return VB.SyntaxFactory.SubtractAssignmentStatement(left, VB.SyntaxFactory.Token(VB.SyntaxKind.MinusEqualsToken), right);

                    case CS.SyntaxKind.EqualsToken:
                        return VB.SyntaxFactory.SimpleAssignmentStatement(left, VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken), right);

                    case CS.SyntaxKind.GreaterThanGreaterThanEqualsToken:
                        {
                            VB.Syntax.ExpressionSyntax left2 = VisitExpression(node.Left);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                left,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.RightShiftExpression(left2, right));
                        }

                    case CS.SyntaxKind.LessThanLessThanEqualsToken:
                        {
                            VB.Syntax.ExpressionSyntax left2 = VisitExpression(node.Left);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                left,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.LeftShiftExpression(left2, right));
                        }

                    case CS.SyntaxKind.PercentEqualsToken:
                        {
                            VB.Syntax.ExpressionSyntax left2 = VisitExpression(node.Left);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                left,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.ModuloExpression(left2, right));
                        }

                    case CS.SyntaxKind.PlusEqualsToken:
                        return VB.SyntaxFactory.AddAssignmentStatement(left, VB.SyntaxFactory.Token(VB.SyntaxKind.PlusEqualsToken), right);

                    case CS.SyntaxKind.SlashEqualsToken:
                        return VB.SyntaxFactory.DivideAssignmentStatement(left, VB.SyntaxFactory.Token(VB.SyntaxKind.SlashEqualsToken), right);
                }

                throw new NotImplementedException();
            }

            public override SyntaxNode VisitElseClause(CS.Syntax.ElseClauseSyntax node)
                => VB.SyntaxFactory.ElseBlock(
                    VB.SyntaxFactory.ElseStatement(),
                    statementVisitor.VisitStatement(node.Statement));

            public override SyntaxNode VisitSwitchSection(CS.Syntax.SwitchSectionSyntax node)
                => VB.SyntaxFactory.CaseBlock(
                    VB.SyntaxFactory.CaseStatement(
                        SeparatedCommaList(node.Labels.Select(Visit<VB.Syntax.CaseClauseSyntax>))),
                    List<VB.Syntax.StatementSyntax>(
                        node.Statements.SelectMany(statementVisitor.VisitStatementEnumerable)));

            public override SyntaxNode VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
                => VB.SyntaxFactory.SimpleCaseClause(VisitExpression(node.Value));

            public override SyntaxNode VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node)
                => VB.SyntaxFactory.ElseCaseClause();

            public override SyntaxNode VisitCastExpression(CS.Syntax.CastExpressionSyntax node)
            {
                // todo: need to handle CInt and all those other cases.
                return VB.SyntaxFactory.DirectCastExpression(
                    VisitExpression(node.Expression),
                    VisitType(node.Type));
            }

            public override SyntaxNode VisitParenthesizedLambdaExpression(CS.Syntax.ParenthesizedLambdaExpressionSyntax node)
            {
                VB.Syntax.ParameterListSyntax parameters = Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList);
                VB.Syntax.LambdaHeaderSyntax lambdaHeader = VB.SyntaxFactory.FunctionLambdaHeader(
                    new SyntaxList<VB.Syntax.AttributeListSyntax>(),
                    node.AsyncKeyword.IsKind(CS.SyntaxKind.None) ? VB.SyntaxFactory.TokenList() : VB.SyntaxFactory.TokenList(VisitToken(node.AsyncKeyword)),
                    parameters,
                    null);
                if (node.Body.IsKind(CS.SyntaxKind.Block))
                {
                    return VB.SyntaxFactory.MultiLineFunctionLambdaExpression(
                        lambdaHeader,
                        statementVisitor.VisitStatement((CS.Syntax.BlockSyntax)node.Body),
                        VB.SyntaxFactory.EndFunctionStatement());
                }
                else
                {
                    return VB.SyntaxFactory.SingleLineFunctionLambdaExpression(
                        lambdaHeader,
                        (VB.VisualBasicSyntaxNode)Visit<SyntaxNode>(node.Body));
                }
            }

            public override SyntaxNode VisitSimpleLambdaExpression(CS.Syntax.SimpleLambdaExpressionSyntax node)
            {
                VB.Syntax.ParameterSyntax parameter = VB.SyntaxFactory.Parameter(
                    VB.SyntaxFactory.ModifiedIdentifier(
                        ConvertIdentifier(node.Parameter.Identifier)));

                VB.Syntax.LambdaHeaderSyntax lambdaHeader = VB.SyntaxFactory.FunctionLambdaHeader(
                    new SyntaxList<VB.Syntax.AttributeListSyntax>(),
                    node.AsyncKeyword.IsKind(CS.SyntaxKind.None) ? VB.SyntaxFactory.TokenList() : VB.SyntaxFactory.TokenList(VisitToken(node.AsyncKeyword)),
                    VB.SyntaxFactory.ParameterList(SeparatedList(parameter)),
                    null);
                if (node.Body.IsKind(CS.SyntaxKind.Block))
                {
                    return VB.SyntaxFactory.MultiLineFunctionLambdaExpression(
                        lambdaHeader,
                        statementVisitor.VisitStatement((CS.Syntax.BlockSyntax)node.Body),
                        VB.SyntaxFactory.EndFunctionStatement());
                }
                else
                {
                    return VB.SyntaxFactory.SingleLineFunctionLambdaExpression(
                        lambdaHeader,
                        (VB.VisualBasicSyntaxNode)Visit<SyntaxNode>(node.Body));
                }
            }

            public override SyntaxNode VisitConditionalExpression(CS.Syntax.ConditionalExpressionSyntax node)
            {
                VB.Syntax.ArgumentSyntax[] argumentsArray = new VB.Syntax.ArgumentSyntax[]
                    {
                        VB.SyntaxFactory.SimpleArgument(VisitExpression(node.Condition)),
                        VB.SyntaxFactory.SimpleArgument(VisitExpression(node.WhenTrue)),
                        VB.SyntaxFactory.SimpleArgument(VisitExpression(node.WhenFalse))
                    };

                return VB.SyntaxFactory.InvocationExpression(
                    VB.SyntaxFactory.IdentifierName(VB.SyntaxFactory.Identifier("If")),
                    VB.SyntaxFactory.ArgumentList(SeparatedCommaList(argumentsArray)));
            }

            public override SyntaxNode VisitElementAccessExpression(CS.Syntax.ElementAccessExpressionSyntax node)
                => VB.SyntaxFactory.InvocationExpression(
                    VisitExpression(node.Expression),
                    Visit<VB.Syntax.ArgumentListSyntax>(node.ArgumentList));

            public override SyntaxNode VisitParenthesizedExpression(CS.Syntax.ParenthesizedExpressionSyntax node)
                => VB.SyntaxFactory.ParenthesizedExpression(
                    VisitExpression(node.Expression));

            public override SyntaxNode VisitImplicitArrayCreationExpression(CS.Syntax.ImplicitArrayCreationExpressionSyntax node)
                => Visit<VB.Syntax.CollectionInitializerSyntax>(node.Initializer);

            public override SyntaxNode VisitInitializerExpression(CS.Syntax.InitializerExpressionSyntax node)
            {
                if (node.Parent.IsKind(CS.SyntaxKind.AnonymousObjectCreationExpression))
                {
                    List<VB.Syntax.FieldInitializerSyntax> fieldInitializers = new List<VB.Syntax.FieldInitializerSyntax>();
                    foreach (ExpressionSyntax expression in node.Expressions)
                    {
                        if (expression.IsKind(CS.SyntaxKind.SimpleAssignmentExpression))
                        {
                            AssignmentExpressionSyntax assignment = (CS.Syntax.AssignmentExpressionSyntax)expression;
                            if (assignment.Left.IsKind(CS.SyntaxKind.IdentifierName))
                            {
                                fieldInitializers.Add(VB.SyntaxFactory.NamedFieldInitializer(
                                    VB.SyntaxFactory.IdentifierName(ConvertIdentifier((CS.Syntax.IdentifierNameSyntax)assignment.Left)),
                                    VisitExpression(assignment.Right)));
                                continue;
                            }
                        }

                        fieldInitializers.Add(VB.SyntaxFactory.InferredFieldInitializer(VisitExpression(expression)));
                    }

                    return VB.SyntaxFactory.ObjectMemberInitializer(fieldInitializers.ToArray());
                }
                else if (node.Parent.IsKind(CS.SyntaxKind.ObjectCreationExpression))
                {
                    if (node.Expressions.Count > 0 &&
                        node.Expressions[0].IsKind(CS.SyntaxKind.SimpleAssignmentExpression))
                    {
                        List<VB.Syntax.FieldInitializerSyntax> initializers = new List<VB.Syntax.FieldInitializerSyntax>();
                        foreach (ExpressionSyntax e in node.Expressions)
                        {
                            if (e.IsKind(CS.SyntaxKind.SimpleAssignmentExpression))
                            {
                                AssignmentExpressionSyntax assignment = (CS.Syntax.AssignmentExpressionSyntax)e;
                                if (assignment.Left.IsKind(CS.SyntaxKind.IdentifierName))
                                {
                                    initializers.Add(
                                        VB.SyntaxFactory.NamedFieldInitializer(
                                            Visit<VB.Syntax.IdentifierNameSyntax>(assignment.Left),
                                            VisitExpression(assignment.Right)));
                                    continue;
                                }
                            }

                            initializers.Add(
                                VB.SyntaxFactory.InferredFieldInitializer(VisitExpression(e)));
                        }

                        return VB.SyntaxFactory.ObjectMemberInitializer(initializers.ToArray());
                    }
                    else
                    {
                        return VB.SyntaxFactory.ObjectCollectionInitializer(
                            VB.SyntaxFactory.CollectionInitializer(
                                SeparatedCommaList(node.Expressions.Select(VisitExpression))));
                    }
                }
                else
                {
                    return VB.SyntaxFactory.CollectionInitializer(
                        SeparatedCommaList(node.Expressions.Select(VisitExpression)));
                }
            }

            public override SyntaxNode VisitForEachStatement(CS.Syntax.ForEachStatementSyntax node)
            {
                VB.Syntax.ForEachStatementSyntax begin = VB.SyntaxFactory.ForEachStatement(
                    VB.SyntaxFactory.IdentifierName(ConvertIdentifier(node.Identifier)),
                    VisitExpression(node.Expression));
                return VB.SyntaxFactory.ForEachBlock(
                    begin,
                    statementVisitor.VisitStatement(node.Statement),
                    VB.SyntaxFactory.NextStatement());
            }

            public override SyntaxNode VisitAttributeList(CS.Syntax.AttributeListSyntax node)
                => VB.SyntaxFactory.AttributeList(
                    SeparatedCommaList(node.Attributes.Select(Visit<VB.Syntax.AttributeSyntax>)));

            public override SyntaxNode VisitAttribute(CS.Syntax.AttributeSyntax node)
            {
                AttributeListSyntax parent = (CS.Syntax.AttributeListSyntax)node.Parent;
                return VB.SyntaxFactory.Attribute(
                    Visit<VB.Syntax.AttributeTargetSyntax>(parent.Target),
                    VisitType(node.Name),
                    Visit<VB.Syntax.ArgumentListSyntax>(node.ArgumentList));
            }

            public override SyntaxNode VisitAttributeTargetSpecifier(CS.Syntax.AttributeTargetSpecifierSyntax node)
            {
                // todo: any other types of attribute targets (like 'return', etc.) 
                // should cause us to actually move the attribute to a different 
                // location in the VB signature.
                //
                // For now, we only handle assembly/module.

                switch (node.Identifier.ValueText)
                {
                    default:
                        return null;
                    case "assembly":
                    case "module":
                        SyntaxToken modifier = VisitToken(node.Identifier);
                        return VB.SyntaxFactory.AttributeTarget(
                            modifier,
                            VB.SyntaxFactory.Token(VB.SyntaxKind.ColonToken));
                }
            }

            public override SyntaxNode VisitAttributeArgumentList(CS.Syntax.AttributeArgumentListSyntax node)
                => VB.SyntaxFactory.ArgumentList(SeparatedCommaList(node.Arguments.Select(Visit<VB.Syntax.ArgumentSyntax>)));

            public override SyntaxNode VisitAttributeArgument(CS.Syntax.AttributeArgumentSyntax node)
            {
                if (node.NameEquals == null)
                {
                    return VB.SyntaxFactory.SimpleArgument(VisitExpression(node.Expression));
                }
                else
                {
                    return VB.SyntaxFactory.SimpleArgument(
                               VB.SyntaxFactory.NameColonEquals(
                                   VB.SyntaxFactory.IdentifierName(ConvertIdentifier(node.NameEquals.Name))),
                                   VisitExpression(node.Expression));
                }
            }

            public override SyntaxNode VisitPropertyDeclaration(CS.Syntax.PropertyDeclarationSyntax node)
            {
                SyntaxTokenList modifiers = ConvertModifiers(node.Modifiers);
                if (node.AccessorList.Accessors.Count == 1)
                {
                    if (node.AccessorList.Accessors[0].Keyword.IsKind(CS.SyntaxKind.GetKeyword))
                    {
                        modifiers = TokenList(modifiers.Concat(VB.SyntaxFactory.Token(VB.SyntaxKind.ReadOnlyKeyword)));
                    }
                    else
                    {
                        modifiers = TokenList(modifiers.Concat(VB.SyntaxFactory.Token(VB.SyntaxKind.WriteOnlyKeyword)));
                    }
                }

                VB.Syntax.PropertyStatementSyntax begin = VB.SyntaxFactory.PropertyStatement(
                    ConvertAttributes(node.AttributeLists),
                    modifiers,
                    ConvertIdentifier(node.Identifier),
                    null,
                    VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)),
                    null,
                    null);

                if (node.AccessorList.Accessors.All(a => a.Body == null))
                {
                    return begin;
                }

                return VB.SyntaxFactory.PropertyBlock(
                    begin,
                    List(node.AccessorList.Accessors.Select(Visit<VB.Syntax.AccessorBlockSyntax>)));
            }

            public override SyntaxNode VisitIndexerDeclaration(CS.Syntax.IndexerDeclarationSyntax node)
            {
                VB.Syntax.PropertyStatementSyntax begin = VB.SyntaxFactory.PropertyStatement(
                    ConvertAttributes(node.AttributeLists),
                    ConvertModifiers(node.Modifiers),
                    VB.SyntaxFactory.Identifier("Item"),
                    Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                    VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)),
                    null,
                    null);

                return VB.SyntaxFactory.PropertyBlock(
                    begin,
                    List(node.AccessorList.Accessors.Select(Visit<VB.Syntax.AccessorBlockSyntax>)));
            }

            public override SyntaxNode VisitAccessorDeclaration(CS.Syntax.AccessorDeclarationSyntax node)
            {
                SyntaxList<VB.Syntax.AttributeListSyntax> attributes = ConvertAttributes(node.AttributeLists);
                SyntaxTokenList modifiers = ConvertModifiers(node.Modifiers);
                SyntaxList<VB.Syntax.StatementSyntax> body = statementVisitor.VisitStatement(node.Body);

                switch (node.Kind())
                {
                    case CS.SyntaxKind.AddAccessorDeclaration:
                        {
                            VB.Syntax.AccessorStatementSyntax begin = VB.SyntaxFactory.AddHandlerAccessorStatement(
                                attributes, modifiers, null);

                            return VB.SyntaxFactory.AddHandlerAccessorBlock(
                                begin, body,
                                VB.SyntaxFactory.EndAddHandlerStatement());
                        }

                    case CS.SyntaxKind.GetAccessorDeclaration:
                        {
                            VB.Syntax.AccessorStatementSyntax begin = VB.SyntaxFactory.GetAccessorStatement(
                                attributes, modifiers, null);

                            return VB.SyntaxFactory.GetAccessorBlock(
                                begin, body,
                                VB.SyntaxFactory.EndGetStatement());
                        }

                    case CS.SyntaxKind.RemoveAccessorDeclaration:
                        {
                            VB.Syntax.AccessorStatementSyntax begin = VB.SyntaxFactory.RemoveHandlerAccessorStatement(
                                attributes, modifiers, null);

                            return VB.SyntaxFactory.RemoveHandlerAccessorBlock(
                                begin, body,
                                VB.SyntaxFactory.EndRemoveHandlerStatement());
                        }

                    case CS.SyntaxKind.SetAccessorDeclaration:
                        {
                            VB.Syntax.AccessorStatementSyntax begin = VB.SyntaxFactory.SetAccessorStatement(
                                attributes, modifiers, null);

                            return VB.SyntaxFactory.SetAccessorBlock(
                                begin, body,
                                VB.SyntaxFactory.EndSetStatement());
                        }

                    default:
                        throw new NotImplementedException();
                }
            }

            // private static readonly Regex SpaceSlashSlashSlashRegex =
            //    new Regex("^(\\s*)///(.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
            // private static readonly Regex SpaceStarRegex =
            //    new Regex("^(\\s*)((/\\*\\*)|(\\*)|(\\*/))(.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
            // private static readonly char[] LineSeparators = { '\r', '\n', '\u0085', '\u2028', '\u2029' };

            public override SyntaxNode VisitDocumentationCommentTrivia(CS.Syntax.DocumentationCommentTriviaSyntax node)
            {
                string text = node.ToFullString().Replace("///", "'''");
                VB.Syntax.CompilationUnitSyntax root = VB.SyntaxFactory.ParseSyntaxTree(text).GetRoot() as VB.Syntax.CompilationUnitSyntax;
                return root.EndOfFileToken.LeadingTrivia.ElementAt(0).GetStructure();
            }

            public override SyntaxNode VisitArrayType(CS.Syntax.ArrayTypeSyntax node)
                => VB.SyntaxFactory.ArrayType(
                    VisitType(node.ElementType),
                    List(node.RankSpecifiers.Select(Visit<VB.Syntax.ArrayRankSpecifierSyntax>)));

            public override SyntaxNode VisitArrayRankSpecifier(CS.Syntax.ArrayRankSpecifierSyntax node)
            {
                // TODO: pass the right number of commas
                return VB.SyntaxFactory.ArrayRankSpecifier();
            }

            public override SyntaxNode VisitArrayCreationExpression(CS.Syntax.ArrayCreationExpressionSyntax node)
            {
                VB.Syntax.CollectionInitializerSyntax initializer = Visit<VB.Syntax.CollectionInitializerSyntax>(node.Initializer);
                if (initializer == null)
                {
                    initializer = VB.SyntaxFactory.CollectionInitializer();
                }

                VB.Syntax.ArrayTypeSyntax arrayType = (VB.Syntax.ArrayTypeSyntax)VisitType(node.Type);

                return VB.SyntaxFactory.ArrayCreationExpression(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.NewKeyword),
                    new SyntaxList<VB.Syntax.AttributeListSyntax>(),
                    arrayType.ElementType,
                    null,
                    arrayType.RankSpecifiers,
                    initializer);
            }

            public override SyntaxNode VisitVariableDeclaration(CS.Syntax.VariableDeclarationSyntax node)
                => VB.SyntaxFactory.LocalDeclarationStatement(
                    VB.SyntaxFactory.TokenList(VB.SyntaxFactory.Token(VB.SyntaxKind.DimKeyword)),
                    SeparatedCommaList(node.Variables.Select(Visit<VB.Syntax.VariableDeclaratorSyntax>)));

            public override SyntaxNode VisitPostfixUnaryExpression(CS.Syntax.PostfixUnaryExpressionSyntax node)
            {
                VB.Syntax.ExpressionSyntax operand = VisitExpression(node.Operand);

                switch (node.Kind())
                {
                    case CS.SyntaxKind.PostIncrementExpression:
                        return VB.SyntaxFactory.SimpleAssignmentStatement(
                            operand,
                            VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                            VB.SyntaxFactory.AddExpression(operand,
                                VB.SyntaxFactory.NumericLiteralExpression(VB.SyntaxFactory.IntegerLiteralToken("1", VB.Syntax.LiteralBase.Decimal, VB.Syntax.TypeCharacter.None, 1))));
                    case CS.SyntaxKind.PostDecrementExpression:
                        return VB.SyntaxFactory.SimpleAssignmentStatement(
                            operand,
                            VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                            VB.SyntaxFactory.SubtractExpression(operand,
                                VB.SyntaxFactory.NumericLiteralExpression(VB.SyntaxFactory.IntegerLiteralToken("1", VB.Syntax.LiteralBase.Decimal, VB.Syntax.TypeCharacter.None, 1))));
                }

                throw new NotImplementedException();
            }

            public override SyntaxNode VisitOperatorDeclaration(CS.Syntax.OperatorDeclarationSyntax node)
            {
                SyntaxToken @operator;
                switch (node.OperatorToken.Kind())
                {
                    case CS.SyntaxKind.AmpersandToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.AndKeyword);
                        break;
                    case CS.SyntaxKind.AmpersandAmpersandToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.AndAlsoKeyword);
                        break;
                    case CS.SyntaxKind.AsteriskToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.AsteriskToken);
                        break;
                    case CS.SyntaxKind.BarToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.OrKeyword);
                        break;
                    case CS.SyntaxKind.CaretToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.XorKeyword);
                        break;
                    case CS.SyntaxKind.MinusToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.MinusToken);
                        break;
                    case CS.SyntaxKind.MinusMinusToken:
                        @operator = VB.SyntaxFactory.Identifier("Decrement");
                        break;
                    case CS.SyntaxKind.EqualsEqualsToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken);
                        break;
                    case CS.SyntaxKind.FalseKeyword:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.IsFalseKeyword);
                        break;
                    case CS.SyntaxKind.ExclamationToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.NotKeyword);
                        break;
                    case CS.SyntaxKind.ExclamationEqualsToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.LessThanGreaterThanToken);
                        break;
                    case CS.SyntaxKind.GreaterThanToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.GreaterThanToken);
                        break;
                    case CS.SyntaxKind.GreaterThanGreaterThanToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.GreaterThanGreaterThanToken);
                        break;
                    case CS.SyntaxKind.GreaterThanEqualsToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.GreaterThanEqualsToken);
                        break;
                    case CS.SyntaxKind.LessThanToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.LessThanToken);
                        break;
                    case CS.SyntaxKind.LessThanEqualsToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.LessThanEqualsToken);
                        break;
                    case CS.SyntaxKind.LessThanLessThanToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.LessThanLessThanToken);
                        break;
                    case CS.SyntaxKind.PercentToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.ModKeyword);
                        break;
                    case CS.SyntaxKind.PlusToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.PlusToken);
                        break;
                    case CS.SyntaxKind.PlusPlusToken:
                        @operator = VB.SyntaxFactory.Identifier("Increment");
                        break;
                    case CS.SyntaxKind.SlashToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.SlashToken);
                        break;
                    case CS.SyntaxKind.TildeToken:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.NotKeyword);
                        break;
                    case CS.SyntaxKind.TrueKeyword:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.IsTrueKeyword);
                        break;
                    case CS.SyntaxKind.ExplicitKeyword:
                    case CS.SyntaxKind.ImplicitKeyword:
                    case CS.SyntaxKind.None:
                        @operator = VB.SyntaxFactory.Token(VB.SyntaxKind.EmptyToken, node.OperatorToken.ToString());
                        break;
                    default:
                        throw new NotImplementedException();
                }
                SplitAttributes(node.AttributeLists.ToList(), out SyntaxList<VB.Syntax.AttributeListSyntax> returnAttributes, out SyntaxList<VB.Syntax.AttributeListSyntax> remainingAttributes);

                VB.Syntax.OperatorStatementSyntax begin = VB.SyntaxFactory.OperatorStatement(
                    remainingAttributes,
                    ConvertModifiers(node.Modifiers),
                    @operator,
                    Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                    VB.SyntaxFactory.SimpleAsClause(returnAttributes, VisitType(node.ReturnType)));

                if (node.Body == null)
                {
                    return begin;
                }

                return VB.SyntaxFactory.OperatorBlock(
                    begin,
                    statementVisitor.VisitStatement(node.Body),
                    VB.SyntaxFactory.EndOperatorStatement());
            }

            public override SyntaxNode VisitPrefixUnaryExpression(CS.Syntax.PrefixUnaryExpressionSyntax node)
            {
                switch (node.Kind())
                {
                    case CS.SyntaxKind.AddressOfExpression:
                        return VB.SyntaxFactory.AddressOfExpression(VisitExpression(node.Operand));
                    case CS.SyntaxKind.BitwiseNotExpression:
                    case CS.SyntaxKind.LogicalNotExpression:
                        return VB.SyntaxFactory.NotExpression(VisitExpression(node.Operand));
                    case CS.SyntaxKind.UnaryMinusExpression:
                        return VB.SyntaxFactory.UnaryMinusExpression(VisitExpression(node.Operand));
                    case CS.SyntaxKind.UnaryPlusExpression:
                        return VB.SyntaxFactory.UnaryPlusExpression(VisitExpression(node.Operand));
                    case CS.SyntaxKind.PointerIndirectionExpression:
                        return Visit<SyntaxNode>(node.Operand);
                    case CS.SyntaxKind.PreDecrementExpression:
                        {
                            VB.Syntax.ExpressionSyntax operand = VisitExpression(node.Operand);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                operand,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.SubtractExpression(operand, VB.SyntaxFactory.NumericLiteralExpression(
                                    VB.SyntaxFactory.IntegerLiteralToken("1", VB.Syntax.LiteralBase.Decimal, VB.Syntax.TypeCharacter.None, 1))));
                        }

                    case CS.SyntaxKind.PreIncrementExpression:
                        {
                            VB.Syntax.ExpressionSyntax operand = VisitExpression(node.Operand);
                            return VB.SyntaxFactory.SimpleAssignmentStatement(
                                operand,
                                VB.SyntaxFactory.Token(VB.SyntaxKind.EqualsToken),
                                VB.SyntaxFactory.AddExpression(operand, VB.SyntaxFactory.NumericLiteralExpression(
                                    VB.SyntaxFactory.IntegerLiteralToken("1", VB.Syntax.LiteralBase.Decimal, VB.Syntax.TypeCharacter.None, 1))));
                        }

                    default:
                        throw new NotImplementedException();
                }
            }

            public override SyntaxNode VisitAwaitExpression(CS.Syntax.AwaitExpressionSyntax node)
                => VB.SyntaxFactory.AwaitExpression(VisitExpression(node.Expression));

            public override SyntaxNode VisitDefaultExpression(CS.Syntax.DefaultExpressionSyntax node)
                => VB.SyntaxFactory.NothingLiteralExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.NothingKeyword));

            public override SyntaxNode VisitTypeOfExpression(CS.Syntax.TypeOfExpressionSyntax node)
                => VB.SyntaxFactory.GetTypeExpression(VisitType(node.Type));

            public override SyntaxNode VisitCheckedExpression(CS.Syntax.CheckedExpressionSyntax node)
            {
                string functionName;
                switch (node.Kind())
                {
                    case CS.SyntaxKind.CheckedExpression:
                        functionName = "Checked";
                        break;
                    case CS.SyntaxKind.UncheckedExpression:
                        functionName = "Unchecked";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return VB.SyntaxFactory.InvocationExpression(
                    VB.SyntaxFactory.StringLiteralExpression(VB.SyntaxFactory.StringLiteralToken(functionName, functionName)),
                    VB.SyntaxFactory.ArgumentList(
                        SeparatedList<VB.Syntax.ArgumentSyntax>(
                            VB.SyntaxFactory.SimpleArgument(VisitExpression(node.Expression)))));
            }

            public override SyntaxNode VisitMakeRefExpression(CS.Syntax.MakeRefExpressionSyntax node)
            {
                const string FunctionName = "MakeRef";
                return VB.SyntaxFactory.InvocationExpression(
                    VB.SyntaxFactory.StringLiteralExpression(VB.SyntaxFactory.StringLiteralToken(FunctionName, FunctionName)),
                    VB.SyntaxFactory.ArgumentList(
                        SeparatedList<VB.Syntax.ArgumentSyntax>(
                            VB.SyntaxFactory.SimpleArgument(VisitExpression(node.Expression)))));
            }

            public override SyntaxNode VisitRefTypeExpression(CS.Syntax.RefTypeExpressionSyntax node)
            {
                const string FunctionName = "RefType";
                return VB.SyntaxFactory.InvocationExpression(
                    VB.SyntaxFactory.StringLiteralExpression(VB.SyntaxFactory.StringLiteralToken(FunctionName, FunctionName)),
                    VB.SyntaxFactory.ArgumentList(
                        SeparatedList<VB.Syntax.ArgumentSyntax>(
                            VB.SyntaxFactory.SimpleArgument(VisitExpression(node.Expression)))));
            }

            public override SyntaxNode VisitRefValueExpression(CS.Syntax.RefValueExpressionSyntax node)
            {
                const string FunctionName = "RefValue";
                return VB.SyntaxFactory.InvocationExpression(
                    VB.SyntaxFactory.StringLiteralExpression(VB.SyntaxFactory.StringLiteralToken(FunctionName, FunctionName)),
                    VB.SyntaxFactory.ArgumentList(
                        SeparatedList<VB.Syntax.ArgumentSyntax>(
                            VB.SyntaxFactory.SimpleArgument(VisitExpression(node.Expression)))));
            }

            public override SyntaxNode VisitSizeOfExpression(CS.Syntax.SizeOfExpressionSyntax node)
            {
                const string FunctionName = "SizeOf";
                return VB.SyntaxFactory.InvocationExpression(
                    VB.SyntaxFactory.StringLiteralExpression(VB.SyntaxFactory.StringLiteralToken(FunctionName, FunctionName)),
                    VB.SyntaxFactory.ArgumentList(
                        SeparatedList<VB.Syntax.ArgumentSyntax>(
                            VB.SyntaxFactory.SimpleArgument(VisitType(node.Type)))));
            }

            public override SyntaxNode VisitBadDirectiveTrivia(CS.Syntax.BadDirectiveTriviaSyntax node)
            {
                SyntaxTrivia comment = VB.SyntaxFactory.CommentTrivia(CreateCouldNotBeConvertedComment(node.ToFullString(), typeof(VB.Syntax.DirectiveTriviaSyntax)));
                return VB.SyntaxFactory.BadDirectiveTrivia(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.HashToken).WithTrailingTrivia(comment));
            }

            public override SyntaxNode VisitWarningDirectiveTrivia(CS.Syntax.WarningDirectiveTriviaSyntax node) => CreateBadDirective(node, this);

            public override SyntaxNode VisitErrorDirectiveTrivia(CS.Syntax.ErrorDirectiveTriviaSyntax node) => CreateBadDirective(node, this);

            public override SyntaxNode VisitRegionDirectiveTrivia(CS.Syntax.RegionDirectiveTriviaSyntax node)
                => VB.SyntaxFactory.RegionDirectiveTrivia(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.HashToken),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.RegionKeyword),
                    VB.SyntaxFactory.StringLiteralToken(node.EndOfDirectiveToken.ToString(), node.EndOfDirectiveToken.ToString()));

            public override SyntaxNode VisitEndRegionDirectiveTrivia(CS.Syntax.EndRegionDirectiveTriviaSyntax node)
                => VB.SyntaxFactory.EndRegionDirectiveTrivia(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.HashToken),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.EndKeyword),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.RegionKeyword));

            public override SyntaxNode VisitEndIfDirectiveTrivia(CS.Syntax.EndIfDirectiveTriviaSyntax node)
                => VB.SyntaxFactory.EndIfDirectiveTrivia(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.HashToken),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.EndKeyword),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.IfKeyword));

            public override SyntaxNode VisitElseDirectiveTrivia(CS.Syntax.ElseDirectiveTriviaSyntax node)
                => VB.SyntaxFactory.ElseDirectiveTrivia(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.HashToken),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.ElseKeyword));

            public override SyntaxNode VisitIfDirectiveTrivia(CS.Syntax.IfDirectiveTriviaSyntax node)
                => VB.SyntaxFactory.IfDirectiveTrivia(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.HashToken),
                    new SyntaxToken(),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.IfKeyword),
                    VisitExpression(node.Condition),
                    new SyntaxToken());

            public override SyntaxNode VisitElifDirectiveTrivia(CS.Syntax.ElifDirectiveTriviaSyntax node)
                => VB.SyntaxFactory.ElseIfDirectiveTrivia(
                    VB.SyntaxFactory.Token(VB.SyntaxKind.HashToken),
                    new SyntaxToken(),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.ElseIfKeyword),
                    VisitExpression(node.Condition),
                    new SyntaxToken());

            public override SyntaxNode VisitEnumMemberDeclaration(CS.Syntax.EnumMemberDeclarationSyntax node)
            {
                VB.Syntax.ExpressionSyntax expression = node.EqualsValue == null ? null : VisitExpression(node.EqualsValue.Value);
                VB.Syntax.EqualsValueSyntax initializer = expression == null ? null : VB.SyntaxFactory.EqualsValue(expression);
                return VB.SyntaxFactory.EnumMemberDeclaration(
                    ConvertAttributes(node.AttributeLists),
                    ConvertIdentifier(node.Identifier),
                    initializer);
            }

            public override SyntaxNode VisitNullableType(CS.Syntax.NullableTypeSyntax node)
                => VB.SyntaxFactory.NullableType(VisitType(node.ElementType));

            public override SyntaxNode VisitAnonymousMethodExpression(CS.Syntax.AnonymousMethodExpressionSyntax node)
            {
                VB.Syntax.LambdaHeaderSyntax begin = VB.SyntaxFactory.FunctionLambdaHeader(
                    new SyntaxList<VB.Syntax.AttributeListSyntax>(),
                    new SyntaxTokenList(),
                    Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                    null);
                return VB.SyntaxFactory.MultiLineFunctionLambdaExpression(
                    begin,
                    statementVisitor.VisitStatement(node.Block),
                    VB.SyntaxFactory.EndFunctionStatement());
            }

            public override SyntaxNode VisitQueryExpression(CS.Syntax.QueryExpressionSyntax node)
            {
                IEnumerable<VB.Syntax.QueryClauseSyntax> newClauses =
                    Enumerable.Repeat(Visit<VB.Syntax.QueryClauseSyntax>(node.FromClause), 1)
                    .Concat(node.Body.Clauses.Select(Visit<VB.Syntax.QueryClauseSyntax>))
                    .Concat(Visit<VB.Syntax.QueryClauseSyntax>(node.Body.SelectOrGroup));
                return VB.SyntaxFactory.QueryExpression(List(newClauses));
            }

            public override SyntaxNode VisitSelectClause(CS.Syntax.SelectClauseSyntax node)
                => VB.SyntaxFactory.SelectClause(
                    VB.SyntaxFactory.ExpressionRangeVariable(VisitExpression(node.Expression)));

            public override SyntaxNode VisitFromClause(CS.Syntax.FromClauseSyntax node)
            {
                VB.Syntax.CollectionRangeVariableSyntax initializer = VB.SyntaxFactory.CollectionRangeVariable(
                    VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(node.Identifier)),
                    node.Type == null ? null : VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)),
                    VisitExpression(node.Expression));

                return VB.SyntaxFactory.FromClause(initializer);
            }

            public override SyntaxNode VisitOrderByClause(CS.Syntax.OrderByClauseSyntax node)
                => VB.SyntaxFactory.OrderByClause(
                    node.Orderings.Select(Visit<VB.Syntax.OrderingSyntax>).ToArray());

            public override SyntaxNode VisitOrdering(CS.Syntax.OrderingSyntax node)
            {
                if (node.AscendingOrDescendingKeyword.IsKind(CS.SyntaxKind.None) ||
                    node.AscendingOrDescendingKeyword.IsKind(CS.SyntaxKind.AscendingKeyword))
                {
                    return VB.SyntaxFactory.AscendingOrdering(VisitExpression(node.Expression));
                }
                else
                {
                    return VB.SyntaxFactory.DescendingOrdering(VisitExpression(node.Expression));
                }
            }

            public override SyntaxNode VisitWhereClause(CS.Syntax.WhereClauseSyntax node)
                => VB.SyntaxFactory.WhereClause(VisitExpression(node.Condition));

            public override SyntaxNode VisitJoinClause(CS.Syntax.JoinClauseSyntax node)
            {
                if (node.Into == null)
                {
                    return VB.SyntaxFactory.SimpleJoinClause(
                        SeparatedList(VB.SyntaxFactory.CollectionRangeVariable(
                            VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(node.Identifier)),
                            node.Type == null ? null : VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)),
                            VisitExpression(node.InExpression))),
                            new SyntaxList<VB.Syntax.JoinClauseSyntax>(),
                            SeparatedList(VB.SyntaxFactory.JoinCondition(
                                VisitExpression(node.LeftExpression),
                                VisitExpression(node.RightExpression))));
                }
                else
                {
                    return VB.SyntaxFactory.GroupJoinClause(
                        SeparatedList(VB.SyntaxFactory.CollectionRangeVariable(
                            VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(node.Identifier)),
                            node.Type == null ? null : VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)),
                            VisitExpression(node.InExpression))),
                        new SyntaxList<VB.Syntax.JoinClauseSyntax>(),
                        SeparatedList(VB.SyntaxFactory.JoinCondition(
                            VisitExpression(node.LeftExpression),
                            VisitExpression(node.RightExpression))),
                        SeparatedList(VB.SyntaxFactory.AggregationRangeVariable(
                            VB.SyntaxFactory.VariableNameEquals(VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(node.Into.Identifier))),
                            VB.SyntaxFactory.GroupAggregation())));
                }
            }

            public override SyntaxNode VisitGroupClause(CS.Syntax.GroupClauseSyntax node)
            {
                VB.Syntax.ExpressionRangeVariableSyntax groupExpression = VB.SyntaxFactory.ExpressionRangeVariable(
                    null, VisitExpression(node.GroupExpression));
                VB.Syntax.ExpressionRangeVariableSyntax byExpression = VB.SyntaxFactory.ExpressionRangeVariable(
                    null, VisitExpression(node.ByExpression));
                QueryExpressionSyntax query = (CS.Syntax.QueryExpressionSyntax)node.Parent;
                VB.Syntax.AggregationRangeVariableSyntax rangeVariable;
                if (query.Body.Continuation == null)
                {
                    rangeVariable = VB.SyntaxFactory.AggregationRangeVariable(VB.SyntaxFactory.GroupAggregation());
                }
                else
                {
                    rangeVariable = VB.SyntaxFactory.AggregationRangeVariable(
                          VB.SyntaxFactory.VariableNameEquals(VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(query.Body.Continuation.Identifier))),
                          VB.SyntaxFactory.GroupAggregation(VB.SyntaxFactory.Token(VB.SyntaxKind.GroupKeyword)));
                }

                return VB.SyntaxFactory.GroupByClause(
                    SeparatedList(groupExpression),
                    SeparatedList(byExpression),
                    SeparatedList(rangeVariable));
            }

            public override SyntaxNode VisitLetClause(CS.Syntax.LetClauseSyntax node)
                => VB.SyntaxFactory.LetClause(
                    VB.SyntaxFactory.ExpressionRangeVariable(
                    VB.SyntaxFactory.VariableNameEquals(VB.SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(node.Identifier))),
                    VisitExpression(node.Expression)));

            public override SyntaxNode VisitAnonymousObjectCreationExpression(CS.Syntax.AnonymousObjectCreationExpressionSyntax node)
                => VB.SyntaxFactory.AnonymousObjectCreationExpression(
                    VB.SyntaxFactory.ObjectMemberInitializer(
                        node.Initializers.Select(Visit<VB.Syntax.FieldInitializerSyntax>).ToArray()));

            public override SyntaxNode VisitAnonymousObjectMemberDeclarator(CS.Syntax.AnonymousObjectMemberDeclaratorSyntax node)
                => node.NameEquals == null
                    ? VB.SyntaxFactory.InferredFieldInitializer(VisitExpression(node.Expression))
                    : (VB.Syntax.FieldInitializerSyntax)VB.SyntaxFactory.NamedFieldInitializer(
                        VB.SyntaxFactory.IdentifierName(ConvertIdentifier(node.NameEquals.Name)),
                        VisitExpression(node.Expression));

            public override SyntaxNode VisitDefineDirectiveTrivia(CS.Syntax.DefineDirectiveTriviaSyntax node)
                => CreateBadDirective(node, this);

            public override SyntaxNode VisitUndefDirectiveTrivia(CS.Syntax.UndefDirectiveTriviaSyntax node)
                => CreateBadDirective(node, this);

            public override SyntaxNode VisitPragmaWarningDirectiveTrivia(CS.Syntax.PragmaWarningDirectiveTriviaSyntax node)
                => CreateBadDirective(node, this);

            public override SyntaxNode VisitPragmaChecksumDirectiveTrivia(CS.Syntax.PragmaChecksumDirectiveTriviaSyntax node)
                => CreateBadDirective(node, this);

            public override SyntaxNode VisitLineDirectiveTrivia(CS.Syntax.LineDirectiveTriviaSyntax node)
                => CreateBadDirective(node, this);

            public override SyntaxNode VisitFinallyClause(CS.Syntax.FinallyClauseSyntax node)
                => VB.SyntaxFactory.FinallyBlock(
                    statementVisitor.VisitStatement(node.Block));

            public override SyntaxNode VisitCatchClause(CS.Syntax.CatchClauseSyntax node)
            {
                VB.Syntax.CatchStatementSyntax statement;
                if (node.Declaration == null)
                {
                    statement = VB.SyntaxFactory.CatchStatement();
                }
                else if (node.Declaration.Identifier.IsKind(CS.SyntaxKind.None))
                {
                    statement = VB.SyntaxFactory.CatchStatement(
                        null,
                        VB.SyntaxFactory.SimpleAsClause(VisitType(node.Declaration.Type)),
                        null);
                }
                else
                {
                    statement = VB.SyntaxFactory.CatchStatement(
                        VB.SyntaxFactory.IdentifierName(ConvertIdentifier(node.Declaration.Identifier)),
                        VB.SyntaxFactory.SimpleAsClause(VisitType(node.Declaration.Type)),
                        null);
                }

                return VB.SyntaxFactory.CatchBlock(
                    statement,
                    statementVisitor.VisitStatement(node.Block));
            }

            public override SyntaxNode VisitConversionOperatorDeclaration(CS.Syntax.ConversionOperatorDeclarationSyntax node)
            {
                SyntaxToken direction = node.Modifiers.Any(t => t.IsKind(CS.SyntaxKind.ImplicitKeyword))
                    ? VB.SyntaxFactory.Token(VB.SyntaxKind.WideningKeyword)
                    : VB.SyntaxFactory.Token(VB.SyntaxKind.NarrowingKeyword);

                VB.Syntax.OperatorStatementSyntax begin = VB.SyntaxFactory.OperatorStatement(
                    ConvertAttributes(node.AttributeLists),
                    TokenList(ConvertModifiers(node.Modifiers).Concat(direction)),
                    VB.SyntaxFactory.Token(VB.SyntaxKind.CTypeKeyword),
                    Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                    VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)));

                if (node.Body == null)
                {
                    return begin;
                }

                return VB.SyntaxFactory.OperatorBlock(
                    begin,
                    statementVisitor.VisitStatement(node.Body),
                    VB.SyntaxFactory.EndOperatorStatement());
            }

            public override SyntaxNode VisitPointerType(CS.Syntax.PointerTypeSyntax node)
                // just ignore the pointer part
                => Visit<SyntaxNode>(node.ElementType);

            public override SyntaxNode VisitDestructorDeclaration(CS.Syntax.DestructorDeclarationSyntax node)
            {
                VB.Syntax.MethodStatementSyntax begin = VB.SyntaxFactory.SubStatement(
                    new SyntaxList<VB.Syntax.AttributeListSyntax>(),
                    new SyntaxTokenList(),
                    VB.SyntaxFactory.Identifier("Finalize"),
                    null,
                    VB.SyntaxFactory.ParameterList(),
                    null, null, null);

                if (node.Body == null)
                {
                    return begin;
                }

                return VB.SyntaxFactory.SubBlock(
                    begin,
                    statementVisitor.VisitStatement(node.Body),
                    VB.SyntaxFactory.EndSubStatement());
            }

            public override SyntaxNode VisitDelegateDeclaration(CS.Syntax.DelegateDeclarationSyntax node)
            {
                SyntaxToken identifier = ConvertIdentifier(node.Identifier);
                VB.Syntax.TypeParameterListSyntax typeParameters = Visit<VB.Syntax.TypeParameterListSyntax>(node.TypeParameterList);

                if (node.ReturnType.IsKind(CS.SyntaxKind.PredefinedType) &&
                    ((CS.Syntax.PredefinedTypeSyntax)node.ReturnType).Keyword.IsKind(CS.SyntaxKind.VoidKeyword))
                {
                    return VB.SyntaxFactory.DelegateSubStatement(
                        ConvertAttributes(node.AttributeLists),
                        ConvertModifiers(node.Modifiers),
                        identifier,
                        typeParameters,
                        Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                        null);
                }
                else
                {
                    return VB.SyntaxFactory.DelegateFunctionStatement(
                        ConvertAttributes(node.AttributeLists),
                        ConvertModifiers(node.Modifiers),
                        identifier,
                        typeParameters,
                        Visit<VB.Syntax.ParameterListSyntax>(node.ParameterList),
                        VB.SyntaxFactory.SimpleAsClause(VisitType(node.ReturnType)));
                }
            }

            public override SyntaxNode VisitEventFieldDeclaration(CS.Syntax.EventFieldDeclarationSyntax node)
                => VB.SyntaxFactory.EventStatement(
                    ConvertAttributes(node.AttributeLists),
                    ConvertModifiers(node.Modifiers),
                    ConvertIdentifier(node.Declaration.Variables[0].Identifier),
                    null,
                    VB.SyntaxFactory.SimpleAsClause(VisitType(node.Declaration.Type)),
                    null);

            public override SyntaxNode VisitEventDeclaration(CS.Syntax.EventDeclarationSyntax node)
            {
                SyntaxToken identifier = ConvertIdentifier(node.Identifier);

                VB.Syntax.EventStatementSyntax begin = VB.SyntaxFactory.EventStatement(
                    ConvertAttributes(node.AttributeLists),
                    ConvertModifiers(node.Modifiers),
                    identifier,
                    null,
                    VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)),
                    null);

                return VB.SyntaxFactory.EventBlock(
                    begin,
                    List(node.AccessorList.Accessors.Select(Visit<VB.Syntax.AccessorBlockSyntax>)),
                    VB.SyntaxFactory.EndEventStatement());
            }

            public override SyntaxNode VisitStackAllocArrayCreationExpression(CS.Syntax.StackAllocArrayCreationExpressionSyntax node)
            {
                string error = CreateCouldNotBeConvertedString(node.ToFullString(), typeof(SyntaxNode));
                return VB.SyntaxFactory.StringLiteralExpression(
                    VB.SyntaxFactory.StringLiteralToken(error, error));
            }

            public override SyntaxNode VisitIncompleteMember(CS.Syntax.IncompleteMemberSyntax node)
                => VB.SyntaxFactory.FieldDeclaration(
                    ConvertAttributes(node.AttributeLists),
                    ConvertModifiers(node.Modifiers),
                    SeparatedList(
                        VB.SyntaxFactory.VariableDeclarator(
                            SeparatedList(VB.SyntaxFactory.ModifiedIdentifier(VB.SyntaxFactory.Identifier("IncompleteMember"))),
                            VB.SyntaxFactory.SimpleAsClause(VisitType(node.Type)), null)));

            public override SyntaxNode VisitExternAliasDirective(CS.Syntax.ExternAliasDirectiveSyntax node)
            {
                IEnumerable<SyntaxTrivia> leadingTrivia = node.GetFirstToken(includeSkipped: true).LeadingTrivia.SelectMany(VisitTrivia);
                IEnumerable<SyntaxTrivia> trailingTrivia = node.GetLastToken(includeSkipped: true).TrailingTrivia.SelectMany(VisitTrivia);

                SyntaxTrivia comment = VB.SyntaxFactory.CommentTrivia(
                    CreateCouldNotBeConvertedComment(node.ToString(), typeof(VB.Syntax.ImportsStatementSyntax)));
                leadingTrivia = leadingTrivia.Concat(comment);

                return VB.SyntaxFactory.ImportsStatement(
                    VB.SyntaxFactory.Token(TriviaList(leadingTrivia), VB.SyntaxKind.ImportsKeyword, TriviaList(trailingTrivia), string.Empty),
                    new SeparatedSyntaxList<VB.Syntax.ImportsClauseSyntax>());
            }

            public override SyntaxNode VisitConstructorInitializer(CS.Syntax.ConstructorInitializerSyntax node)
            {
                VB.Syntax.InstanceExpressionSyntax expr = null;
                if (node.IsKind(CS.SyntaxKind.BaseConstructorInitializer))
                {
                    expr = VB.SyntaxFactory.MyBaseExpression();
                }
                else
                {
                    expr = VB.SyntaxFactory.MyClassExpression();
                }

                VB.Syntax.InvocationExpressionSyntax invocation = VB.SyntaxFactory.InvocationExpression(
                    VB.SyntaxFactory.SimpleMemberAccessExpression(
                        expr,
                        VB.SyntaxFactory.Token(VB.SyntaxKind.DotToken),
                        VB.SyntaxFactory.IdentifierName(VB.SyntaxFactory.Identifier("New"))),
                        Visit<VB.Syntax.ArgumentListSyntax>(node.ArgumentList));

                return VB.SyntaxFactory.ExpressionStatement(expression: invocation);
            }

            public override SyntaxNode DefaultVisit(SyntaxNode node)
            {
                // If you hit this, it means there was some sort of CS construct
                // that we haven't written a conversion routine for.  Simply add
                // it above and rerun.
                throw new NotImplementedException();
            }
        }
    }
}

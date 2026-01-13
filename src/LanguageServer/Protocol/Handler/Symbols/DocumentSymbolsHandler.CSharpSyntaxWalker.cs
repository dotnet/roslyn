// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed partial class DocumentSymbolsHandler
{
    /// <summary>
    /// A CSharpSyntaxWalker that builds hierarchical document symbols from syntax nodes.
    /// This is used when useHierarchicalSymbols is enabled to provide proper nested symbol hierarchy
    /// including namespaces for features like VS Code's Sticky Scroll.
    /// </summary>
    private sealed class CSharpDocumentSymbolWalker : CSharpSyntaxWalker
    {
        private readonly SourceText _text;
        private readonly CancellationToken _cancellationToken;
        private readonly Stack<List<RoslynDocumentSymbol>> _symbolStack = new();

        public CSharpDocumentSymbolWalker(SourceText text, CancellationToken cancellationToken)
        {
            _text = text;
            _cancellationToken = cancellationToken;
            // Start with the root list
            _symbolStack.Push([]);
        }

        public RoslynDocumentSymbol[] GetDocumentSymbols()
        {
            return [.. _symbolStack.Peek()];
        }

        private void AddSymbol(RoslynDocumentSymbol symbol)
        {
            _symbolStack.Peek().Add(symbol);
        }

        private void PushContainer()
        {
            _symbolStack.Push([]);
        }

        private RoslynDocumentSymbol[] PopContainer()
        {
            return [.. _symbolStack.Pop()];
        }

        private static Range GetRange(SyntaxNode node, SourceText text)
            => ProtocolConversions.TextSpanToRange(node.Span, text);

        private static Range GetSelectionRange(SyntaxToken identifier, SourceText text)
            => ProtocolConversions.TextSpanToRange(identifier.Span, text);

        private void VisitContainerDeclaration(
            SyntaxNode node,
            string name,
            string detail,
            LSP.SymbolKind kind,
            Glyph glyph,
            SyntaxToken identifierToken)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            PushContainer();
            base.DefaultVisit(node);
            var children = PopContainer();

            var symbol = new RoslynDocumentSymbol
            {
                Name = GetDocumentSymbolName(name),
                Detail = detail,
                Kind = kind,
                Glyph = (int)glyph,
                Range = GetRange(node, _text),
                SelectionRange = GetSelectionRange(identifierToken, _text),
                Children = children,
            };

            AddSymbol(symbol);
        }

        private void VisitMemberDeclaration(
            SyntaxNode node,
            string name,
            string detail,
            LSP.SymbolKind kind,
            Glyph glyph,
            SyntaxToken identifierToken,
            bool visitChildren = false)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            RoslynDocumentSymbol[] children = [];
            if (visitChildren)
            {
                PushContainer();
                base.DefaultVisit(node);
                children = PopContainer();
            }

            var symbol = new RoslynDocumentSymbol
            {
                Name = GetDocumentSymbolName(name),
                Detail = detail,
                Kind = kind,
                Glyph = (int)glyph,
                Range = GetRange(node, _text),
                SelectionRange = GetSelectionRange(identifierToken, _text),
                Children = children,
            };

            AddSymbol(symbol);
        }

        // Namespace declarations
        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var name = node.Name.ToString();
            VisitContainerDeclaration(
                node,
                name,
                name,
                LSP.SymbolKind.Namespace,
                Glyph.Namespace,
                node.Name.GetLastToken());
        }

        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            var name = node.Name.ToString();
            VisitContainerDeclaration(
                node,
                name,
                name,
                LSP.SymbolKind.Namespace,
                Glyph.Namespace,
                node.Name.GetLastToken());
        }

        // Type declarations
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.ClassPublic, Glyph.ClassInternal, Glyph.ClassProtected, Glyph.ClassPrivate);
            VisitContainerDeclaration(node, name, GetTypeDetail(node), LSP.SymbolKind.Class, glyph, node.Identifier);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.StructurePublic, Glyph.StructureInternal, Glyph.StructureProtected, Glyph.StructurePrivate);
            VisitContainerDeclaration(node, name, GetTypeDetail(node), LSP.SymbolKind.Struct, glyph, node.Identifier);
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var isStruct = node.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword);
            var glyph = isStruct
                ? GetAccessibilityGlyph(node.Modifiers, Glyph.StructurePublic, Glyph.StructureInternal, Glyph.StructureProtected, Glyph.StructurePrivate)
                : GetAccessibilityGlyph(node.Modifiers, Glyph.ClassPublic, Glyph.ClassInternal, Glyph.ClassProtected, Glyph.ClassPrivate);
            var kind = isStruct ? LSP.SymbolKind.Struct : LSP.SymbolKind.Class;
            VisitContainerDeclaration(node, name, GetTypeDetail(node), kind, glyph, node.Identifier);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.InterfacePublic, Glyph.InterfaceInternal, Glyph.InterfaceProtected, Glyph.InterfacePrivate);
            VisitContainerDeclaration(node, name, GetTypeDetail(node), LSP.SymbolKind.Interface, glyph, node.Identifier);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.EnumPublic, Glyph.EnumInternal, Glyph.EnumProtected, Glyph.EnumPrivate);
            VisitContainerDeclaration(node, name, name, LSP.SymbolKind.Enum, glyph, node.Identifier);
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.DelegatePublic, Glyph.DelegateInternal, Glyph.DelegateProtected, Glyph.DelegatePrivate);
            var detail = $"{name}({GetParameterListString(node.ParameterList)})";
            VisitMemberDeclaration(node, name, detail, LSP.SymbolKind.Class, glyph, node.Identifier);
        }

        // Member declarations
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.MethodPublic, Glyph.MethodInternal, Glyph.MethodProtected, Glyph.MethodPrivate);
            var detail = $"{name}({GetParameterListString(node.ParameterList)})";
            // Visit children to find local functions
            VisitMemberDeclaration(node, name, detail, LSP.SymbolKind.Method, glyph, node.Identifier, visitChildren: true);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.MethodPublic, Glyph.MethodInternal, Glyph.MethodProtected, Glyph.MethodPrivate);
            var detail = $"{name}({GetParameterListString(node.ParameterList)})";
            VisitMemberDeclaration(node, name, detail, LSP.SymbolKind.Constructor, glyph, node.Identifier);
        }

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            var name = $"~{node.Identifier.Text}";
            VisitMemberDeclaration(node, name, $"{name}()", LSP.SymbolKind.Method, Glyph.MethodPublic, node.Identifier);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.PropertyPublic, Glyph.PropertyInternal, Glyph.PropertyProtected, Glyph.PropertyPrivate);
            VisitMemberDeclaration(node, name, name, LSP.SymbolKind.Property, glyph, node.Identifier);
        }

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            var name = "this";
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.PropertyPublic, Glyph.PropertyInternal, Glyph.PropertyProtected, Glyph.PropertyPrivate);
            var detail = $"this[{GetBracketedParameterListString(node.ParameterList)}]";
            VisitMemberDeclaration(node, name, detail, LSP.SymbolKind.Property, glyph, node.ThisKeyword);
        }

        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.EventPublic, Glyph.EventInternal, Glyph.EventProtected, Glyph.EventPrivate);
            VisitMemberDeclaration(node, name, name, LSP.SymbolKind.Event, glyph, node.Identifier);
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.EventPublic, Glyph.EventInternal, Glyph.EventProtected, Glyph.EventPrivate);
            foreach (var variable in node.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                VisitMemberDeclaration(variable, name, name, LSP.SymbolKind.Event, glyph, variable.Identifier);
            }
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var isConst = node.Modifiers.Any(SyntaxKind.ConstKeyword);
            var fieldGlyph = GetAccessibilityGlyph(node.Modifiers, Glyph.FieldPublic, Glyph.FieldInternal, Glyph.FieldProtected, Glyph.FieldPrivate);
            var constGlyph = GetAccessibilityGlyph(node.Modifiers, Glyph.ConstantPublic, Glyph.ConstantInternal, Glyph.ConstantProtected, Glyph.ConstantPrivate);
            var glyph = isConst ? constGlyph : fieldGlyph;
            var kind = isConst ? LSP.SymbolKind.Constant : LSP.SymbolKind.Field;

            foreach (var variable in node.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                VisitMemberDeclaration(variable, name, name, kind, glyph, variable.Identifier);
            }
        }

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            var name = $"operator {node.OperatorToken.Text}";
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.OperatorPublic, Glyph.OperatorInternal, Glyph.OperatorProtected, Glyph.OperatorPrivate);
            var detail = $"{name}({GetParameterListString(node.ParameterList)})";
            VisitMemberDeclaration(node, name, detail, LSP.SymbolKind.Operator, glyph, node.OperatorToken);
        }

        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            var keyword = node.ImplicitOrExplicitKeyword.Text;
            var typeName = node.Type.ToString();
            var name = $"{keyword} operator {typeName}";
            var glyph = GetAccessibilityGlyph(node.Modifiers, Glyph.OperatorPublic, Glyph.OperatorInternal, Glyph.OperatorProtected, Glyph.OperatorPrivate);
            var detail = $"{name}({GetParameterListString(node.ParameterList)})";
            VisitMemberDeclaration(node, name, detail, LSP.SymbolKind.Operator, glyph, node.OperatorKeyword);
        }

        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            VisitMemberDeclaration(node, name, name, LSP.SymbolKind.EnumMember, Glyph.EnumMemberPublic, node.Identifier);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            var name = node.Identifier.Text;
            var detail = $"{name}({GetParameterListString(node.ParameterList)})";
            // Visit children to find nested local functions
            VisitMemberDeclaration(node, name, detail, LSP.SymbolKind.Function, Glyph.MethodPrivate, node.Identifier, visitChildren: true);
        }

        // Helper methods
        private static string GetTypeDetail(TypeDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            if (node.TypeParameterList != null && node.TypeParameterList.Parameters.Count > 0)
            {
                var typeParams = string.Join(", ", node.TypeParameterList.Parameters.Select(p => p.Identifier.Text));
                return $"{name}<{typeParams}>";
            }

            return name;
        }

        private static string GetParameterListString(ParameterListSyntax? parameterList)
        {
            if (parameterList == null)
                return string.Empty;

            return string.Join(", ", parameterList.Parameters.Select(p => GetParameterString(p)));
        }

        private static string GetBracketedParameterListString(BracketedParameterListSyntax? parameterList)
        {
            if (parameterList == null)
                return string.Empty;

            return string.Join(", ", parameterList.Parameters.Select(p => GetParameterString(p)));
        }

        private static string GetParameterString(ParameterSyntax parameter)
        {
            var type = parameter.Type?.ToString() ?? "object";
            var name = parameter.Identifier.Text;
            return $"{type} {name}";
        }

        private static Glyph GetAccessibilityGlyph(SyntaxTokenList modifiers, Glyph publicGlyph, Glyph internalGlyph, Glyph protectedGlyph, Glyph privateGlyph)
        {
            foreach (var modifier in modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                        return publicGlyph;
                    case SyntaxKind.ProtectedKeyword:
                        return protectedGlyph;
                    case SyntaxKind.PrivateKeyword:
                        return privateGlyph;
                    case SyntaxKind.InternalKeyword:
                        return internalGlyph;
                }
            }

            // Default to internal for types, private for members
            return internalGlyph;
        }
    }

    /// <summary>
    /// Gets hierarchical document symbols from a C# document using syntax-only analysis.
    /// </summary>
    internal static RoslynDocumentSymbol[] GetHierarchicalDocumentSymbolsFromSyntax(
        SyntaxNode root, SourceText text, CancellationToken cancellationToken)
    {
        var walker = new CSharpDocumentSymbolWalker(text, cancellationToken);
        walker.Visit(root);
        return walker.GetDocumentSymbols();
    }
}

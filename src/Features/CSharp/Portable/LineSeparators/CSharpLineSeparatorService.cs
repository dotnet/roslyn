// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.LineSeparators;

[ExportLanguageService(typeof(ILineSeparatorService), LanguageNames.CSharp), Shared]
internal class CSharpLineSeparatorService : ILineSeparatorService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpLineSeparatorService()
    {
    }

    /// <summary>
    /// Given a tree returns line separator spans.
    /// The operation may take fairly long time on a big tree so it is cancellable.
    /// </summary>
    public async Task<ImmutableArray<TextSpan>> GetLineSeparatorsAsync(
        Document document,
        TextSpan textSpan,
        CancellationToken cancellationToken)
    {
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var node = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<TextSpan>.GetInstance(out var spans);

        var blocks = node.Traverse<SyntaxNode>(textSpan, IsSeparableContainer);

        foreach (var block in blocks)
        {
            if (cancellationToken.IsCancellationRequested)
                return [];

            switch (block)
            {
                case TypeDeclarationSyntax typeBlock:
                    ProcessNodeList(typeBlock.Members, spans, cancellationToken);
                    continue;
                case BaseNamespaceDeclarationSyntax namespaceBlock:
                    ProcessUsings(namespaceBlock.Usings, spans, cancellationToken);
                    ProcessNodeList(namespaceBlock.Members, spans, cancellationToken);
                    continue;
                case CompilationUnitSyntax progBlock:
                    ProcessUsings(progBlock.Usings, spans, cancellationToken);
                    ProcessNodeList(progBlock.Members, spans, cancellationToken);
                    break;
            }
        }

        return spans.ToImmutableAndClear();
    }

    /// <summary>Node types that are interesting for line separation.</summary>
    private static bool IsSeparableBlock(SyntaxNode node)
    {
        if (SyntaxFacts.IsTypeDeclaration(node.Kind()))
        {
            return true;
        }

        switch (node.Kind())
        {
            case SyntaxKind.NamespaceDeclaration:
            case SyntaxKind.MethodDeclaration:
            case SyntaxKind.PropertyDeclaration:
            case SyntaxKind.EventDeclaration:
            case SyntaxKind.IndexerDeclaration:
            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.DestructorDeclaration:
            case SyntaxKind.OperatorDeclaration:
            case SyntaxKind.ConversionOperatorDeclaration:
                return true;

            default:
                return false;
        }
    }

    /// <summary>Node types that may contain separable blocks.</summary>
    private static bool IsSeparableContainer(SyntaxNode node)
        => node is TypeDeclarationSyntax or BaseNamespaceDeclarationSyntax or CompilationUnitSyntax;

    private static bool IsBadType(SyntaxNode node)
    {
        if (node is TypeDeclarationSyntax typeDecl)
        {
            if (typeDecl.OpenBraceToken.IsMissing ||
                typeDecl.CloseBraceToken.IsMissing)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBadEnum(SyntaxNode node)
    {
        if (node is EnumDeclarationSyntax enumDecl)
        {
            if (enumDecl.OpenBraceToken.IsMissing ||
                enumDecl.CloseBraceToken.IsMissing)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBadMethod(SyntaxNode node)
    {
        if (node is MethodDeclarationSyntax methodDecl)
        {
            if (methodDecl.Body != null &&
               (methodDecl.Body.OpenBraceToken.IsMissing ||
                methodDecl.Body.CloseBraceToken.IsMissing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBadProperty(SyntaxNode node)
        => IsBadAccessorList(node as PropertyDeclarationSyntax);

    private static bool IsBadEvent(SyntaxNode node)
        => IsBadAccessorList(node as EventDeclarationSyntax);

    private static bool IsBadIndexer(SyntaxNode node)
        => IsBadAccessorList(node as IndexerDeclarationSyntax);

    private static bool IsBadAccessorList(BasePropertyDeclarationSyntax? baseProperty)
    {
        if (baseProperty?.AccessorList == null)
            return false;

        return baseProperty.AccessorList.OpenBraceToken.IsMissing ||
            baseProperty.AccessorList.CloseBraceToken.IsMissing;
    }

    private static bool IsBadConstructor(SyntaxNode node)
    {
        if (node is ConstructorDeclarationSyntax constructorDecl)
        {
            if (constructorDecl.Body != null &&
               (constructorDecl.Body.OpenBraceToken.IsMissing ||
                constructorDecl.Body.CloseBraceToken.IsMissing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBadDestructor(SyntaxNode node)
    {
        if (node is DestructorDeclarationSyntax destructorDecl)
        {
            if (destructorDecl.Body != null &&
               (destructorDecl.Body.OpenBraceToken.IsMissing ||
                destructorDecl.Body.CloseBraceToken.IsMissing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBadOperator(SyntaxNode node)
    {
        if (node is OperatorDeclarationSyntax operatorDecl)
        {
            if (operatorDecl.Body != null &&
               (operatorDecl.Body.OpenBraceToken.IsMissing ||
                operatorDecl.Body.CloseBraceToken.IsMissing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBadConversionOperator(SyntaxNode node)
    {
        if (node is ConversionOperatorDeclarationSyntax conversionDecl)
        {
            if (conversionDecl.Body != null &&
               (conversionDecl.Body.OpenBraceToken.IsMissing ||
                conversionDecl.Body.CloseBraceToken.IsMissing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBadNode(SyntaxNode node)
    {
        if (node is IncompleteMemberSyntax)
        {
            return true;
        }

        if (IsBadType(node) ||
            IsBadEnum(node) ||
            IsBadMethod(node) ||
            IsBadProperty(node) ||
            IsBadEvent(node) ||
            IsBadIndexer(node) ||
            IsBadConstructor(node) ||
            IsBadDestructor(node) ||
            IsBadOperator(node) ||
            IsBadConversionOperator(node))
        {
            return true;
        }

        return false;
    }

    private static void ProcessUsings(SyntaxList<UsingDirectiveSyntax> usings, ArrayBuilder<TextSpan> spans, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(spans);

        if (usings.Any())
        {
            AddLineSeparatorSpanForNode(usings.Last(), spans, cancellationToken);
        }
    }

    /// <summary>
    /// If node is separable and not the last in its container => add line separator after the node
    /// If node is separable and not the first in its container => ensure separator before the node
    /// last separable node in Program needs separator after it.
    /// </summary>
    private static void ProcessNodeList<T>(SyntaxList<T> children, ArrayBuilder<TextSpan> spans, CancellationToken cancellationToken) where T : SyntaxNode
    {
        Contract.ThrowIfNull(spans);

        if (children.Count == 0)
        {
            // nothing to separate
            return;
        }

        // first child needs no separator
        var seenSeparator = true;
        for (var i = 0; i < children.Count - 1; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cur = children[i];

            if (!IsSeparableBlock(cur))
            {
                seenSeparator = false;
            }
            else
            {
                if (!seenSeparator)
                {
                    var prev = children[i - 1];
                    AddLineSeparatorSpanForNode(prev, spans, cancellationToken);
                }

                AddLineSeparatorSpanForNode(cur, spans, cancellationToken);
                seenSeparator = true;
            }
        }

        // last child may need separator only before it
        var lastChild = children.Last();

        if (IsSeparableBlock(lastChild))
        {
            if (!seenSeparator)
            {
                var nextToLast = children[^2];
                AddLineSeparatorSpanForNode(nextToLast, spans, cancellationToken);
            }

            if (lastChild.IsParentKind(SyntaxKind.CompilationUnit))
            {
                AddLineSeparatorSpanForNode(lastChild, spans, cancellationToken);
            }
        }
    }

    private static void AddLineSeparatorSpanForNode(SyntaxNode node, ArrayBuilder<TextSpan> spans, CancellationToken cancellationToken)
    {
        if (IsBadNode(node))
        {
            return;
        }

        var span = GetLineSeparatorSpanForNode(node);

        if (IsLegalSpanForLineSeparator(node.SyntaxTree, span, cancellationToken))
        {
            spans.Add(span);
        }
    }

    private static bool IsLegalSpanForLineSeparator(SyntaxTree syntaxTree, TextSpan textSpan, CancellationToken cancellationToken)
    {
        // A span is a legal location for a line separator if the following line 
        // contains only whitespace or the span is the last line in the buffer.

        var line = syntaxTree.GetText(cancellationToken).Lines.IndexOf(textSpan.End);
        if (line == syntaxTree.GetText(cancellationToken).Lines.Count - 1)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(syntaxTree.GetText(cancellationToken).Lines[line + 1].ToString()))
        {
            return true;
        }

        return false;
    }

    private static TextSpan GetLineSeparatorSpanForNode(SyntaxNode node)
    {
        // we only want to underline the node with a long line
        // for this purpose the last token is as good as the whole node, but has 
        // simpler and typically single line geometry (so it will be easier to find "bottom")
        return node.GetLastToken().Span;
    }
}

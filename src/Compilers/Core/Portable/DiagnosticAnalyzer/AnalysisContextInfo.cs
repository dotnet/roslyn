// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal struct AnalysisContextInfo
    {
        private readonly Compilation _compilation;
        private readonly IOperation _operation;
        private readonly ISymbol _symbol;
        private readonly SyntaxTree _tree;
        private readonly SyntaxNode _node;

        public AnalysisContextInfo(Compilation compilation) :
            this(compilation: compilation, operation: null, symbol: null, tree: null, node: null)
        {
        }

        public AnalysisContextInfo(SemanticModel model) :
            this(model.Compilation, model.SyntaxTree)
        {
        }

        public AnalysisContextInfo(Compilation compilation, ISymbol symbol) :
            this(compilation: compilation, operation: null, symbol: symbol, tree: null, node: null)
        {
        }

        public AnalysisContextInfo(Compilation compilation, SyntaxTree tree) :
            this(compilation: compilation, operation: null, symbol: null, tree: tree, node: null)
        {
        }

        public AnalysisContextInfo(Compilation compilation, SyntaxNode node) :
            this(compilation: compilation, operation: null, symbol: null, tree: node.SyntaxTree, node: node)
        {
        }

        public AnalysisContextInfo(Compilation compilation, IOperation operation) :
            this(compilation: compilation, operation: operation, symbol: null, tree: operation.Syntax.SyntaxTree, node: operation.Syntax)
        {
        }

        public AnalysisContextInfo(Compilation compilation, ISymbol symbol, SyntaxNode node) :
            this(compilation: compilation, operation: null, symbol: symbol, tree: node.SyntaxTree, node: node)
        {
        }

        public AnalysisContextInfo(
            Compilation compilation,
            IOperation operation,
            ISymbol symbol,
            SyntaxTree tree,
            SyntaxNode node)
        {
            _compilation = compilation;
            _operation = operation;
            _symbol = symbol;
            _tree = tree;
            _node = node;
        }

        public string GetContext()
        {
            var sb = new StringBuilder();

            if (_compilation?.AssemblyName != null)
            {
                sb.AppendLine($"{nameof(Compilation)}: {_compilation.AssemblyName}");
            }

            if (_operation != null)
            {
                sb.AppendLine($"{nameof(IOperation)}: {_operation.Kind}");
            }

            if (_symbol?.Name != null)
            {
                sb.AppendLine($"{nameof(ISymbol)}: {_symbol.Name} ({_symbol.Kind})");
            }

            if (_tree?.FilePath != null)
            {
                sb.AppendLine($"{nameof(SyntaxTree)}: {_tree.FilePath}");
            }

            if (_node != null)
            {
                var text = _tree?.GetText();
                var lineSpan = text?.Lines?.GetLinePositionSpan(_node.Span);

                // can't use Kind since that is language specific. instead will output typename.
                sb.AppendLine($"{nameof(SyntaxNode)}: {GetFlattenedNodeText(_node)} [{_node.GetType().Name}]@{_node.Span.ToString()} {(lineSpan.HasValue ? lineSpan.Value.ToString() : string.Empty)}");
            }

            return sb.ToString();
        }

        private string GetFlattenedNodeText(SyntaxNode node)
        {
            const int cutoff = 30;

            var lastEnd = node.Span.Start;
            var sb = new StringBuilder();
            foreach (var token in node.DescendantTokens(descendIntoTrivia: false))
            {
                if (token.Span.Start - lastEnd > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(token.ToString());
                lastEnd = token.Span.End;

                if (sb.Length > cutoff)
                {
                    break;
                }
            }

            return sb.ToString() + (sb.Length > cutoff ? " ..." : string.Empty);
        }
    }
}

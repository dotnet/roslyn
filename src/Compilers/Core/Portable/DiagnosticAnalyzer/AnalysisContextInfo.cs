// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// this hold onto analyzer executor context which will be used later to put context information in analyzer exception if it occurs.
    /// </summary>
    internal readonly struct AnalysisContextInfo
    {
        private readonly Compilation? _compilation;
        private readonly IOperation? _operation;
        private readonly ISymbol? _symbol;
        private readonly SourceOrAdditionalFile? _file;
        private readonly SyntaxNode? _node;

        public AnalysisContextInfo(Compilation compilation) :
            this(compilation: compilation, operation: null, symbol: null, file: null, node: null)
        {
        }

        public AnalysisContextInfo(SemanticModel model) :
            this(model.Compilation, new SourceOrAdditionalFile(model.SyntaxTree))
        {
        }

        public AnalysisContextInfo(Compilation compilation, ISymbol symbol) :
            this(compilation: compilation, operation: null, symbol: symbol, file: null, node: null)
        {
        }

        public AnalysisContextInfo(Compilation compilation, SourceOrAdditionalFile file) :
            this(compilation: compilation, operation: null, symbol: null, file: file, node: null)
        {
        }

        public AnalysisContextInfo(Compilation compilation, SyntaxNode node) :
            this(compilation: compilation, operation: null, symbol: null, file: new SourceOrAdditionalFile(node.SyntaxTree), node)
        {
        }

        public AnalysisContextInfo(Compilation compilation, IOperation operation) :
            this(compilation: compilation, operation: operation, symbol: null, file: new SourceOrAdditionalFile(operation.Syntax.SyntaxTree), node: operation.Syntax)
        {
        }

        public AnalysisContextInfo(Compilation compilation, ISymbol symbol, SyntaxNode node) :
            this(compilation: compilation, operation: null, symbol: symbol, file: new SourceOrAdditionalFile(node.SyntaxTree), node)
        {
        }

        private AnalysisContextInfo(
            Compilation? compilation,
            IOperation? operation,
            ISymbol? symbol,
            SourceOrAdditionalFile? file,
            SyntaxNode? node)
        {
            Debug.Assert(node == null || file?.SourceTree != null);
            Debug.Assert(operation == null || file?.SourceTree != null);

            _compilation = compilation;
            _operation = operation;
            _symbol = symbol;
            _file = file;
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

            if (_file.HasValue)
            {
                if (_file.Value.SourceTree != null)
                {
                    sb.AppendLine($"{nameof(SyntaxTree)}: {_file.Value.SourceTree.FilePath}");
                }
                else
                {
                    RoslynDebug.Assert(_file.Value.AdditionalFile != null);
                    sb.AppendLine($"{nameof(AdditionalText)}: {_file.Value.AdditionalFile.Path}");
                }
            }

            if (_node != null)
            {
                RoslynDebug.Assert(_file.HasValue);
                RoslynDebug.Assert(_file.Value.SourceTree != null);

                var text = _file.Value.SourceTree.GetText();
                var lineSpan = text?.Lines?.GetLinePositionSpan(_node.Span);

                // can't use Kind since that is language specific. instead will output typename.
                sb.AppendLine($"{nameof(SyntaxNode)}: {GetFlattenedNodeText(_node)} [{_node.GetType().Name}]@{_node.Span} {(lineSpan.HasValue ? lineSpan.Value.ToString() : string.Empty)}");
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
